namespace Application.Api.GraphQL.Accounts;

/// <summary>
/// NOTE:   Specific assigned values are important for reading and writing to database.
///         Should not be modified without consideration!
/// </summary>
public enum AccountStatementEntryType
{
    TransferIn = 1,
    TransferOut = 2,
    AmountDecrypted = 3,
    AmountEncrypted = 4,
    TransactionFee = 5,
    FinalizationReward = 6,
    FoundationReward = 7,
    BakerReward = 8,
    TransactionFeeReward = 9
}