using Application.Api.GraphQL.Network;
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

    public BakerPool? Pool { get; set; }

    public PendingBakerChange? PendingChange { get; set; }
    
    [GraphQLDescription("The status of the bakers node. Will be null if no status for the node exists.")]
    public NodeStatus? GetNodeStatus([Service] NodeStatusSnapshot nodeSummarySnapshot)
    {
        var statuses = nodeSummarySnapshot.NodeStatuses
            .Where(x => x.ConsensusBakerId == (ulong)Owner.BakerId)
            .ToArray();

        if (statuses.Length == 0)
            return null;
        if (statuses.Length == 1)
            return statuses[0];

        /* Multiple nodes report as the given baker ID. For now, we will return null.
         * Another approach that could be implemented at a later point would either be to 
         * return a list of nodes, so that the UI could show users that multiple nodes report
         * for this baker id -OR- have the result carry a result flag, that could indicate to
         * users that multiple nodes report for this baker id. */
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