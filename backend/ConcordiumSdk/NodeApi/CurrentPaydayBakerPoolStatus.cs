using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

public record CurrentPaydayBakerPoolStatus(
    ulong BlocksBaked,
    bool FinalizationLive,
    CcdAmount TransactionFeesEarned,
    CcdAmount EffectiveStake,
    decimal LotteryPower,
    CcdAmount BakerEquityCapital,
    CcdAmount DelegatedCapital);