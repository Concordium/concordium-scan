using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Blocks;

public class Block : IBlockOrTransactionUnion
{
    [ID]
    public long Id { get; set; }
    public string BlockHash { get; set; }
    public int BlockHeight { get; set; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public int? BakerId { get; init; }
    public bool Finalized { get; init; }
    public int TransactionCount { get; init; }
    public SpecialEvents SpecialEventsOld { get; init; }
    public FinalizationSummary? FinalizationSummary { get; init; }
    public BalanceStatistics BalanceStatistics { get; init; }
    public BlockStatistics BlockStatistics { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IEnumerable<Transaction> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .Where(tx => tx.BlockId == Id).OrderBy(x => x.TransactionIndex);
    }
    
    [GraphQLIgnore]
    public int ChainParametersId { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Task<ChainParameters> GetChainParameters([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.ChainParameters
            .AsNoTracking()
            .SingleAsync(x => x.Id == ChainParametersId);
    }
}