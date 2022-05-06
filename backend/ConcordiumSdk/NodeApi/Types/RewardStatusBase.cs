using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public abstract record RewardStatusBase(
    CcdAmount TotalAmount,
    CcdAmount TotalEncryptedAmount,
    CcdAmount BakingRewardAccount,
    CcdAmount FinalizationRewardAccount,
    CcdAmount GasAccount);