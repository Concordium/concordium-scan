using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

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

    [GraphQLDescription("Total stake of the baker pool as a percentage of all CCDs in existence. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition.")]
    public decimal? GetTotalStakePercentage()
    {
        return Owner.Owner.Statistics?.PoolTotalStakePercentage;
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
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "pool_reward_by_descending_index")]
    public IQueryable<PoolReward> GetRewards([ScopedService] GraphQlDbContext dbContext)
    {
        var pool = new BakerPoolRewardTarget(Owner.Owner.BakerId);

        return dbContext.PoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
}