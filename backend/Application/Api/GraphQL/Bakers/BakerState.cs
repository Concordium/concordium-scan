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
}

public class RemovedBakerState : BakerState
{
    public RemovedBakerState(DateTimeOffset removedAt)
    {
        RemovedAt = removedAt;
    }

    public DateTimeOffset RemovedAt { get; set; }
}