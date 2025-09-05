use super::{block::Block, get_pool, ApiResult, InternalError};
use crate::{
    address::AccountAddress,
    scalar_types::{Amount, BlockHeight, DateTime, Decimal},
};
use async_graphql::{Context, Object, SimpleObject};

#[derive(SimpleObject)]
pub struct CommissionRates {
    pub transaction_commission: Option<Decimal>,
    pub finalization_commission: Option<Decimal>,
    pub baking_commission: Option<Decimal>,
}

#[derive(SimpleObject, Clone)]
pub struct PaydayPoolRewardAmounts {
    // The total amount in microCCD (baker + delegators).
    pub total_amount: u64,
    // The bakers share of the above total reward in microCCD.
    pub baker_amount: u64,
    // The delegators share of the above total reward in microCCD.
    pub delegators_amount: u64,
}

pub struct PaydayPoolReward {
    pub block_height: BlockHeight,
    pub slot_time: DateTime,
    pub pool_owner: Option<i64>,
    pub total_transaction_rewards: i64,
    pub delegators_transaction_rewards: i64,
    pub total_baking_rewards: i64,
    pub delegators_baking_rewards: i64,
    pub total_finalization_rewards: i64,
    pub delegators_finalization_rewards: i64,
}

#[Object]
impl PaydayPoolReward {
    async fn id(&self) -> BlockHeight {
        self.block_height
    }

    async fn block<'a>(&self, ctx: &Context<'a>) -> ApiResult<Block> {
        Block::query_by_height(get_pool(ctx)?, self.block_height).await
    }

    async fn pool_owner(&self) -> Option<i64> {
        self.pool_owner
    }

    async fn timestamp(&self) -> DateTime {
        self.slot_time
    }

    async fn transaction_fees(&self) -> ApiResult<PaydayPoolRewardAmounts> {
        Ok(PaydayPoolRewardAmounts {
            total_amount: self.total_transaction_rewards.try_into()?,
            baker_amount: (self.total_transaction_rewards - self.delegators_transaction_rewards)
                .try_into()?,
            delegators_amount: self.delegators_transaction_rewards.try_into()?,
        })
    }

    async fn baker_reward(&self) -> ApiResult<PaydayPoolRewardAmounts> {
        Ok(PaydayPoolRewardAmounts {
            total_amount: self.total_baking_rewards.try_into()?,
            baker_amount: (self.total_baking_rewards - self.delegators_baking_rewards)
                .try_into()?,
            delegators_amount: self.delegators_baking_rewards.try_into()?,
        })
    }

    async fn finalization_reward(&self) -> ApiResult<PaydayPoolRewardAmounts> {
        Ok(PaydayPoolRewardAmounts {
            total_amount: self.total_finalization_rewards.try_into()?,
            baker_amount: (self.total_finalization_rewards - self.delegators_finalization_rewards)
                .try_into()?,
            delegators_amount: self.delegators_finalization_rewards.try_into()?,
        })
    }
}

pub struct DelegationSummary {
    pub index: i64,
    pub account_address: AccountAddress,
    pub staked_amount: i64,
    pub restake_earnings: Option<bool>,
}

#[Object]
impl DelegationSummary {
    async fn account_address(&self) -> &AccountAddress {
        &self.account_address
    }

    async fn staked_amount(&self) -> ApiResult<Amount> {
        self.staked_amount.try_into().map_err(|_| {
            InternalError::InternalError(
                "Staked amount in database should be a valid UnsignedLong".to_string(),
            )
            .into()
        })
    }

    async fn restake_earnings(&self) -> ApiResult<bool> {
        self.restake_earnings.ok_or_else(|| {
            InternalError::InternalError(
                "Delegator should have a boolean in the `restake_earnings` variable.".to_string(),
            )
            .into()
        })
    }
}
