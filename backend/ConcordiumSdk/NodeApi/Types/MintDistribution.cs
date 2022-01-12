namespace ConcordiumSdk.NodeApi.Types;

public record MintDistribution(
    decimal MintPerSlot,
    decimal BakingReward,
    decimal FinalizationReward);