use crate::{
    address::AccountAddress,
    connection::connection_from_slice,
    graphql_api::ApiResult,
    scalar_types::{Amount, BakerId},
};
use async_graphql::{connection, types, Object, SimpleObject, Union};
use concordium_rust_sdk::{
    common::types as sdk_common_types, id::types as sdk_id_types, types::SpecialTransactionOutcome,
};

#[derive(Union, serde::Serialize, serde::Deserialize)]
#[allow(clippy::enum_variant_names)]
pub enum SpecialEvent {
    MintSpecialEvent(MintSpecialEvent),
    FinalizationRewardsSpecialEvent(FinalizationRewardsSpecialEvent),
    BlockRewardsSpecialEvent(BlockRewardsSpecialEvent),
    BakingRewardsSpecialEvent(BakingRewardsSpecialEvent),
    PaydayAccountRewardSpecialEvent(PaydayAccountRewardSpecialEvent),
    BlockAccrueRewardSpecialEvent(BlockAccrueRewardSpecialEvent),
    PaydayFoundationRewardSpecialEvent(PaydayFoundationRewardSpecialEvent),
    PaydayPoolRewardSpecialEvent(PaydayPoolRewardSpecialEvent),
    ValidatorSuspended(ValidatorSuspended),
    ValidatorPrimedForSuspension(ValidatorPrimedForSuspension),
}

impl SpecialEvent {
    pub fn from_special_transaction_outcome(
        block_height: i64,
        block_outcome_index: i64,
        outcome: SpecialTransactionOutcome,
    ) -> anyhow::Result<SpecialEvent> {
        let id = types::ID::from(format!("{}:{}", block_height, block_outcome_index));
        match outcome {
            SpecialTransactionOutcome::BakingRewards {
                baker_rewards,
                remainder,
            } => Ok(Self::BakingRewardsSpecialEvent(BakingRewardsSpecialEvent {
                id,
                remainder: remainder.into(),
                rewards: baker_rewards
                    .into_iter()
                    .map(|reward| reward.into())
                    .collect(),
            })),
            SpecialTransactionOutcome::Mint {
                mint_baking_reward,
                mint_finalization_reward,
                mint_platform_development_charge,
                foundation_account,
            } => Ok(Self::MintSpecialEvent(MintSpecialEvent {
                id,
                baking_reward: mint_baking_reward.into(),
                finalization_reward: mint_finalization_reward.into(),
                platform_development_charge: mint_platform_development_charge.into(),
                foundation_account_address: foundation_account.into(),
            })),
            SpecialTransactionOutcome::FinalizationRewards {
                finalization_rewards,
                remainder,
            } => Ok(Self::FinalizationRewardsSpecialEvent(
                FinalizationRewardsSpecialEvent {
                    id,
                    rewards: finalization_rewards
                        .into_iter()
                        .map(|reward| reward.into())
                        .collect(),
                    remainder: remainder.into(),
                },
            )),
            SpecialTransactionOutcome::BlockReward {
                transaction_fees,
                old_gas_account,
                new_gas_account,
                baker_reward,
                foundation_charge,
                baker,
                foundation_account,
            } => Ok(Self::BlockRewardsSpecialEvent(BlockRewardsSpecialEvent {
                id,
                transaction_fees: transaction_fees.into(),
                old_gas_account: old_gas_account.into(),
                new_gas_account: new_gas_account.into(),
                baker_reward: baker_reward.into(),
                foundation_charge: foundation_charge.into(),
                baker_account_address: baker.into(),
                foundation_account_address: foundation_account.into(),
            })),
            SpecialTransactionOutcome::PaydayFoundationReward {
                foundation_account,
                development_charge,
            } => Ok(Self::PaydayFoundationRewardSpecialEvent(
                PaydayFoundationRewardSpecialEvent {
                    id,
                    foundation_account: foundation_account.into(),
                    development_charge: development_charge.into(),
                },
            )),
            SpecialTransactionOutcome::PaydayAccountReward {
                account,
                transaction_fees,
                baker_reward,
                finalization_reward,
            } => Ok(Self::PaydayAccountRewardSpecialEvent(
                PaydayAccountRewardSpecialEvent {
                    id,
                    account: account.into(),
                    transaction_fees: transaction_fees.into(),
                    baker_reward: baker_reward.into(),
                    finalization_reward: finalization_reward.into(),
                },
            )),
            SpecialTransactionOutcome::BlockAccrueReward {
                transaction_fees,
                old_gas_account,
                new_gas_account,
                baker_reward,
                passive_reward,
                foundation_charge,
                baker_id,
            } => Ok(Self::BlockAccrueRewardSpecialEvent(
                BlockAccrueRewardSpecialEvent {
                    transaction_fees: transaction_fees.into(),
                    old_gas_account: old_gas_account.into(),
                    new_gas_account: new_gas_account.into(),
                    baker_reward: baker_reward.into(),
                    passive_reward: passive_reward.into(),
                    foundation_charge: foundation_charge.into(),
                    baker_id: baker_id.try_into()?,
                    id,
                },
            )),
            SpecialTransactionOutcome::PaydayPoolReward {
                pool_owner,
                transaction_fees,
                baker_reward,
                finalization_reward,
            } => {
                let pool = match pool_owner {
                    Some(baker_id) => {
                        PoolRewardTarget::BakerPoolRewardTarget(BakerPoolRewardTarget {
                            baker_id: baker_id.try_into()?,
                        })
                    }
                    None => PoolRewardTarget::PassiveDelegationPoolRewardTarget(
                        PassiveDelegationPoolRewardTarget::default(),
                    ),
                };
                Ok(Self::PaydayPoolRewardSpecialEvent(
                    PaydayPoolRewardSpecialEvent {
                        pool,
                        transaction_fees: transaction_fees.into(),
                        baker_reward: baker_reward.into(),
                        finalization_reward: finalization_reward.into(),
                        id,
                    },
                ))
            }
            SpecialTransactionOutcome::ValidatorSuspended { baker_id, account } => {
                Ok(Self::ValidatorSuspended(ValidatorSuspended {
                    baker_id: baker_id.try_into()?,
                    account: account.into(),
                }))
            }
            SpecialTransactionOutcome::ValidatorPrimedForSuspension { baker_id, account } => Ok(
                Self::ValidatorPrimedForSuspension(ValidatorPrimedForSuspension {
                    baker_id: baker_id.try_into()?,
                    account: account.into(),
                }),
            ),
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct MintSpecialEvent {
    id: types::ID,
    baking_reward: Amount,
    finalization_reward: Amount,
    platform_development_charge: Amount,
    foundation_account_address: AccountAddress,
}

#[derive(serde::Serialize, serde::Deserialize)]
pub struct FinalizationRewardsSpecialEvent {
    id: types::ID,
    remainder: Amount,
    rewards: Vec<AccountAddressAmount>,
}

#[Object]
impl FinalizationRewardsSpecialEvent {
    async fn id(&self) -> &types::ID {
        &self.id
    }

    async fn remainder(&self) -> Amount {
        self.remainder
    }

    async fn finalization_rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<usize>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountAddressAmount>> {
        connection_from_slice(self.rewards.as_slice(), first, after, last, before)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BlockRewardsSpecialEvent {
    transaction_fees: Amount,
    old_gas_account: Amount,
    new_gas_account: Amount,
    baker_reward: Amount,
    foundation_charge: Amount,
    baker_account_address: AccountAddress,
    foundation_account_address: AccountAddress,
    id: types::ID,
}
#[derive(serde::Serialize, serde::Deserialize)]
pub struct BakingRewardsSpecialEvent {
    id: types::ID,
    remainder: Amount,
    rewards: Vec<AccountAddressAmount>,
}
#[Object]
impl BakingRewardsSpecialEvent {
    async fn id(&self) -> &types::ID {
        &self.id
    }

    async fn remainder(&self) -> Amount {
        self.remainder
    }

    async fn baking_rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<usize>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountAddressAmount>> {
        connection_from_slice(self.rewards.as_slice(), first, after, last, before)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PaydayAccountRewardSpecialEvent {
    id: types::ID,
    /// The account that got rewarded.
    account: AccountAddress,
    /// The transaction fee reward at payday to the account.
    transaction_fees: Amount,
    /// The baking reward at payday to the account.
    baker_reward: Amount,
    /// The finalization reward at payday to the account.
    finalization_reward: Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BlockAccrueRewardSpecialEvent {
    id: types::ID,
    /// The total fees paid for transactions in the block.
    transaction_fees: Amount,
    /// The old balance of the GAS account.
    old_gas_account: Amount,
    /// The new balance of the GAS account.
    new_gas_account: Amount,
    /// The amount awarded to the baker.
    baker_reward: Amount,
    /// The amount awarded to the passive delegators.
    passive_reward: Amount,
    /// The amount awarded to the foundation.
    foundation_charge: Amount,
    /// The baker of the block, who will receive the award.
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PaydayFoundationRewardSpecialEvent {
    id: types::ID,
    foundation_account: AccountAddress,
    development_charge: Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PaydayPoolRewardSpecialEvent {
    id: types::ID,
    /// The pool awarded.
    pool: PoolRewardTarget,
    /// Accrued transaction fees for pool.
    transaction_fees: Amount,
    /// Accrued baking rewards for pool.
    baker_reward: Amount,
    /// Accrued finalization rewards for pool.
    finalization_reward: Amount,
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
enum PoolRewardTarget {
    PassiveDelegationPoolRewardTarget(PassiveDelegationPoolRewardTarget),
    BakerPoolRewardTarget(BakerPoolRewardTarget),
}

#[derive(SimpleObject, Default, serde::Serialize, serde::Deserialize)]
struct PassiveDelegationPoolRewardTarget {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerPoolRewardTarget {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
struct AccountAddressAmount {
    account_address: AccountAddress,
    amount: Amount,
}

impl From<(sdk_id_types::AccountAddress, sdk_common_types::Amount)> for AccountAddressAmount {
    fn from((address, amount): (sdk_id_types::AccountAddress, sdk_common_types::Amount)) -> Self {
        Self {
            account_address: address.to_owned().into(),
            amount: amount.micro_ccd().into(),
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ValidatorSuspended {
    baker_id: BakerId,
    account: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ValidatorPrimedForSuspension {
    baker_id: BakerId,
    account: AccountAddress,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, sqlx::Type, async_graphql::Enum)]
#[sqlx(type_name = "special_transaction_outcome_type")]
pub enum SpecialEventTypeFilter {
    Mint,
    FinalizationRewards,
    BlockRewards,
    BakingRewards,
    PaydayAccountReward,
    BlockAccrueReward,
    PaydayFoundationReward,
    PaydayPoolReward,
    ValidatorSuspended,
    ValidatorPrimedForSuspension,
}

impl From<&concordium_rust_sdk::types::SpecialTransactionOutcome> for SpecialEventTypeFilter {
    fn from(value: &concordium_rust_sdk::types::SpecialTransactionOutcome) -> Self {
        use concordium_rust_sdk::types::SpecialTransactionOutcome as O;
        match value {
            O::BakingRewards { .. } => Self::BakingRewards,
            O::Mint { .. } => Self::Mint,
            O::FinalizationRewards { .. } => Self::FinalizationRewards,
            O::BlockReward { .. } => Self::BlockRewards,
            O::PaydayFoundationReward { .. } => Self::PaydayFoundationReward,
            O::PaydayAccountReward { .. } => Self::PaydayAccountReward,
            O::BlockAccrueReward { .. } => Self::BlockAccrueReward,
            O::PaydayPoolReward { .. } => Self::PaydayPoolReward,
            O::ValidatorSuspended { .. } => Self::ValidatorSuspended,
            O::ValidatorPrimedForSuspension { .. } => Self::ValidatorPrimedForSuspension,
        }
    }
}
