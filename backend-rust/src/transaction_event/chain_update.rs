use crate::scalar_types::DateTime;
use async_graphql::{SimpleObject, Union};
use concordium_rust_sdk::types::UpdatePayload;

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdateEnqueued {
    pub effective_time: DateTime,
    // effective_immediately: bool, // Not sure this makes sense.
    pub payload: ChainUpdatePayload,
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
#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum ChainUpdatePayload {
    Protocol(ProtocolChainUpdatePayload),
    MinBlockTime(MinBlockTimeUpdate)
}

#[derive(SimpleObject, Debug, Clone, serde::Serialize, serde::Deserialize)]
pub struct MinBlockTimeUpdate {
    pub duration_seconds: u64,
}

#[derive(Debug, Clone)]
pub struct ValidatorScoreParametersUpdate {
    pub maximum_missed_rounds: u64,
}

#[derive(SimpleObject, Debug, Clone, serde::Serialize, serde::Deserialize)]
pub struct ProtocolChainUpdatePayload {
    pub message: String,
    pub specification_url: String,
    pub specification_hash: String,
    pub specification_auxiliary_data_hex: String,
}

/// Implement conversion from the Concordium SDK's ProtocolUpdate type.
impl From<UpdatePayload> for ChainUpdatePayload {
    fn from(payload: UpdatePayload) -> Self {
        match payload {
            UpdatePayload::Protocol(update) => ChainUpdatePayload::Protocol(ProtocolChainUpdatePayload {
                message: update.message,
                specification_url: update.specification_url,
                specification_hash: update.specification_hash.to_string(), // TODO: Validate this
                specification_auxiliary_data_hex: hex::encode(update.specification_auxiliary_data).to_lowercase(),
            }),
            UpdatePayload::MinBlockTimeCPV2(update) => ChainUpdatePayload::MinBlockTime(MinBlockTimeUpdate{
                duration_seconds: update.seconds()
            }),
            _ => todo!()
        }
    }
}