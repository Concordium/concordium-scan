using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public abstract record AccountBakerPendingChange;

public record AccountBakerRemovePendingV0(ulong Epoch) : AccountBakerPendingChange;
public record AccountBakerReduceStakePendingV0(CcdAmount NewStake, ulong Epoch) : AccountBakerPendingChange;
public record AccountBakerRemovePendingV1(DateTimeOffset EffectiveTime) : AccountBakerPendingChange;
public record AccountBakerReduceStakePendingV1(CcdAmount NewStake, DateTimeOffset EffectiveTime) : AccountBakerPendingChange;