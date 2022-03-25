namespace Application.Api.GraphQL;

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
    BakingReward = 6,
    BlockReward = 7,
    FinalizationReward = 8,
    MintReward = 9,
}