namespace ConcordiumSdk.NodeApi.Types;

public abstract record DelegationTarget;

public record LPoolDelegationTarget : DelegationTarget;

public record BakerDelegationTarget(
    ulong BakerId) : DelegationTarget;