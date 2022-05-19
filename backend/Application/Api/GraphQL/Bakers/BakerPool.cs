using HotChocolate;

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
    public BakerPoolCommissionRates CommissionRates { get; init; }
    public string MetadataUrl { get; set; }

    [GraphQLDescription("The total amount staked by delegation to this baker pool.")]
    public ulong DelegatedStake { get; init; }

    [GraphQLDescription("The total amount staked in this baker pool. Includes both baker stake and delegated stake.")]
    public ulong TotalStake { get; init; }

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
}