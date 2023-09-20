using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Polly;

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
    [UsePaging(IncludeTotalCount = true)]
    public IList<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; init; } = null!;
    /// <summary>
    /// See pagination comment on above.
    /// </summary>
    [UsePaging(IncludeTotalCount = true)]
    public IList<ModuleReferenceRejectEvent> ModuleReferenceRejectEvents { get; init; } = null!;

    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ModuleReferenceEvent()
    {}

    public ModuleReferenceEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        string moduleReference,
        AccountAddress sender,
        ImportSource source,
        DateTimeOffset blockSlotTime) : 
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        EventIndex = eventIndex;
        ModuleReference = moduleReference;
        Sender = sender;
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
        [UsePaging(IncludeTotalCount = true)]
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
