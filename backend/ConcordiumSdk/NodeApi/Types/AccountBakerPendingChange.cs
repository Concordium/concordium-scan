using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public abstract record AccountBakerPendingChange;

public record AccountBakerRemovePending(ulong Epoch) : AccountBakerPendingChange;
public record AccountBakerReduceStakePending(CcdAmount NewStake, ulong Epoch) : AccountBakerPendingChange;