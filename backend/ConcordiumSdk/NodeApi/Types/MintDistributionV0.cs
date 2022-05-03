namespace ConcordiumSdk.NodeApi.Types;

public record MintDistributionV0(
    decimal MintPerSlot,
    decimal BakingReward,
    decimal FinalizationReward);