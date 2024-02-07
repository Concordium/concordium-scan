using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using HotChocolate;
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
    public BalanceStatistics BalanceStatistics { get; init; }
    public BlockStatistics BlockStatistics { get; init; }

    [UsePaging]
    public IQueryable<SpecialEvent> GetSpecialEvents(
        GraphQlDbContext dbContext,
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

    [UsePaging]
    public IEnumerable<Transaction> GetTransactions(GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .Where(tx => tx.BlockId == Id).OrderBy(x => x.TransactionIndex);
    }
    
    [GraphQLIgnore]
    public int ChainParametersId { get; init; }

    public Task<ChainParameters> GetChainParameters(GraphQlDbContext dbContext)
    {
        return dbContext.ChainParameters
            .AsNoTracking()
            .SingleAsync(x => x.Id == ChainParametersId);
    }

    internal IBlockHashInput Into()
    {
        return new Given(Concordium.Sdk.Types.BlockHash.From(BlockHash));
    }
    
    internal static Block MapBlock(
        BlockInfo blockInfo,
        double blockTime,
        int chainParametersId,
        BalanceStatistics balanceStatistics)
    {
        var block = new Block
        {
            BlockHash = blockInfo.BlockHash.ToString(),
            BlockHeight = (int)blockInfo.BlockHeight,
            BlockSlotTime = blockInfo.BlockSlotTime,
            BakerId = blockInfo.BlockBaker != null ? (int)blockInfo.BlockBaker!.Value.Id.Index : null,
            Finalized = blockInfo.Finalized,
            TransactionCount = (int)blockInfo.TransactionCount,
            BalanceStatistics = balanceStatistics,
            BlockStatistics = new BlockStatistics
            {
                BlockTime = blockTime, 
                FinalizationTime = null // Updated when the block with proof of finalization for this block is imported
            },
            ChainParametersId = chainParametersId
        };
        return block;
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
