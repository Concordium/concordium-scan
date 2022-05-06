namespace ConcordiumSdk.NodeApi.Types;

/// <summary>
/// Parameters related to staking pools.
/// </summary>
/// <param name="PassiveFinalizationCommission">Fraction of finalization rewards charged by the passive delegation.</param>
/// <param name="PassiveBakingCommission">Fraction of baking rewards charged by the passive delegation.</param>
/// <param name="PassiveTransactionCommission">Fraction of transaction rewards charged by the passive delegation.</param>
/// <param name="FinalizationCommissionRange">The range of allowed finalization commissions.</param>
/// <param name="BakingCommissionRange">The range of allowed baker commissions.</param>
/// <param name="TransactionCommissionRange">The range of allowed transaction commissions.</param>
/// <param name="MinimumEquityCapital">Minimum equity capital required for a new baker.</param>
/// <param name="CapitalBound">Maximum fraction of the total staked capital of that a new baker can have.</param>
/// <param name="LeverageBound">The maximum leverage that a baker can have as a ratio of total stake to equity capital.</param>
public record PoolParameters(
    decimal PassiveFinalizationCommission,
    decimal PassiveBakingCommission,
    decimal PassiveTransactionCommission,
    InclusiveRange<decimal> FinalizationCommissionRange,
    InclusiveRange<decimal> BakingCommissionRange,
    InclusiveRange<decimal> TransactionCommissionRange,
    ulong MinimumEquityCapital,
    decimal CapitalBound,
    LeverageFactor LeverageBound);