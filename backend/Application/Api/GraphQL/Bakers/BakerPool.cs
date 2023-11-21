using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Payday;
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
    /// <summary>
    /// This holds the latest update to commission rates and are not necessarily those which are used at the
    /// current payday.
    /// The commissions rates which are in effect the current payday are at
    /// <see cref="CurrentPaydayStatus.CommissionRates"/>.
    /// </summary>
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
    
    public decimal? GetLotteryPower()
    {
        return PaydayStatus?.LotteryPower;
    }

    /// <summary>
    /// Returns the active commissions rate in the current payday.  
    /// </summary>
    public CommissionRates? GetPaydayCommissionRates() => PaydayStatus?.CommissionRates;

    public PoolApy GetApy(ApyPeriod period)
    {
        return period switch
        {
            ApyPeriod.Last7Days => Owner.Owner.PoolApys?.Apy7Days ?? new PoolApy(null, null, null),
            ApyPeriod.Last30Days => Owner.Owner.PoolApys?.Apy30Days ?? new PoolApy(null, null, null),
            _ => throw new NotImplementedException()
        };
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
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "payday_pool_reward_by_descending_index")]
    public IQueryable<PaydayPoolReward> GetPoolRewards([ScopedService] GraphQlDbContext dbContext)
    {
        var pool = new BakerPoolRewardTarget(Owner.Owner.BakerId);

        return dbContext.PaydayPoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
    
    /// <summary>
    /// Creates a new default pool.
    ///
    /// <see cref="CommissionRates"/> will be overwritten in later events from same transaction. When a validator is
    /// created multiple events are generated, where <see cref="Concordium.Sdk.Types.BakerAddedEvent"/> is the first.
    ///
    /// <see cref="PaydayStatus"/> is set to null since the validator will first be active on the next payday. On payday
    /// blocks <see cref="PaydayStatus"/> is overwritten with values fetched from the chain.
    /// <remarks>
    /// <see href="https://testnet.ccdscan.io/staking?dcount=1&amp;dentity=transaction&amp;dhash=c7357fe0d11ec8e6c6a7f410971624845cd19c2034cd4bd44df7cfd759344026">Events when adding a validator.</see>
    /// </remarks> 
    /// </summary>
    internal static BakerPool CreateDefaultBakerPool()
    {
        return new BakerPool
        {
            OpenStatus = BakerPoolOpenStatus.ClosedForAll,
            MetadataUrl = "",
            CommissionRates = new CommissionRates
            {
                TransactionCommission = 0.0m,
                FinalizationCommission = 0.0m,
                BakingCommission = 0.0m
            },
            PaydayStatus = null,
        };
    }
}
