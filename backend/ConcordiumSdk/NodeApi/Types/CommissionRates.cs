namespace ConcordiumSdk.NodeApi.Types;

public record CommissionRates(
    decimal TransactionCommission,
    decimal FinalizationCommission,
    decimal BakingCommission);