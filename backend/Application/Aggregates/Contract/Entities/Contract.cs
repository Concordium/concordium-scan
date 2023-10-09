using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Initial event stored when a smart contract is created
/// These are non mutable values through the lifetime of the smart contract.
/// </summary>
public sealed class Contract : BaseIdentification
{
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public string ContractAddress { get; init; } = null!;
    public uint EventIndex { get; init; }
    public AccountAddress Creator { get; init; } = null!;
 
    /// <summary>
    /// It is important, that when pagination is used together with a <see cref="System.Linq.IQueryable"/> return type
    /// then aggregation result like <see cref="Contract.ContractExtensions.GetAmount"/> will not be correct.
    ///
    /// Hence pagination should only by used in cases where database query has executed like <see cref="Contract.ContractQuery.GetContract"/>.
    /// </summary>
    [UsePaging(IncludeTotalCount = true)]
    public IList<ContractEvent> ContractEvents { get; init; } = null!;
    /// <summary>
    /// See pagination comment on above.
    /// </summary>
    [UsePaging(IncludeTotalCount = true)]
    public IList<ContractRejectEvent> ContractRejectEvents { get; init; } = null!;
    public IList<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; init; } = null!;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private Contract()
    {}

    internal Contract(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        ContractAddress contractAddress,
        AccountAddress creator,
        ImportSource source,
        DateTimeOffset blockSlotTime) : 
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        ContractAddress = contractAddress.AsString;
        EventIndex = eventIndex;
        Creator = creator;
    }
    
    [ExtendObjectType(typeof(Query))]
    public class ContractQuery
    {
        public Task<Contract?> GetContract(GraphQlDbContext context, ulong contractAddressIndex, ulong contractAddressSubIndex)
        {
            return context.Contract
                .AsSplitQuery()
                .AsNoTracking()
                .Where(c => c.ContractAddressIndex == contractAddressIndex && c.ContractAddressSubIndex == contractAddressSubIndex)
                .Include(c => c.ContractEvents
                    .OrderByDescending(ce => ce.BlockHeight)
                    .ThenByDescending(ce => ce.TransactionIndex)
                    .ThenByDescending(ce => ce.EventIndex))
                .Include(c => c.ContractRejectEvents
                    .OrderByDescending(ce => ce.BlockHeight)
                    .ThenByDescending(ce => ce.TransactionIndex))
                .Include(c => c.ModuleReferenceContractLinkEvents
                    .OrderByDescending(ce => ce.BlockHeight)
                    .ThenByDescending(ce => ce.TransactionIndex)
                    .ThenByDescending(ce => ce.EventIndex))
                .SingleOrDefaultAsync();
        }
        
        /// <summary>
        /// Get contracts with pagination support.
        /// 
        /// Currently contracts module reference are not updated for the lifetime of the contract. Hence often there will
        /// be only one module link event for each contract.
        ///
        /// Because of this we are currently not using <see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.AsSplitQuery"/>.
        /// If performance issues on this query is seen and module reference links increases then look into using above splitting technique.
        /// </summary>
        /// <remarks>
        ///     See <see href="https://aka.ms/efcore-docs-split-queries">EF Core split queries</see> for more information.
        /// </remarks> 
        [UsePaging]
        public IQueryable<Contract> GetContracts(
            GraphQlDbContext context) 
        {
            return context.Contract
                .AsNoTracking()
                .Include(s => s.ContractEvents)
                .Include(s => s.ModuleReferenceContractLinkEvents)
                .OrderByDescending(c => c.ContractAddressIndex);
        }
    }

    /// <summary>
    /// Adds additional field to the GraphQL type <see cref="Contract"/>
    /// </summary>
    [ExtendObjectType(typeof(Contract))]
    public sealed class ContractExtensions
    {
        public string GetContractName([Parent] Contract contract)
        {
            var contractEvent = contract.ContractEvents
                .First(e => e.Event is ContractInitialized);
            return (contractEvent.Event as ContractInitialized)!.InitName[5..];
        }
        
        /// <summary>
        /// Returns the current linked module reference which is the latest added <see cref="ModuleReferenceContractLinkEvent"/>.
        /// </summary>
        public string GetModuleReference([Parent] Contract contract)
        {
            var link = contract.ModuleReferenceContractLinkEvents
                .Where(link => link.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                .OrderByDescending(link => link.BlockHeight)
                .ThenByDescending(link => link.TransactionIndex)
                .ThenByDescending(link => link.EventIndex)
                .First();
            return link.ModuleReference;
        }

        /// <summary>
        /// Returns aggregated amount from events on contract.
        /// </summary>
        public double GetAmount([Parent] Contract contract)
        {
            if (contract.ContractEvents is null)
            {
                return 0;
            }

            var amount = 0L;
            foreach (var contractEvent in contract.ContractEvents)
            {
                switch (contractEvent.Event)
                {
                    case ContractInitialized contractInitialized:
                        amount += (long)contractInitialized.Amount;
                        break;
                    case ContractUpdated contractUpdated:
                        amount += (long)contractUpdated.Amount;
                        break;
                    case ContractCall contractCall:
                    {
                        if (contractCall.ContractUpdated.Instigator is ContractAddress instigator &&
                            instigator.Index == contract.ContractAddressIndex &&
                            instigator.SubIndex == contract.ContractAddressSubIndex)
                        {
                            amount -= (long)contractCall.ContractUpdated.Amount;
                        }                        
                        break;
                    }
                    case Transferred transferred:
                        if (transferred.From is ContractAddress contractAddressFrom &&
                            contractAddressFrom.Index == contract.ContractAddressIndex &&
                            contractAddressFrom.SubIndex == contract.ContractAddressSubIndex)
                        {
                            amount -= (long)transferred.Amount;
                        }
                        break;
                }
            }

            return amount;
        }
    }
}
