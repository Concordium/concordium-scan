using System.IO;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Types;
using Dapper;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractAddress = Application.Api.GraphQL.ContractAddress;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated when a module reference is created.
/// </summary>
public sealed class ModuleReferenceEvent : BaseIdentification
{
    [GraphQLIgnore]
    public uint EventIndex { get; init; }
    public string ModuleReference { get; init; } = null!;
    public AccountAddress Sender { get; init; } = null!;
    /// <summary>
    /// It is important, that when pagination is used together with a <see cref="System.Linq.IQueryable"/> return type
    /// then aggregation result like <see cref="Contract.ContractExtensions.GetAmount"/> will not be correct.
    ///
    /// Hence pagination should only by used in cases where database query has executed like
    /// <see cref="ModuleReferenceEvent.ModuleReferenceEventQuery.GetModuleReferenceEvent"/>.
    /// </summary>
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IList<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; internal set; } = null!;
    /// <summary>
    /// See pagination comment on above.
    /// </summary>
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IList<ModuleReferenceRejectEvent> ModuleReferenceRejectEvents { get; internal set; } = null!;
    [GraphQLIgnore]
    public string? ModuleSource { get; private set; }
    [GraphQLIgnore]
    public string? Schema { get; private set; }
    [GraphQLIgnore]
    public ModuleSchemaVersion? SchemaVersion { get; private set; }

    internal VersionedModuleSchema? GetVersionedModuleSchema()
    {
        if (Schema == null || SchemaVersion == null)
        {
            return null;
        }

        return new VersionedModuleSchema(Convert.FromHexString(Schema), SchemaVersion.Value);
    }

    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ModuleReferenceEvent()
    {}

    internal ModuleReferenceEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        string moduleReference,
        AccountAddress sender,
        string moduleSource,
        string? schema,
        ModuleSchemaVersion? version,
        ImportSource source,
        DateTimeOffset blockSlotTime) : 
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        EventIndex = eventIndex;
        ModuleReference = moduleReference;
        Sender = sender;
        ModuleSource = moduleSource;
        Schema = schema;
        SchemaVersion = version;
    }

    internal void UpdateWithModuleSourceInfo(ModuleSourceInfo info)
    {
        ModuleSource = info.ModuleSource;
        Schema = info.Schema;
        SchemaVersion = info.ModuleSchemaVersion;
    }

    internal static async Task<ModuleReferenceEvent> Create(ModuleReferenceEventInfo info, IContractNodeClient client)
    {
        var moduleSchema = await ModuleSourceInfo.Create(client, info.BlockHeight, info.ModuleReference);
        return new ModuleReferenceEvent(
            info.BlockHeight,
            info.TransactionHash,
            info.TransactionIndex,
            info.EventIndex,
            info.ModuleReference,
            info.Sender,
            moduleSchema.ModuleSource,
            moduleSchema.Schema,
            moduleSchema.ModuleSchemaVersion,
            info.Source,
            info.BlockSlotTime
        );
    } 

    internal sealed record ModuleSourceInfo(string ModuleSource, string? Schema, ModuleSchemaVersion? ModuleSchemaVersion)
    {
        internal static async Task<ModuleSourceInfo> Create(IContractNodeClient client, ulong blockHeight, string moduleReference)
        {
            var (versionedModuleSource, moduleSource) = await GetVersionedModuleSchema(client, blockHeight, moduleReference);
            return new ModuleSourceInfo(moduleSource, versionedModuleSource?.Schema != null ? Convert.ToHexString(versionedModuleSource.Schema) : null, versionedModuleSource?.Version);
        }

        private static async Task<(VersionedModuleSchema? versionedModuleSchema, string ModuleSource)> GetVersionedModuleSchema(IContractNodeClient client, ulong blockHeight, string moduleReference)
        {
            var absolute = new Absolute(blockHeight);
            var moduleRef = new ModuleReference(moduleReference);
    
            var moduleSourceAsync = await client.GetModuleSourceAsync(absolute, moduleRef);
            var versionedModuleSource = moduleSourceAsync.Response;
            var versionedModuleSchema = versionedModuleSource.GetModuleSchema();
            var moduleSourceHex = Convert.ToHexString(versionedModuleSource.Source);
            
            return (versionedModuleSchema, moduleSourceHex);
        }
    }
    
    [ExtendObjectType(typeof(Query))]
    public class ModuleReferenceEventQuery
    {
        
        public async Task<ModuleReferenceEvent?> GetModuleReferenceEvent(GraphQlDbContext context, string moduleReference)
        {
            var module = await context.ModuleReferenceEvents
                .AsNoTracking()
                .Where(m => m.ModuleReference == moduleReference)
                .SingleOrDefaultAsync();
            if (module == null)
            {
                return null;
            }

            var connection = context.Database.GetDbConnection();
            var parameter = new { module.ModuleReference };
            var moduleReferenceContractLinkEvents = await connection
                .QueryAsync<ModuleReferenceContractLinkEvent>(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkEventsParameterModuleReferenceSql, parameter);
            var moduleReferenceRejectEvents = await connection
                .QueryAsync<ModuleReferenceRejectEvent>(ModuleReferenceRejectEvent.ModuleReferenceRejectEventsSql, parameter);
            module.ModuleReferenceContractLinkEvents = moduleReferenceContractLinkEvents.ToList();
            module.ModuleReferenceRejectEvents = moduleReferenceRejectEvents.ToList();

            return module;
        }
    }
    
    /// <summary>
    /// Adds additional fields to the GraphQL type <see cref="ModuleReferenceEvent"/>.
    /// </summary>
    [ExtendObjectType(typeof(ModuleReferenceEvent))]
    public sealed class ModuleReferenceEventExtensions
    {
        private readonly ILogger _logger = Log.ForContext<ModuleReferenceEventExtensions>();

        /// <summary>
        /// Returns module schema in a human interpretable form. Only present if the schema is embedded into the
        /// Wasm module.
        /// </summary>
        public string? GetDisplaySchema([Parent] ModuleReferenceEvent module)
        {
            if (module.Schema == null || module.SchemaVersion == null)
            {
                return null;
            }

            try
            {
                var versionedModuleSchema = new VersionedModuleSchema(Convert.FromHexString(module.Schema), module.SchemaVersion.Value);
                return versionedModuleSchema.GetDeserializedSchema().ToString();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when getting module schema to display");
                return null;
            }
        }
        
        [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
        public IList<LinkedContract> GetLinkedContracts([Parent] ModuleReferenceEvent moduleReferenceEvent)
        {
            var map = new Dictionary<(ulong, ulong), DateTimeOffset>();
            foreach (var moduleReferenceContractLinkEvent in moduleReferenceEvent.ModuleReferenceContractLinkEvents
                         .OrderBy(me => me.BlockHeight)
                         .ThenBy(me => me.TransactionIndex)
                         .ThenBy(me => me.EventIndex))
            {
                switch (moduleReferenceContractLinkEvent.LinkAction)
                {
                    case ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added:
                        map[(moduleReferenceContractLinkEvent.ContractAddressIndex, moduleReferenceContractLinkEvent.ContractAddressSubIndex)] = moduleReferenceContractLinkEvent.BlockSlotTime;
                        break;
                    case ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed:
                        map.Remove((moduleReferenceContractLinkEvent.ContractAddressIndex,
                            moduleReferenceContractLinkEvent.ContractAddressSubIndex));
                        break;
                }
            }

            return map
                .Select(entry => new LinkedContract(new ContractAddress(entry.Key.Item1, entry.Key.Item2), entry.Value))
                .OrderByDescending(l => l.LinkedDateTime)
                .ToList();
        }
    }
}

public record LinkedContract(ContractAddress ContractAddress, DateTimeOffset LinkedDateTime);
