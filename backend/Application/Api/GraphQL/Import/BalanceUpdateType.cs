namespace Application.Api.GraphQL.Import;

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
