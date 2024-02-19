using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Dapper;
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
    
    [GraphQLIgnore]
    public uint EventIndex { get; init; }
    public AccountAddress Creator { get; init; } = null!;
    
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<ContractEvent> ContractEvents { get; internal set; } = null!;
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<ContractRejectEvent> ContractRejectEvents { get; private set; } = null!;
    
    [GraphQLIgnore]
    public IQueryable<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; internal set; } = null!;
    
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

    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<Token> GetTokens(GraphQlDbContext context) =>
        context
            .Tokens
            .Where(t =>
                t.ContractIndex == ContractAddressIndex && t.ContractSubIndex == ContractAddressSubIndex)
            .OrderBy(t => t.TokenId);

    [ExtendObjectType(typeof(Query))]
    public class ContractQuery
    {
        public async Task<Contract?> GetContract(GraphQlDbContext context, ulong contractAddressIndex, ulong contractAddressSubIndex)
        {
            var contract = await context.Contract
                .AsNoTracking()
                .Where(c => c.ContractAddressIndex == contractAddressIndex && c.ContractAddressSubIndex == contractAddressSubIndex)
                .SingleOrDefaultAsync();

            return contract;
        }
        
        /// <summary>
        /// Get contracts with pagination support.
        /// </summary>
        [UsePaging(MaxPageSize = 100)]
        public IQueryable<Contract> GetContracts(
            GraphQlDbContext context) 
        {
            return context.Contract
                .AsSplitQuery()
                .AsNoTracking()
                .Include(s => s.ContractEvents
                    .OrderByDescending(ce => ce.BlockHeight)
                    .ThenByDescending(ce => ce.TransactionIndex)
                    .ThenByDescending(ce => ce.EventIndex))
                .Include(s => s.ModuleReferenceContractLinkEvents
                    .OrderByDescending(ce => ce.BlockHeight)
                    .ThenByDescending(ce => ce.TransactionIndex)
                    .ThenByDescending(ce => ce.EventIndex))
                .OrderByDescending(c => c.ContractAddressIndex);
        }
    }

    /// <summary>
    /// Adds additional field to the GraphQL type <see cref="Contract"/>
    /// </summary>
    [ExtendObjectType(typeof(Contract))]
    public sealed class ContractExtensions
    {
        public Task<ContractSnapshot> GetSnapshot([Parent] Contract contract, GraphQlDbContext context) =>
            context.ContractSnapshots
                .Where(s => s.ContractAddressIndex == contract.ContractAddressIndex &&
                            s.ContractAddressSubIndex == contract.ContractAddressSubIndex)
                .OrderByDescending(s => s.BlockHeight)
                .FirstAsync();
    }
}
