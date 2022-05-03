using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record ChainParametersV1(
    decimal ElectionDifficulty,
    ExchangeRate EuroPerEnergy,
    ExchangeRate MicroGTUPerEuro,
    ulong PoolOwnerCooldown, // Number of seconds that pool owners must cooldown when reducing their equity capital or closing the pool.
    ulong DelegatorCooldown, // Number of seconds that a delegator must cooldown when reducing their delegated stake.
    ulong RewardPeriodLength,
    decimal MintPerPayday,
    ushort AccountCreationLimit,
    RewardParametersV1 RewardParameters,
    ulong FoundationAccountIndex,
    decimal FinalizationCommissionLPool,
    decimal BakingCommissionLPool,
    decimal TransactionCommissionLPool,
    InclusiveRange<decimal> FinalizationCommissionRange,
    InclusiveRange<decimal> BakingCommissionRange,
    InclusiveRange<decimal> TransactionCommissionRange,
    CcdAmount MinimumEquityCapital,
    decimal CapitalBound,
    LeverageFactor LeverageBound);