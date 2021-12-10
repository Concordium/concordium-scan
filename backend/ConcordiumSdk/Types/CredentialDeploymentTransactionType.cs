namespace ConcordiumSdk.Types;

/// <summary>
/// The specific numeric values are the values used for binary serialization when sending transactions.
/// Reference: At the time of writing this they are defined in "AccountCredentialWithProofs"
/// (https://github.com/Concordium/concordium-base/blob/main/haskell-src/Concordium/ID/Types.hs#L839)
/// </summary>
public enum CredentialDeploymentTransactionType
{
    Initial = 0,
    Normal = 1
}