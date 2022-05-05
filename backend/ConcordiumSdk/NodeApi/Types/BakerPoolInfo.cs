namespace ConcordiumSdk.NodeApi.Types;

public record BakerPoolInfo(
    CommissionRates CommissionRates,
    BakerPoolOpenStatus OpenStatus,
    string MetadataUrl);