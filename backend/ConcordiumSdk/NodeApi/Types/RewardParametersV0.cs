namespace ConcordiumSdk.NodeApi.Types;

public record RewardParametersV0(
    MintDistributionV0 MintDistribution,
    TransactionFeeDistribution TransactionFeeDistribution,
    GasRewards GASRewards);