namespace ConcordiumSdk.NodeApi.Types;

/// <param name="PoolOwnerCooldown">Number of seconds that pool owners must cooldown when reducing their equity capital or closing the pool.</param>
/// <param name="DelegatorCooldown">Number of seconds that a delegator must cooldown when reducing their delegated stake.</param>
public record CooldownParameters(
    ulong PoolOwnerCooldown,
    ulong DelegatorCooldown);