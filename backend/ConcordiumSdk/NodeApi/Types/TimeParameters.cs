namespace ConcordiumSdk.NodeApi.Types;

public record TimeParameters(
    ulong RewardPeriodLength,
    decimal MintPerPayday);