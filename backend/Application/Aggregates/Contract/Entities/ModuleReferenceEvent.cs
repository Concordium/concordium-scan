using System.IO;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Interop;
using Concordium.Sdk.Types;
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
    public IList<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; init; } = null!;
    /// <summary>
    /// See pagination comment on above.
    /// </summary>
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IList<ModuleReferenceRejectEvent> ModuleReferenceRejectEvents { get; init; } = null!;
    [GraphQLIgnore]
    public string? ModuleSource { get; private set; }
    [GraphQLIgnore]
    public string? Schema { get; private set; }
    [GraphQLIgnore]
    public ModuleSchemaVersion? SchemaVersion { get; private set; }

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
            var (versionedModuleSource, moduleSource, module) = await GetWasmModule(client, blockHeight, moduleReference);
            var schema = GetModuleSchema(module, versionedModuleSource);
            return new ModuleSourceInfo(moduleSource, schema?.Schema, schema?.SchemaVersion);
        }

        private static async Task<(VersionedModuleSource VersionedModuleSource, string ModuleSource, WebAssembly.Module Module)> GetWasmModule(IContractNodeClient client, ulong blockHeight, string moduleReference)
        {
            var absolute = new Absolute(blockHeight);
            var moduleRef = new ModuleReference(moduleReference);
    
            var moduleSourceAsync = await client.GetModuleSourceAsync(absolute, moduleRef);
            var versionedModuleSource = moduleSourceAsync.Response;
            var moduleSourceHex = Convert.ToHexString(versionedModuleSource.Source);
        
            using var stream = new MemoryStream(versionedModuleSource.Source);
            var moduleWasm = WebAssembly.Module.ReadFromBinary(stream);
            return (versionedModuleSource, moduleSourceHex, moduleWasm);
        }

        private static (string Schema, ModuleSchemaVersion SchemaVersion)? GetModuleSchema(WebAssembly.Module module, VersionedModuleSource moduleSource)
        {
            switch (moduleSource)
            {
                case ModuleV0:
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema", out var moduleV0SchemaUndefined))
                    {
                        return (moduleV0SchemaUndefined!, Application.Aggregates.Contract.Types.ModuleSchemaVersion.Undefined); // always v0
                    }
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema-v1", out var moduleV0SchemaV0))
                    {
                        return (moduleV0SchemaV0!, Application.Aggregates.Contract.Types.ModuleSchemaVersion.V0); // v0 (not a typo)
                    }
                    return null;
                case ModuleV1:
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema", out var moduleV1SchemaUndefined))
                    {
                        return (moduleV1SchemaUndefined!, Application.Aggregates.Contract.Types.ModuleSchemaVersion.Undefined); // v1, v2, or v3
                    }
                    if (GetSchemaFromWasmCustomSection(module, "concordium-schema-v1", out var moduleV1SchemaV1))
                    {
                        return (moduleV1SchemaV1!, Application.Aggregates.Contract.Types.ModuleSchemaVersion.V1); // v1 (not a typo)
                    }
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(moduleSource));
            }
        }
        
        private static bool GetSchemaFromWasmCustomSection(WebAssembly.Module module, string entryKey, out string? schema)
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
        public string? GetDisplaySchema([Parent] ModuleReferenceEvent module)
        {
            if (module.Schema == null)
            {
                return null;
            }
            var ffiOption = ModuleSchemaVersionExtensions.Into(module.SchemaVersion);
            var schemaDisplay = InteropBinding.SchemaDisplay(module.Schema, ffiOption);
            return schemaDisplay.Succeeded ? schemaDisplay.Message! : null;
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
