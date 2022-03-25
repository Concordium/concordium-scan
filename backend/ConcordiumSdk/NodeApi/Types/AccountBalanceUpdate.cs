using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record AccountBalanceUpdate(
    AccountAddress AccountAddress, 
    long AmountAdjustment, 
    BalanceUpdateType BalanceUpdateType,
    TransactionHash? TransactionHash = null);
