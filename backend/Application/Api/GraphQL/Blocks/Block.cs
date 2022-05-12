using System.Linq.Expressions;
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
    public IQueryable<SpecialEvent> GetSpecialEvents(
        [ScopedService] GraphQlDbContext dbContext,
        [GraphQLDescription("Filter special events by special event type. Set to null to return all special events (no filtering).")]
        SpecialEventTypeFilter[]? includeFilter = null)
    {
        var query = dbContext.SpecialEvents
            .AsNoTracking()
            .Where(x => x.BlockId == Id);

        if (includeFilter != null)
        {
            var parameter = Expression.Parameter(typeof(SpecialEvent));
            var includedTypes = includeFilter.Select(Map);
            var typeIsExpressions = includedTypes.Select(x => Expression.TypeIs(parameter, x));
            var combinedExpression = typeIsExpressions.Cast<Expression>().Aggregate(Expression.Or);
            var filterLambda = Expression.Lambda<Func<SpecialEvent, bool>>(combinedExpression, parameter);
            query = query.Where(filterLambda);
        }

        return query
            .OrderBy(x => x.Index);
    }

    private Type Map(SpecialEventTypeFilter arg)
    {
        return arg switch
        {
            SpecialEventTypeFilter.Mint =>typeof(MintSpecialEvent), 
            SpecialEventTypeFilter.FinalizationRewards =>typeof(FinalizationRewardsSpecialEvent),
            SpecialEventTypeFilter.BlockRewards =>typeof(BlockRewardsSpecialEvent),
            SpecialEventTypeFilter.BakingRewards =>typeof(BakingRewardsSpecialEvent),
            SpecialEventTypeFilter.PaydayAccountReward =>typeof(PaydayAccountRewardSpecialEvent),
            SpecialEventTypeFilter.BlockAccrueReward =>typeof(BlockAccrueRewardSpecialEvent),
            SpecialEventTypeFilter.PaydayFoundationReward =>typeof(PaydayFoundationRewardSpecialEvent), 
            SpecialEventTypeFilter.PaydayPoolReward =>typeof(PaydayPoolRewardSpecialEvent), 
            _ => throw new NotImplementedException()
        };
    }

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

public enum SpecialEventTypeFilter
{
    Mint,
    FinalizationRewards,
    BlockRewards,
    BakingRewards,
    PaydayAccountReward,
    BlockAccrueReward,
    PaydayFoundationReward,
    PaydayPoolReward
}