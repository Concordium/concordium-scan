use crate::{
    address::AccountAddress,
    graphql_api::{todo_api, ApiResult},
    scalar_types::{AccountIndex, Amount, BakerId, Decimal},
};
use async_graphql::{ComplexObject, Context, Enum, SimpleObject};

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerAdded {
    pub staked_amount:    Amount,
    pub restake_earnings: bool,
    pub baker_id:         BakerId,
    pub sign_key:         String,
    pub election_key:     String,
    pub aggregation_key:  String,
}
#[ComplexObject]
impl BakerAdded {
    async fn account_address(&self, _ctx: &Context<'_>) -> ApiResult<AccountAddress> { todo_api!() }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerKeysUpdated {
    pub baker_id:        BakerId,
    pub sign_key:        String,
    pub election_key:    String,
    pub aggregation_key: String,
}
#[ComplexObject]
impl BakerKeysUpdated {
    async fn account_address(&self, _ctx: &Context<'_>) -> ApiResult<AccountAddress> { todo_api!() }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerRemoved {
    pub baker_id: BakerId,
}
#[ComplexObject]
impl BakerRemoved {
    async fn account_address(&self, _ctx: &Context<'_>) -> ApiResult<AccountAddress> { todo_api!() }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerSetRestakeEarnings {
    pub baker_id:         BakerId,
    pub restake_earnings: bool,
}
#[ComplexObject]
impl BakerSetRestakeEarnings {
    async fn account_address(&self, _ctx: &Context<'_>) -> ApiResult<AccountAddress> { todo_api!() }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeDecreased {
    pub baker_id:          BakerId,
    pub new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeDecreased {
    async fn account_address(&self, _ctx: &Context<'_>) -> ApiResult<AccountAddress> { todo_api!() }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeIncreased {
    pub baker_id:          BakerId,
    pub new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeIncreased {
    async fn account_address(&self, _ctx: &Context<'_>) -> ApiResult<AccountAddress> { todo_api!() }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetBakingRewardCommission {
    pub baker_id:                 BakerId,
    pub account_address:          AccountAddress,
    pub baking_reward_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetFinalizationRewardCommission {
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
    pub finalization_reward_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetTransactionFeeCommission {
    pub baker_id:                   BakerId,
    pub account_address:            AccountAddress,
    pub transaction_fee_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetMetadataURL {
    pub baker_id:        BakerId,
    pub account_address: AccountAddress,
    pub metadata_url:    String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetOpenStatus {
    pub baker_id:        BakerId,
    pub account_address: AccountAddress,
    pub open_status:     BakerPoolOpenStatus,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerDelegationRemoved {
    pub delegator_id:    AccountIndex,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSuspended {
    pub baker_id:        BakerId,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerResumed {
    pub baker_id:        BakerId,
    pub account_address: AccountAddress,
}

#[derive(
    Debug, Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize, sqlx::Type,
)]
#[sqlx(type_name = "pool_open_status")] // only for PostgreSQL to match a type definition
pub enum BakerPoolOpenStatus {
    OpenForAll,
    ClosedForNew,
    ClosedForAll,
}

impl From<concordium_rust_sdk::types::OpenStatus> for BakerPoolOpenStatus {
    fn from(status: concordium_rust_sdk::types::OpenStatus) -> Self {
        use concordium_rust_sdk::types::OpenStatus;
        match status {
            OpenStatus::OpenForAll => Self::OpenForAll,
            OpenStatus::ClosedForNew => Self::ClosedForNew,
            OpenStatus::ClosedForAll => Self::ClosedForAll,
        }
    }
}

pub struct PaydayPoolRewardAmounts {
    // The total amount in microCCD (baker + delegators).
    pub total_amount:      u64,
    // The bakers share of the above total reward in microCCD.
    pub baker_amount:      u64,
    // The delegators share of the above total reward in microCCD.
    pub delegators_amount: u64,
}

impl PaydayPoolRewardAmounts {
    pub fn new() -> Self {
        PaydayPoolRewardAmounts {
            total_amount:      0u64,
            baker_amount:      0u64,
            delegators_amount: 0u64,
        }
    }
}

pub struct PaydayPoolRewards {
    pub transaction_fees:   PaydayPoolRewardAmounts,
    pub block_finalization: PaydayPoolRewardAmounts,
    pub block_baking:       PaydayPoolRewardAmounts,
}

impl PaydayPoolRewards {
    pub fn new() -> Self {
        PaydayPoolRewards {
            transaction_fees:   PaydayPoolRewardAmounts::new(),
            block_finalization: PaydayPoolRewardAmounts::new(),
            block_baking:       PaydayPoolRewardAmounts::new(),
        }
    }
}
