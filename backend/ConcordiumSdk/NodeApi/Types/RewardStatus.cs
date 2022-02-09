using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

/// <summary>
/// Balance statistics (at a given block height)
/// </summary>
/// <param name="TotalAmount">The total CCD in existence</param>
/// <param name="TotalEncryptedAmount">The total CCD in encrypted balances</param>
/// <param name="BakingRewardAccount">The amount in the baking reward account</param>
/// <param name="FinalizationRewardAccount">The amount in the finalization reward account</param>
/// <param name="GasAccount">The amount in the GAS account</param>
public record RewardStatus(
    CcdAmount TotalAmount,
    CcdAmount TotalEncryptedAmount,
    CcdAmount BakingRewardAccount,
    CcdAmount FinalizationRewardAccount,
    CcdAmount GasAccount
);