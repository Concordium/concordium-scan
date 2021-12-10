namespace ConcordiumSdk.Types;

/// The specific numeric values are the values used for binary serialization when sending transactions.
/// Reference: At the time of writing this they are defined in "BlockItemKind"
/// (https://github.com/Concordium/concordium-base/blob/a50612e023da79cb625cd36c52703af6ed483738/haskell-src/Concordium/Types/Transactions.hs#L303)
public enum BlockItemKind
{
    AccountTransactionKind = 0,
    CredentialDeploymentKind = 1,
    UpdateInstructionKind = 2
}