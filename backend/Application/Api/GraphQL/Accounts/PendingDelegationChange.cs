namespace Application.Api.GraphQL.Accounts;

public abstract record PendingDelegationChange(
    DateTimeOffset EffectiveTime);
    
public record PendingDelegationRemoval(
    DateTimeOffset EffectiveTime) : PendingDelegationChange(EffectiveTime);

public record PendingDelegationReduceStake(
    DateTimeOffset EffectiveTime,
    ulong NewStakedAmount) : PendingDelegationChange(EffectiveTime);