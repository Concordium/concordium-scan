using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[UnionType]
public abstract record BakerState;

public record ActiveBakerState(
    PendingBakerChange? PendingChange) : BakerState;
    
public record RemovedBakerState(
    bool _ = false) : BakerState;