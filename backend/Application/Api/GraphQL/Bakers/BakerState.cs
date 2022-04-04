using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[UnionType]
public abstract class BakerState
{
}

public class ActiveBakerState : BakerState
{
    public ActiveBakerState(bool restakeEarnings, PendingBakerChange? pendingChange)
    {
        RestakeEarnings = restakeEarnings;
        PendingChange = pendingChange;
    }

    [GraphQLIgnore]
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