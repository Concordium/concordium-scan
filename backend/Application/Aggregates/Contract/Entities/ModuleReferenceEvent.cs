using System.IO;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using WebAssembly;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractAddress = Application.Api.GraphQL.ContractAddress;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated when a module reference is created.
/// </summary>
public sealed class ModuleReferenceEvent : BaseIdentification
{
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
    public IList<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; init; } = null!;
    /// <summary>
    /// See pagination comment on above.
    /// </summary>
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IList<ModuleReferenceRejectEvent> ModuleReferenceRejectEvents { get; init; } = null!;
    [GraphQLIgnore]
    public string? ModuleSource { get; init; }
    public string? Schema { get; init; }
    [GraphQLIgnore]
    public ModuleSchemaVersion? SchemaVersion { get; init; }

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

    internal static async Task<ModuleReferenceEvent> Create(ModuleReferenceEventInfo info, IContractNodeClient client)
    {
        var moduleSchema = await ModuleSchema.Create(client, info.BlockHeight, info.ModuleReference);
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

    public readonly record struct ModuleReferenceEventInfo(
        ulong BlockHeight,
        string TransactionHash,
        ulong TransactionIndex,
        uint EventIndex,
        string ModuleReference,
        AccountAddress Sender,
        ImportSource Source,
        DateTimeOffset BlockSlotTime);
    
    public enum ModuleSchemaVersion
    {
        Undefined = -1,
        V0 = 0,
        V1 = 1,
        V2 = 2,
        V3 = 3
    }

    private sealed record ModuleSchema(string ModuleSource, string? Schema, ModuleSchemaVersion? ModuleSchemaVersion)
    {
        internal static async Task<ModuleSchema> Create(IContractNodeClient client, ulong blockHeight, string moduleReference)
        {
            var (versionedModuleSource, moduleSource, module) = await GetWasmModule(client, blockHeight, moduleReference);
            var schema = CreateModuleSchema(module, versionedModuleSource);
            return new ModuleSchema(moduleSource, schema?.Schema, schema?.SchemaVersion);
        }

        private static async Task<(VersionedModuleSource VersionedModuleSource, string ModuleSource, Module Module)> GetWasmModule(IContractNodeClient client, ulong blockHeight, string moduleReference)
        {
            var absolute = new Absolute(blockHeight);
            var moduleRef = new ModuleReference(moduleReference);
    
            var moduleSourceAsync = await client.GetModuleSourceAsync(absolute, moduleRef);
            var versionedModuleSource = moduleSourceAsync.Response;
            var moduleSourceHex = Convert.ToHexString(versionedModuleSource.Source);
        
            using var stream = new MemoryStream(versionedModuleSource.Source);
            var moduleWasm = Module.ReadFromBinary(stream);
            return (versionedModuleSource, moduleSourceHex, moduleWasm);
        }

        private static (string Schema, ModuleSchemaVersion SchemaVersion)? CreateModuleSchema(Module module, VersionedModuleSource moduleSource)
        {
            switch (moduleSource)
            {
                case ModuleV0:
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema", out var moduleV0SchemaUndefined))
                    {
                        return (moduleV0SchemaUndefined!, ModuleReferenceEvent.ModuleSchemaVersion.Undefined); // always v0
                    }
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema-v1", out var moduleV0SchemaV0))
                    {
                        return (moduleV0SchemaV0!, ModuleReferenceEvent.ModuleSchemaVersion.V0); // v0 (not a typo)
                    }
                    return null;
                case ModuleV1:
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema", out var moduleV1SchemaUndefined))
                    {
                        return (moduleV1SchemaUndefined!, ModuleReferenceEvent.ModuleSchemaVersion.Undefined); // v1, v2, or v3
                    }
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema-v1", out var moduleV1SchemaV1))
                    {
                        return (moduleV1SchemaV1!, ModuleReferenceEvent.ModuleSchemaVersion.V1); // v1 (not a typo)
                    }
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(moduleSource));
            }
        }
        
        private static bool GetSchemaFromWasmCustomSection(Module module, string entryKey, out string? schema)
        {
            schema = null;
            var customSection = module.CustomSections
                .SingleOrDefault(section => section.Name.StartsWith(entryKey, StringComparison.InvariantCulture));

            if (customSection == null) return false;
            
            schema = Convert.ToHexString(customSection.Content.ToArray());
            return true;
        }
    }
    
    [ExtendObjectType(typeof(Query))]
    public class ModuleReferenceEventQuery
    {
        public Task<ModuleReferenceEvent?> GetModuleReferenceEvent(GraphQlDbContext context, string moduleReference)
        {
            return context.ModuleReferenceEvents
                .AsSplitQuery()
                .AsNoTracking()
                .Where(m => m.ModuleReference == moduleReference)
                .Include(m => m.ModuleReferenceContractLinkEvents
                    .OrderByDescending(me => me.BlockHeight)
                    .ThenByDescending(me => me.TransactionIndex)
                    .ThenByDescending(me => me.EventIndex))
                .Include(m => m.ModuleReferenceRejectEvents
                    .OrderByDescending(me => me.BlockHeight)
                    .ThenByDescending(me => me.TransactionIndex))
                .SingleOrDefaultAsync();
        }
    }
    
    /// <summary>
    /// Adds additional fields to the GraphQL type <see cref="ModuleReferenceEvent"/>.
    /// </summary>
    [ExtendObjectType(typeof(ModuleReferenceEvent))]
    public sealed class ModuleReferenceEventExtensions
    {
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
