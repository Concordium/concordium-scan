using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[UnionType]
public abstract class BakerState
{
}

public class ActiveBakerState : BakerState
{
    /// <summary>
    /// This property is intentionally not part of the GraphQL schema.
    /// Only here as a back reference to the owning block so that child data can be loaded.
    /// </summary>
    [GraphQLIgnore]
    public Baker Owner { get; private set; } = null!;

    /// <summary>
    /// EF-core constructor!
    /// </summary>
    private ActiveBakerState() {}

    public ActiveBakerState(ulong stakedAmount, bool restakeEarnings, BakerPool? pool, PendingBakerChange? pendingChange)
    {
        StakedAmount = stakedAmount;
        RestakeEarnings = restakeEarnings;
        Pool = pool;
        PendingChange = pendingChange;
    }

    public ulong StakedAmount { get; set; } 
    
    public bool RestakeEarnings { get; set; }

    [GraphQLIgnore] // Still not ready for graphql endpoint...
    public BakerPool? Pool { get; set; }

    public PendingBakerChange? PendingChange { get; set; }

    [GraphQLDescription("Stake of the baker as a percentage of all CCDs in existence. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition.")]
    public decimal? GetStakePercentage()
    {
        return Owner.Statistics?.StakePercentage;
    }

    [GraphQLDescription("Ranking of the baker by staked amount. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition.")]
    public Ranking? GetRankingByStake()
    {
        var rank = Owner.Statistics?.RankByStake;
        var total = Owner.Statistics?.ActiveBakerCount;
        
        if (rank.HasValue && total.HasValue)
            return new Ranking(rank.Value, total.Value);
        return null;
    }
}

public class RemovedBakerState : BakerState
{
    public RemovedBakerState(DateTimeOffset removedAt)
    {
        RemovedAt = removedAt;
    }

    public DateTimeOffset RemovedAt { get; set; }
}