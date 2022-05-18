using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

/// <param name="BakerId">The 'BakerId' of the pool owner</param>
/// <param name="BakerAddress">The account address of the pool owner.</param>
/// <param name="BakerEquityCapital">The equity capital provided by the pool owner.</param>
/// <param name="DelegatedCapital">The capital delegated to the pool by other accounts.</param>
/// <param name="DelegatedCapitalCap">The maximum amount that may be delegated to the pool, accounting for leverage and stake limits.</param>
/// <param name="PoolInfo">The pool info associated with the pool.</param>
/// <param name="AllPoolTotalCapital">Total capital staked across all pools.</param>
public record BakerPoolStatus(
    ulong BakerId,
    AccountAddress BakerAddress,
    CcdAmount BakerEquityCapital,
    CcdAmount DelegatedCapital,
    CcdAmount DelegatedCapitalCap,
    BakerPoolInfo PoolInfo,
    // TODO: baker_stake_pending_change: PoolPendingChange
    // TODO: current_payday_status:      Option<CurrentPaydayBakerPoolStatus>
    CcdAmount AllPoolTotalCapital);