namespace ConcordiumSdk.NodeApi.Types;

public enum BalanceUpdateType
{
    FoundationReward,
    BakerReward,
    TransactionFeeReward,
    FinalizationReward,
    TransactionFee,
    AmountDecrypted,
    AmountEncrypted,
    TransferOut,
    TransferIn
}