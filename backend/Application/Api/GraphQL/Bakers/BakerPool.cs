using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Metrics;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Api.GraphQL.Bakers;

public class BakerPool
{
    /// <summary>
    /// This property is intentionally not part of the GraphQL schema.
    /// Only here as a back reference to the owning block so that child data can be loaded.
    /// </summary>
    [GraphQLIgnore]
    public ActiveBakerState Owner { get; private set; } = null!;

    public BakerPoolOpenStatus OpenStatus { get; set; }
    public CommissionRates CommissionRates { get; init; }
    public string MetadataUrl { get; set; }

    [GraphQLDescription("The total amount staked by delegation to this baker pool.")]
    public ulong DelegatedStake { get; init; }

    [GraphQLDescription("The maximum amount that may be delegated to the pool, accounting for leverage and stake limits.")]
    public ulong DelegatedStakeCap { get; init; }

    [GraphQLDescription("The total amount staked in this baker pool. Includes both baker stake and delegated stake.")]
    public ulong TotalStake { get; init; }

    public int DelegatorCount { get; set; }

    [GraphQLIgnore]
    public CurrentPaydayStatus? PaydayStatus { get; set; }
    
    [GraphQLDescription("Total stake of the baker pool as a percentage of all CCDs in existence. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition.")]
    public decimal? GetTotalStakePercentage()
    {
        return Owner.Owner.Statistics?.PoolTotalStakePercentage;
    }

    public async Task<PoolApy> GetApy([Service] DatabaseSettings dbSettings, [Service] ITimeProvider timeProvider,  ApyPeriod period)
    {
        await using var conn = new NpgsqlConnection(dbSettings.ConnectionString);
        await conn.OpenAsync();

        var utcNow = timeProvider.UtcNow;
        var queryParams = new
        {
            FromTime = period switch
            {
                ApyPeriod.Last7Days => utcNow.AddDays(-7),
                ApyPeriod.Last30Days => utcNow.AddDays(-30),
                _ => throw new NotImplementedException()
            },
            ToTime = utcNow,
            PoolId = Owner.Owner.BakerId
        };
            
        var sql = @"
            select exp(avg(ln(total))) - 1      as total_apy_geom_mean,
                   exp(avg(ln(baker))) - 1      as baker_apy_geom_mean,
                   exp(avg(ln(delegators))) - 1 as delegators_apy_geom_mean
            from (select coalesce(total_apy, 0) + 1      as total,
                         coalesce(baker_apy, 0) + 1      as baker,
                         coalesce(delegators_apy, 0) + 1 as delegators
                  from graphql_payday_summaries ps
                           left join metrics_payday_pool_rewards r on r.block_id = ps.block_id
                  where ps.payday_time between @FromTime and @ToTime
                    and r.pool_id = @PoolId) a;";
        
        var result = await conn.QuerySingleAsync(sql, queryParams);
        var totalApy = (double?)result.total_apy_geom_mean;
        var bakerApy = (double?)result.baker_apy_geom_mean;
        var delegatorsApy = (double?)result.delegators_apy_geom_mean;

        return new PoolApy(totalApy, bakerApy, delegatorsApy);
    }

    [GraphQLDescription("Ranking of the baker pool by total staked amount. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition.")]
    public Ranking? GetRankingByTotalStake()
    {
        var rank = Owner.Owner.Statistics?.PoolRankByTotalStake;
        var total = Owner.Owner.Statistics?.ActiveBakerPoolCount;
        
        if (rank.HasValue && total.HasValue)
            return new Ranking(rank.Value, total.Value);
        return null;
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10)]
    public IQueryable<DelegationSummary> GetDelegators([ScopedService] GraphQlDbContext dbContext)
    {
        var bakerId = Owner.Owner.BakerId;

        return dbContext.Accounts.AsNoTracking()
            .Where(x => x.Delegation!.DelegationTarget == new BakerDelegationTarget(bakerId))
            .OrderByDescending(x => x.Delegation!.StakedAmount)
            .Select(x => new DelegationSummary(x.CanonicalAddress, x.Delegation!.StakedAmount, x.Delegation.RestakeEarnings));
    }
    
    [GraphQLDeprecated("Use poolRewards instead. Will be removed in the near future")]
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "pool_reward_by_descending_index")]
    public IQueryable<PoolReward> GetRewards([ScopedService] GraphQlDbContext dbContext)
    {
        var pool = new BakerPoolRewardTarget(Owner.Owner.BakerId);

        return dbContext.PoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "payday_pool_reward_by_descending_index")]
    public IQueryable<PaydayPoolReward> GetPoolRewards([ScopedService] GraphQlDbContext dbContext)
    {
        var pool = new BakerPoolRewardTarget(Owner.Owner.BakerId);

        return dbContext.PaydayPoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
}

public enum ApyPeriod
{
    Last7Days,
    Last30Days
}

public record PoolApy(
    double? TotalApy,
    double? BakerApy,
    double? DelegatorsApy);
