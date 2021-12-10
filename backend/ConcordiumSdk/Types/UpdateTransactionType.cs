namespace ConcordiumSdk.Types;

/// <summary>
/// The specific numeric values are the values used for binary serialization when sending transactions.
/// Reference: At the time of writing this they are defined in "UpdatePayload"
/// (https://github.com/Concordium/concordium-base/blob/a50612e023da79cb625cd36c52703af6ed483738/haskell-src/Concordium/Types/Updates.hs#L797)
/// </summary>
public enum UpdateTransactionType  
{
    UpdateProtocol = 1,
    UpdateElectionDifficulty = 2,
    UpdateEuroPerEnergy = 3,
    UpdateMicroGtuPerEuro = 4,
    UpdateFoundationAccount = 5,
    UpdateMintDistribution = 6,
    UpdateTransactionFeeDistribution = 7,
    UpdateGasRewards = 8,
    UpdateBakerStakeThreshold = 9,
    UpdateAddAnonymityRevoker = 10,
    UpdateAddIdentityProvider = 11,
    UpdateRootKeys = 12,
    UpdateLevel1Keys = 13,
    UpdateLevel2Keys = 14,
}
