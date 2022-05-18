using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

/// <param name="DelegatedCapital">The total capital delegated passively.</param>
/// <param name="CommissionRates">The passive delegation commission rates.</param>
/// <param name="CurrentPaydayTransactionFeesEarned">The transaction fees accruing to the passive delegators in the current reward period.</param>
/// <param name="CurrentPaydayDelegatedCapital">The effective delegated capital to the passive delegators for the current reward period.</param>
/// <param name="AllPoolTotalCapital">Total capital staked across all pools, including passive delegation.</param>
public record PoolStatusPassiveDelegation (
    CcdAmount DelegatedCapital,
    CommissionRates CommissionRates,
    CcdAmount CurrentPaydayTransactionFeesEarned,
    CcdAmount CurrentPaydayDelegatedCapital,
    CcdAmount AllPoolTotalCapital);