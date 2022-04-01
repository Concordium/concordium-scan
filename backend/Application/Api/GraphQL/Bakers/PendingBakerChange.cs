using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[InterfaceType()]
public abstract record PendingBakerChange(DateTimeOffset EffectiveTime);

public record PendingBakerRemoval(DateTimeOffset EffectiveTime) : PendingBakerChange(EffectiveTime);