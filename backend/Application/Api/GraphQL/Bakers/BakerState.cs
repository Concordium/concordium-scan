using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[UnionType]
public abstract class BakerState
{
}

public class ActiveBakerState : BakerState
{
    public ActiveBakerState(ulong stakedAmount, bool restakeEarnings, PendingBakerChange? pendingChange)
    {
        StakedAmount = stakedAmount;
        RestakeEarnings = restakeEarnings;
        PendingChange = pendingChange;
    }

    public ulong StakedAmount { get; set; } 
    
    public bool RestakeEarnings { get; set; }
    
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