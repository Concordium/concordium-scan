using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[UnionType]
public abstract class BakerState
{
}

public class ActiveBakerState : BakerState
{
    public ActiveBakerState(bool restakeRewards, PendingBakerChange? pendingChange)
    {
        RestakeRewards = restakeRewards;
        PendingChange = pendingChange;
    }

    [GraphQLIgnore]
    public bool RestakeRewards { get; set; }
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