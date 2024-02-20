using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
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
    
    [GraphQLIgnore]
    public ICollection<ContractEvent> ContractEvents { get; internal set; } = null!;
    
    [GraphQLIgnore]
    public ICollection<ContractRejectEvent> ContractRejectEvents { get; private set; } = null!;
    
    [GraphQLIgnore]
    public ICollection<ModuleReferenceContractLinkEvent> ModuleReferenceContractLinkEvents { get; internal set; } = null!;
    
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
    public IQueryable<ContractEvent> GetContractEvents(GraphQlDbContext context) =>
        context
            .ContractEvents
            .Where(t =>
                t.ContractAddressIndex == ContractAddressIndex && t.ContractAddressSubIndex == ContractAddressSubIndex)
            .OrderByDescending(c => c.BlockHeight)
            .ThenByDescending(c => c.TransactionIndex)
            .ThenByDescending(c => c.EventIndex);
    
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<ContractRejectEvent> GetContractRejectEvents(GraphQlDbContext context) =>
        context
            .ContractRejectEvents
            .Where(t =>
                t.ContractAddressIndex == ContractAddressIndex && t.ContractAddressSubIndex == ContractAddressSubIndex)
            .OrderByDescending(c => c.BlockHeight)
            .ThenByDescending(c => c.TransactionIndex);

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
        public async Task<Contract?> GetContract(GraphQlDbContext context, ulong contractAddressIndex, ulong contractAddressSubIndex) =>
            await context.Contract
                .AsNoTracking()
                .Where(c => c.ContractAddressIndex == contractAddressIndex && c.ContractAddressSubIndex == contractAddressSubIndex)
                .SingleOrDefaultAsync();

        /// <summary>
        /// Get contracts with pagination support.
        /// </summary>
        [UsePaging(MaxPageSize = 100)]
        public IQueryable<Contract> GetContracts(
            GraphQlDbContext context) =>
            context.Contract
                .AsNoTracking()
                .OrderByDescending(c => c.ContractAddressIndex);
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
