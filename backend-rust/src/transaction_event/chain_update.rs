use crate::scalar_types::DateTime;
use async_graphql::{SimpleObject, Union};
use concordium_rust_sdk::types::{UpdatePayload};
use serde::{Deserialize, Serialize};
use crate::address::AccountAddress;

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct Ratio {
    numerator: u64,
    denominator: u64,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct CommissionRange {
    min: f64,
    max: f64,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdateEnqueued {
    pub effective_time: DateTime,
    // effective_immediately: bool, // Not sure this makes sense.
    pub payload: bool,
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
    MinBlockTime(MinBlockTimeUpdate),
    TimeoutParameters(TimeoutParametersUpdate),
    FinalizationCommitteeParameters(FinalizationCommitteeParametersUpdate),
    BlockEnergyLimit(BlockEnergyLimitUpdate),
    GasRewardsCpv2(GasRewardsCpv2Update),
    ElectionDifficulty(ElectionDifficultyChainUpdatePayload),
    EuroPerEnergy(EuroPerEnergyChainUpdatePayload),
    MicroCcdPerEuro(MicroCcdPerEuroChainUpdatePayload),
    FoundationAccount(FoundationAccountChainUpdatePayload),
    MintDistribution(MintDistributionChainUpdatePayload),
    TransactionFeeDistribution(TransactionFeeDistributionChainUpdatePayload),
    GasRewards(GasRewardsChainUpdatePayload),
    BakerStakeThreshold(BakerStakeThresholdChainUpdatePayload),
//    RootKeys(RootKeysChainUpdatePayload),
//    Level1Keys(Level1KeysChainUpdatePayload),
    AddAnonymityRevoker(AddAnonymityRevokerChainUpdatePayload),
    AddIdentityProvider(AddIdentityProviderChainUpdatePayload),
    CooldownParameters(CooldownParametersChainUpdatePayload),
    PoolParameters(PoolParametersChainUpdatePayload),
    TimeParameters(TimeParametersChainUpdatePayload),
    MintDistributionV1(MintDistributionV1ChainUpdatePayload),
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct MinBlockTimeUpdate {
    pub duration_seconds: u64,
}

#[derive(Debug, Clone)]
pub struct ValidatorScoreParametersUpdate {
    pub maximum_missed_rounds: u64,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct TimeoutParametersUpdate {
    pub duration_seconds: u64,
    pub increase: Ratio,
    pub decrease: Ratio,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct FinalizationCommitteeParametersUpdate {
    pub min_finalizers: u32,
    pub max_finalizers: u32,
    pub finalizers_relative_stake_threshold: f64,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct BlockEnergyLimitUpdate {
    pub energy_limit: u64,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct GasRewardsCpv2Update {
    pub baker: f64,
    pub account_creation: f64,
    pub chain_update: f64,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct ProtocolChainUpdatePayload {
    pub message: String,
    pub specification_url: String,
    pub specification_hash: String,
    pub specification_auxiliary_data_hex: String,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct ElectionDifficultyChainUpdatePayload {
    pub election_difficulty: f64,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct EuroPerEnergyChainUpdatePayload {
    pub exchange_rate: Ratio,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct MicroCcdPerEuroChainUpdatePayload {
    pub exchange_rate: Ratio,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct FoundationAccountChainUpdatePayload {
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct MintDistributionChainUpdatePayload {
    pub mint_per_slot: f64,
    pub baking_reward: f64,
    pub finalization_reward: f64,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct TransactionFeeDistributionChainUpdatePayload {
    pub baker: f64,
    pub gas_account: f64,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct GasRewardsChainUpdatePayload {
    pub account_creation: f64,
    pub baker: f64,
    pub chain_update: f64,
    pub finalization_proof: f64,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct BakerStakeThresholdChainUpdatePayload {
    pub amount: u64,
}

//#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
//pub struct RootKeysChainUpdatePayload {
//    // No fields: this is used as a marker type.
//}
//
//#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
//pub struct Level1KeysChainUpdatePayload {
//    // No fields: this is used as a marker type.
//}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct AddAnonymityRevokerChainUpdatePayload {
    pub ar_identity: i32,
    pub name: String,
    pub url: String,
    pub description: String,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct AddIdentityProviderChainUpdatePayload {
    pub ip_identity: i32,
    pub name: String,
    pub url: String,
    pub description: String,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct CooldownParametersChainUpdatePayload {
    pub pool_owner_cooldown: u64,
    pub delegator_cooldown: u64,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct PoolParametersChainUpdatePayload {
    pub passive_finalization_commission: f64,
    pub passive_baking_commission: f64,
    pub passive_transaction_commission: f64,
    pub finalization_commission_range: CommissionRange,
    pub transaction_commission_range: CommissionRange,
    pub baking_commission_range: CommissionRange,
    pub transaction_commission_range_min: f64,
    pub transaction_commission_range_max: f64,
    pub minimum_equity_capital: u64,
    pub capital_bound: f64,
    pub leverage_bound: Ratio,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct TimeParametersChainUpdatePayload {
    pub reward_period_length: u64,
    pub mint_per_payday: f64,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct MintDistributionV1ChainUpdatePayload {
    pub baking_reward: f64,
    pub finalization_reward: f64,
}


/// Implement conversion from the Concordium SDK's ProtocolUpdate type.
impl From<UpdatePayload> for ChainUpdatePayload {
    fn from(payload: UpdatePayload) -> Self {
        ChainUpdatePayload::MinBlockTime(MinBlockTimeUpdate{
            duration_seconds: 0
        })
//        match payload {
//            UpdatePayload::Protocol(update) => ChainUpdatePayload::Protocol(ProtocolChainUpdatePayload {
//                message: update.message,
//                specification_url: update.specification_url,
//                specification_hash: update.specification_hash.to_string(),
//                specification_auxiliary_data_hex: hex::encode(update.specification_auxiliary_data).to_lowercase(),
//            }),
//            UpdatePayload::MinBlockTimeCPV2(update) => ChainUpdatePayload::MinBlockTime(MinBlockTimeUpdate{
//                duration_seconds: update.seconds()
//            }),
//            UpdatePayload::TimeoutParametersCPV2(update) => {
//                ChainUpdatePayload::TimeoutParameters(TimeoutParametersUpdate {
//                    duration_seconds: update.base.seconds(),
//                    increase: update.increase.into(), // Convert as needed
//                    decrease: update.decrease.into(),
//                })
//            },
//            UpdatePayload::FinalizationCommitteeParametersCPV2(update) => {
//                ChainUpdatePayload::FinalizationCommitteeParameters(FinalizationCommitteeParametersUpdate {
//                    min_finalizers: update.min_finalizers,
//                    max_finalizers: update.max_finalizers,
//                    finalizers_relative_stake_threshold: update.finalizers_relative_stake_threshold.into(),
//                })
//            },
//            UpdatePayload::BlockEnergyLimitCPV2(update) => {
//                ChainUpdatePayload::BlockEnergyLimit(BlockEnergyLimitUpdate {
//                    energy_limit: update.energy,
//                })
//            },
//            UpdatePayload::GASRewardsCPV2(update) => {
//                ChainUpdatePayload::GasRewardsCpv2(GasRewardsCpv2Update {
//                    baker: update.baker.into(),
//                    account_creation: update.account_creation.into(),
//                    chain_update: update.chain_update.into(),
//                })
//            },
//            UpdatePayload::ElectionDifficulty(update) => {
//                ChainUpdatePayload::ElectionDifficulty(ElectionDifficultyChainUpdatePayload {
//                    election_difficulty: update.into(),
//                })
//            },
//            UpdatePayload::EuroPerEnergy(update) => {
//                ChainUpdatePayload::EuroPerEnergy(EuroPerEnergyChainUpdatePayload {
//                    exchange_rate: update.into(),
//                })
//            },
//            UpdatePayload::MicroCcdPerEuro(update) => {
//                ChainUpdatePayload::MicroCcdPerEuro(MicroCcdPerEuroChainUpdatePayload {
//                    exchange_rate: update.exchange_rate.into(),
//                })
//            },
//            UpdatePayload::FoundationAccount(update) => {
//                ChainUpdatePayload::FoundationAccount(FoundationAccountChainUpdatePayload {
//                    account_address: update.account_address.to_string(),
//                })
//            },
//            UpdatePayload::MintDistribution(update) => {
//                ChainUpdatePayload::MintDistribution(MintDistributionChainUpdatePayload {
//                    mint_per_slot: update.mint_distribution.mint_per_slot.into(),
//                    baking_reward: update.mint_distribution.baking_reward.into(),
//                    finalization_reward: update.mint_distribution.finalization_reward.into(),
//                })
//            },
//            UpdatePayload::TransactionFeeDistribution(update) => {
//                ChainUpdatePayload::TransactionFeeDistribution(TransactionFeeDistributionChainUpdatePayload {
//                    baker: update.transaction_fee_distribution.baker.into(),
//                    gas_account: update.transaction_fee_distribution.gas_account.into(),
//                })
//            },
//            UpdatePayload::BakerStakeThreshold(update) => {
//                ChainUpdatePayload::BakerStakeThreshold(BakerStakeThresholdChainUpdatePayload {
//                    amount: update.minimum_threshold_for_baking.value,
//                })
//            },
//            UpdatePayload::Root(update) => {
//                ChainUpdatePayload::RootKeys(RootKeysChainUpdatePayload {})
//            },
//            UpdatePayload::Level1(update) => {
//                ChainUpdatePayload::Level1Keys(Level1KeysChainUpdatePayload {})
//            },
//            UpdatePayload::AddAnonymityRevoker(update) => {
//                ChainUpdatePayload::AddAnonymityRevoker(AddAnonymityRevokerChainUpdatePayload {
//                    ar_identity: update.ar_info.ar_identity.id as i32,
//                    name: update.ar_info.ar_description.name,
//                    url: update.ar_info.ar_description.url,
//                    description: update.ar_info.ar_description.info,
//                })
//            },
//            UpdatePayload::AddIdentityProvider(update) => {
//                ChainUpdatePayload::AddIdentityProvider(AddIdentityProviderChainUpdatePayload {
//                    ip_identity: update.ip_info.ip_identity.id as i32,
//                    name: update.ip_info.description.name,
//                    url: update.ip_info.description.url,
//                    description: update.ip_info.description.info,
//                })
//            },
//            UpdatePayload::CooldownParameters(update) => {
//                ChainUpdatePayload::CooldownParameters(CooldownParametersChainUpdatePayload {
//                    pool_owner_cooldown: update.cooldown_parameters.pool_owner_cooldown.as_secs(),
//                    delegator_cooldown: update.cooldown_parameters.delegator_cooldown.as_secs(),
//                })
//            },
//            UpdatePayload::PoolParameters(update) => {
//                ChainUpdatePayload::PoolParameters(PoolParametersChainUpdatePayload {
//                    passive_finalization_commission: update.pool_parameters.passive_finalization_commission.into(),
//                    passive_baking_commission: update.pool_parameters.passive_baking_commission.into(),
//                    passive_transaction_commission: update.pool_parameters.passive_transaction_commission.into(),
//                    finalization_commission_range_min: update.pool_parameters.commission_bounds.finalization.min.into(),
//                    finalization_commission_range_max: update.pool_parameters.commission_bounds.finalization.max.into(),
//                    baking_commission_range_min: update.pool_parameters.commission_bounds.baking.min.into(),
//                    baking_commission_range_max: update.pool_parameters.commission_bounds.baking.max.into(),
//                    transaction_commission_range_min: update.pool_parameters.commission_bounds.transaction.min.into(),
//                    transaction_commission_range_max: update.pool_parameters.commission_bounds.transaction.max.into(),
//                    minimum_equity_capital: update.pool_parameters.minimum_equity_capital.value,
//                    capital_bound: update.pool_parameters.capital_bound.bound.into(),
//                    leverage_bound: update.pool_parameters.leverage_bound.into(),
//                })
//            },
//            UpdatePayload::TimeParameters(update) => {
//                ChainUpdatePayload::TimeParameters(TimeParametersChainUpdatePayload {
//                    reward_period_length: update.time_parameters.reward_period_length.count(),
//                    mint_per_payday: update.time_parameters.mint_pr_payday.into(),
//                })
//            },
//            UpdatePayload::MintDistributionV1(update) => {
//                ChainUpdatePayload::MintDistributionV1(MintDistributionV1ChainUpdatePayload {
//                    value: update.value.into(), // Placeholder conversion
//                })
//            },
//
//            _ => {
//            }
//        }
    }
}