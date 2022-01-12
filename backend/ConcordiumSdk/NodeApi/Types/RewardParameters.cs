namespace ConcordiumSdk.NodeApi.Types;

public record RewardParameters(
    MintDistribution MintDistribution,
    TransactionFeeDistribution TransactionFeeDistribution,
    GasRewards GASRewards);