use crate::{
    address::AccountAddress,
    graphql_api::{get_pool, ApiResult, InternalError},
    scalar_types::{AccountIndex, Amount, BakerId, Decimal},
};
use async_graphql::{ComplexObject, Context, Enum, SimpleObject};

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerAdded {
    pub staked_amount: Amount,
    pub restake_earnings: bool,
    pub baker_id: BakerId,
    pub sign_key: String,
    pub election_key: String,
    pub aggregation_key: String,
}
#[ComplexObject]
impl BakerAdded {
    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        account_address(&self.baker_id, ctx).await
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerKeysUpdated {
    pub baker_id: BakerId,
    pub sign_key: String,
    pub election_key: String,
    pub aggregation_key: String,
}
#[ComplexObject]
impl BakerKeysUpdated {
    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        account_address(&self.baker_id, ctx).await
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerRemoved {
    pub baker_id: BakerId,
}
#[ComplexObject]
impl BakerRemoved {
    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        account_address(&self.baker_id, ctx).await
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerSetRestakeEarnings {
    pub baker_id: BakerId,
    pub restake_earnings: bool,
}
#[ComplexObject]
impl BakerSetRestakeEarnings {
    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        account_address(&self.baker_id, ctx).await
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeDecreased {
    pub baker_id: BakerId,
    pub new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeDecreased {
    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        account_address(&self.baker_id, ctx).await
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeIncreased {
    pub baker_id: BakerId,
    pub new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeIncreased {
    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        account_address(&self.baker_id, ctx).await
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetBakingRewardCommission {
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
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
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
    pub transaction_fee_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetMetadataURL {
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
    pub metadata_url: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetOpenStatus {
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
    pub open_status: BakerPoolOpenStatus,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerDelegationRemoved {
    pub delegator_id: AccountIndex,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSuspended {
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerResumed {
    pub baker_id: BakerId,
    pub account_address: AccountAddress,
}

async fn account_address(baker_id: &BakerId, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
    let pool = get_pool(ctx)?;
    let address = sqlx::query_scalar!("SELECT address FROM accounts WHERE index = $1", baker_id.0)
        .fetch_one(pool)
        .await
        .map_err(|_| {
            InternalError::InternalError(format!("Unable to find account with index {}", baker_id))
        })?;
    Ok(AccountAddress::from(address))
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
