use crate::{
    address::AccountAddress,
    scalar_types::{DateTime, Decimal, UnsignedInt, UnsignedLong},
};
use async_graphql::{SimpleObject, Union};
use concordium_rust_sdk::types::{ExchangeRate, UpdatePayload};
use serde::{Deserialize, Serialize};

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct LeverageFactor {
    numerator:   UnsignedLong,
    denominator: UnsignedLong,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct Ratio {
    numerator:   UnsignedLong,
    denominator: UnsignedLong,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct CommissionRange {
    min: Decimal,
    max: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdateEnqueued {
    pub effective_time: DateTime,
    pub payload:        ChainUpdatePayload,
}

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
    MintDistributionCpv1(MintDistributionV1ChainUpdatePayload),
    TransactionFeeDistribution(TransactionFeeDistributionChainUpdatePayload),
    GasRewards(GasRewardsChainUpdatePayload),
    BakerStakeThreshold(BakerStakeThresholdChainUpdatePayload),
    RootKeys(RootKeysChainUpdatePayload),
    Level1Keys(Level1KeysChainUpdatePayload),
    AddAnonymityRevoker(AddAnonymityRevokerChainUpdatePayload),
    AddIdentityProvider(AddIdentityProviderChainUpdatePayload),
    CooldownParameters(CooldownParametersChainUpdatePayload),
    PoolParameters(PoolParametersChainUpdatePayload),
    TimeParameters(TimeParametersChainUpdatePayload),
    ValidatorScoreParameters(ValidatorScoreParametersUpdate),
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct MinBlockTimeUpdate {
    pub duration_seconds: UnsignedLong,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TimeoutParametersUpdate {
    pub duration_seconds: UnsignedLong,
    pub increase:         Ratio,
    pub decrease:         Ratio,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct FinalizationCommitteeParametersUpdate {
    pub min_finalizers: UnsignedInt,
    pub max_finalizers: UnsignedInt,
    pub finalizers_relative_stake_threshold: Decimal,
}

#[derive(SimpleObject, Debug, Clone, Serialize, Deserialize)]
pub struct BlockEnergyLimitUpdate {
    pub energy_limit: UnsignedLong,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct GasRewardsCpv2Update {
    pub baker:            Decimal,
    pub account_creation: Decimal,
    pub chain_update:     Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct ProtocolChainUpdatePayload {
    pub message: String,
    pub specification_url: String,
    pub specification_hash: String,
    pub specification_auxiliary_data_hex: String,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct ElectionDifficultyChainUpdatePayload {
    pub election_difficulty: Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct EuroPerEnergyChainUpdatePayload {
    pub exchange_rate: Ratio,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct MicroCcdPerEuroChainUpdatePayload {
    pub exchange_rate: Ratio,
}

#[derive(SimpleObject, Clone, Serialize, Deserialize)]
pub struct FoundationAccountChainUpdatePayload {
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct MintDistributionChainUpdatePayload {
    pub mint_per_slot:       Decimal,
    pub baking_reward:       Decimal,
    pub finalization_reward: Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TransactionFeeDistributionChainUpdatePayload {
    pub baker:       Decimal,
    pub gas_account: Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct GasRewardsChainUpdatePayload {
    account_creation:   Decimal,
    baker:              Decimal,
    chain_update:       Decimal,
    finalization_proof: Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct BakerStakeThresholdChainUpdatePayload {
    amount: UnsignedLong,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct RootKeysChainUpdatePayload {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct Level1KeysChainUpdatePayload {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct AddAnonymityRevokerChainUpdatePayload {
    pub ar_identity: u32,
    pub name:        String,
    pub url:         String,
    pub description: String,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct AddIdentityProviderChainUpdatePayload {
    pub ip_identity: u32,
    pub name:        String,
    pub url:         String,
    pub description: String,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct CooldownParametersChainUpdatePayload {
    pub pool_owner_cooldown: UnsignedLong,
    pub delegator_cooldown:  UnsignedLong,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct PoolParametersChainUpdatePayload {
    pub passive_finalization_commission: Decimal,
    pub passive_baking_commission:       Decimal,
    pub passive_transaction_commission:  Decimal,
    pub finalization_commission_range:   CommissionRange,
    pub transaction_commission_range:    CommissionRange,
    pub baking_commission_range:         CommissionRange,
    pub minimum_equity_capital:          UnsignedLong,
    pub capital_bound:                   Decimal,
    pub leverage_bound:                  LeverageFactor,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TimeParametersChainUpdatePayload {
    pub reward_period_length: UnsignedLong,
    pub mint_per_payday:      Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct MintDistributionV1ChainUpdatePayload {
    pub baking_reward:       Decimal,
    pub finalization_reward: Decimal,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct ValidatorScoreParametersUpdate {
    maximum_missed_rounds: UnsignedLong,
}

/// Implement conversion from the Concordium SDK's UpdatePayload type to the
/// internally owned ChainUpdatePayload
impl From<UpdatePayload> for ChainUpdatePayload {
    fn from(payload: UpdatePayload) -> Self {
        match payload {
            UpdatePayload::AddAnonymityRevoker(update) => {
                ChainUpdatePayload::AddAnonymityRevoker(AddAnonymityRevokerChainUpdatePayload {
                    ar_identity: update.ar_identity.into(),
                    name:        update.ar_description.name,
                    url:         update.ar_description.url,
                    description: update.ar_description.description,
                })
            }
            UpdatePayload::AddIdentityProvider(update) => {
                ChainUpdatePayload::AddIdentityProvider(AddIdentityProviderChainUpdatePayload {
                    ip_identity: update.ip_identity.0,
                    name:        update.ip_description.name,
                    url:         update.ip_description.url,
                    description: update.ip_description.description,
                })
            }
            UpdatePayload::BakerStakeThreshold(update) => {
                ChainUpdatePayload::BakerStakeThreshold(BakerStakeThresholdChainUpdatePayload {
                    amount: UnsignedLong(update.minimum_threshold_for_baking.micro_ccd),
                })
            }
            UpdatePayload::BlockEnergyLimitCPV2(update) => {
                ChainUpdatePayload::BlockEnergyLimit(BlockEnergyLimitUpdate {
                    energy_limit: UnsignedLong(update.energy),
                })
            }
            UpdatePayload::CooldownParametersCPV1(update) => {
                ChainUpdatePayload::CooldownParameters(CooldownParametersChainUpdatePayload {
                    pool_owner_cooldown: UnsignedLong(update.pool_owner_cooldown.seconds),
                    delegator_cooldown:  UnsignedLong(update.delegator_cooldown.seconds),
                })
            }
            UpdatePayload::ElectionDifficulty(update) => {
                ChainUpdatePayload::ElectionDifficulty(ElectionDifficultyChainUpdatePayload {
                    election_difficulty: Decimal(update.into()),
                })
            }
            UpdatePayload::EuroPerEnergy(update) => {
                ChainUpdatePayload::EuroPerEnergy(EuroPerEnergyChainUpdatePayload {
                    exchange_rate: update.into(),
                })
            }
            UpdatePayload::FinalizationCommitteeParametersCPV2(update) => {
                ChainUpdatePayload::FinalizationCommitteeParameters(
                    FinalizationCommitteeParametersUpdate {
                        min_finalizers: UnsignedInt(update.min_finalizers),
                        max_finalizers: UnsignedInt(update.max_finalizers),
                        finalizers_relative_stake_threshold: Decimal(
                            update.finalizers_relative_stake_threshold.into(),
                        ),
                    },
                )
            }
            UpdatePayload::FoundationAccount(update) => {
                ChainUpdatePayload::FoundationAccount(FoundationAccountChainUpdatePayload {
                    account_address: update.into(),
                })
            }
            UpdatePayload::GASRewards(update) => {
                ChainUpdatePayload::GasRewards(GasRewardsChainUpdatePayload {
                    baker:              update.baker.into(),
                    account_creation:   update.account_creation.into(),
                    chain_update:       update.chain_update.into(),
                    finalization_proof: update.finalization_proof.into(),
                })
            }
            UpdatePayload::GASRewardsCPV2(update) => {
                ChainUpdatePayload::GasRewardsCpv2(GasRewardsCpv2Update {
                    baker:            update.baker.into(),
                    account_creation: update.account_creation.into(),
                    chain_update:     update.chain_update.into(),
                })
            }
            UpdatePayload::Level1(_) => {
                ChainUpdatePayload::Level1Keys(Level1KeysChainUpdatePayload {
                    dummy: true,
                })
            }
            UpdatePayload::MicroGTUPerEuro(update) => {
                ChainUpdatePayload::MicroCcdPerEuro(MicroCcdPerEuroChainUpdatePayload {
                    exchange_rate: update.into(),
                })
            }
            UpdatePayload::MinBlockTimeCPV2(update) => {
                ChainUpdatePayload::MinBlockTime(MinBlockTimeUpdate {
                    duration_seconds: UnsignedLong(update.seconds()),
                })
            }
            UpdatePayload::MintDistribution(update) => {
                ChainUpdatePayload::MintDistribution(MintDistributionChainUpdatePayload {
                    mint_per_slot:       Decimal(rust_decimal::Decimal::new(
                        update.mint_per_slot.mantissa as i64,
                        update.mint_per_slot.exponent as u32,
                    )),
                    baking_reward:       update.baking_reward.into(),
                    finalization_reward: update.finalization_reward.into(),
                })
            }
            UpdatePayload::MintDistributionCPV1(update) => {
                ChainUpdatePayload::MintDistributionCpv1(MintDistributionV1ChainUpdatePayload {
                    finalization_reward: update.finalization_reward.into(),
                    baking_reward:       update.baking_reward.into(),
                })
            }
            UpdatePayload::PoolParametersCPV1(update) => {
                ChainUpdatePayload::PoolParameters(PoolParametersChainUpdatePayload {
                    passive_finalization_commission: update.passive_finalization_commission.into(),
                    passive_baking_commission:       update.passive_baking_commission.into(),
                    passive_transaction_commission:  update.passive_transaction_commission.into(),
                    baking_commission_range:         CommissionRange {
                        max: update.commission_bounds.baking.min.into(),
                        min: update.commission_bounds.baking.max.into(),
                    },
                    finalization_commission_range:   CommissionRange {
                        max: update.commission_bounds.finalization.min.into(),
                        min: update.commission_bounds.finalization.max.into(),
                    },
                    transaction_commission_range:    CommissionRange {
                        max: update.commission_bounds.transaction.min.into(),
                        min: update.commission_bounds.transaction.max.into(),
                    },
                    minimum_equity_capital:          UnsignedLong(
                        update.minimum_equity_capital.micro_ccd,
                    ),
                    capital_bound:                   update.capital_bound.bound.into(),
                    leverage_bound:                  LeverageFactor {
                        denominator: UnsignedLong(update.leverage_bound.denominator),
                        numerator:   UnsignedLong(update.leverage_bound.numerator),
                    },
                })
            }
            UpdatePayload::Protocol(update) => {
                ChainUpdatePayload::Protocol(ProtocolChainUpdatePayload {
                    message: update.message,
                    specification_url: update.specification_url,
                    specification_hash: update.specification_hash.to_string(),
                    specification_auxiliary_data_hex: hex::encode(
                        update.specification_auxiliary_data,
                    )
                    .to_lowercase(),
                })
            }
            UpdatePayload::Root(_) => ChainUpdatePayload::RootKeys(RootKeysChainUpdatePayload {
                dummy: true,
            }),
            UpdatePayload::TimeoutParametersCPV2(update) => {
                ChainUpdatePayload::TimeoutParameters(TimeoutParametersUpdate {
                    duration_seconds: UnsignedLong(update.base.seconds()),
                    increase:         update.increase.into(),
                    decrease:         update.decrease.into(),
                })
            }
            UpdatePayload::TimeParametersCPV1(update) => {
                ChainUpdatePayload::TimeParameters(TimeParametersChainUpdatePayload {
                    reward_period_length: UnsignedLong(
                        update.reward_period_length.reward_period_epochs().epoch,
                    ),
                    mint_per_payday:      Decimal(rust_decimal::Decimal::new(
                        update.mint_per_payday.mantissa as i64,
                        update.mint_per_payday.exponent as u32,
                    )),
                })
            }
            UpdatePayload::TransactionFeeDistribution(update) => {
                ChainUpdatePayload::TransactionFeeDistribution(
                    TransactionFeeDistributionChainUpdatePayload {
                        baker:       update.baker.into(),
                        gas_account: update.gas_account.into(),
                    },
                )
            }
            UpdatePayload::ValidatorScoreParametersCPV3(update) => {
                ChainUpdatePayload::ValidatorScoreParameters(ValidatorScoreParametersUpdate {
                    maximum_missed_rounds: UnsignedLong(update.max_missed_rounds),
                })
            }
        }
    }
}

impl From<concordium_rust_sdk::common::types::Ratio> for Ratio {
    fn from(ratio: concordium_rust_sdk::common::types::Ratio) -> Self {
        Ratio {
            denominator: UnsignedLong(ratio.denominator()),
            numerator:   UnsignedLong(ratio.numerator()),
        }
    }
}
impl From<ExchangeRate> for Ratio {
    fn from(rate: ExchangeRate) -> Self {
        Ratio {
            denominator: UnsignedLong(rate.denominator()),
            numerator:   UnsignedLong(rate.numerator()),
        }
    }
}
