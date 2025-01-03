use crate::scalar_types::DateTime;
use async_graphql::SimpleObject;

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdateEnqueued {
    pub effective_time: DateTime,
    // effective_immediately: bool, // Not sure this makes sense.
    pub payload:        bool, // ChainUpdatePayload,
}

// union ChainUpdatePayload = MinBlockTimeUpdate | TimeoutParametersUpdate |
// FinalizationCommitteeParametersUpdate | BlockEnergyLimitUpdate |
// GasRewardsCpv2Update | ProtocolChainUpdatePayload |
// ElectionDifficultyChainUpdatePayload | EuroPerEnergyChainUpdatePayload |
// MicroCcdPerEuroChainUpdatePayload | FoundationAccountChainUpdatePayload |
// MintDistributionChainUpdatePayload |
// TransactionFeeDistributionChainUpdatePayload | GasRewardsChainUpdatePayload |
// BakerStakeThresholdChainUpdatePayload | RootKeysChainUpdatePayload |
// Level1KeysChainUpdatePayload | AddAnonymityRevokerChainUpdatePayload |
// AddIdentityProviderChainUpdatePayload | CooldownParametersChainUpdatePayload
// | PoolParametersChainUpdatePayload | TimeParametersChainUpdatePayload |
// MintDistributionV1ChainUpdatePayload
#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdatePayload {
    todo: bool,
}
