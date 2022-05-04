using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Bakers;

[UnionType]
public abstract record PendingBakerChange(
    DateTimeOffset EffectiveTime, 
    [property:GraphQLIgnore] // this field should be deprecated from CC-node version 4, when pending changes come as a utc timestamp
    ulong? Epoch);

public record PendingBakerRemoval(
    DateTimeOffset EffectiveTime, 
    ulong? Epoch = null) : PendingBakerChange(EffectiveTime, Epoch);

public record PendingBakerReduceStake(
    DateTimeOffset EffectiveTime,
    ulong NewStakedAmount, 
    ulong? Epoch = null) : PendingBakerChange(EffectiveTime, Epoch);