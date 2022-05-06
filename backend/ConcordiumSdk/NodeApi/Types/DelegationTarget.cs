namespace ConcordiumSdk.NodeApi.Types;

public abstract record DelegationTarget;

public record PassiveDelegationTarget : DelegationTarget;

public record BakerDelegationTarget(
    ulong BakerId) : DelegationTarget;