namespace ConcordiumSdk.Types;

/// <summary>
/// The specific numeric values are the values used for binary serialization when sending transactions.
/// Reference: At the time of writing this they are defined in "Payload"
/// (https://github.com/Concordium/concordium-base/blob/a50612e023da79cb625cd36c52703af6ed483738/haskell-src/Concordium/Types/Execution.hs#L273)
/// </summary>
public enum AccountTransactionType 
{
    DeployModule = 0,
    InitializeSmartContractInstance = 1,
    UpdateSmartContractInstance = 2,
    SimpleTransfer = 3,
    AddBaker = 4,
    RemoveBaker = 5,
    UpdateBakerStake = 6,
    UpdateBakerRestakeEarnings = 7,
    UpdateBakerKeys = 8,
    UpdateCredentialKeys = 13,
    EncryptedTransfer = 16,
    TransferToEncrypted = 17,
    TransferToPublic = 18,
    TransferWithSchedule = 19,
    UpdateCredentials = 20,
    RegisterData = 21,
    SimpleTransferWithMemo = 22,
    EncryptedTransferWithMemo = 23,
    TransferWithScheduleWithMemo = 24,
    ConfigureBaker = 25,
    ConfigureDelegation = 26,
}
