namespace ConcordiumSdk.NodeApi.Types;

public enum BalanceUpdateType
{
    BakingReward,
    BlockReward,
    FinalizationReward,
    MintReward,
    TransactionFee,
    AmountDecrypted,
    AmountEncrypted,
    TransferOut,
    TransferIn
}