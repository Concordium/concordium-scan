namespace ConcordiumSdk.NodeApi.Types;

public record RewardParametersV1(
    MintDistributionV1 MintDistribution,
    TransactionFeeDistribution TransactionFeeDistribution,
    GasRewards GASRewards);