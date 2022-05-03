namespace ConcordiumSdk.NodeApi.Types;

public record AuthorizationsV0(
    UpdatePublicKey[] Keys,
    AccessStructure Emergency,
    AccessStructure Protocol,
    AccessStructure ElectionDifficulty,
    AccessStructure EuroPerEnergy,
    AccessStructure MicroGTUPerEuro,
    AccessStructure FoundationAccount,
    AccessStructure MintDistribution,
    AccessStructure TransactionFeeDistribution,
    AccessStructure ParamGASRewards,
    AccessStructure BakerStakeThreshold,
    AccessStructure AddAnonymityRevoker,
    AccessStructure AddIdentityProvider);