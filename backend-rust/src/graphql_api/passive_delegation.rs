use super::{
    baker_and_delegator_types::{DelegationSummary, PaydayPoolReward},
    get_config, get_pool, ApiResult,
};
use crate::connection::{ConnectionQuery, DescendingI64};
use async_graphql::{connection, Context, Object};
use futures::TryStreamExt;

#[derive(Default)]
pub struct QueryPassiveDelegation;

#[Object]
impl QueryPassiveDelegation {
    async fn passive_delegation<'a>(&self, _ctx: &Context<'a>) -> ApiResult<PassiveDelegation> {
        Ok(PassiveDelegation {})
    }
}

pub struct PassiveDelegation {
    // commissionRates:  CommissionRates!
    //
    // delegatorCount: Int!
    //
    // "The total amount staked by delegators to passive delegation."
    // delegatedStake: UnsignedLong!
    //
    // "Total stake passively delegated as a percentage of all CCDs in existence."
    // delegatedStakePercentage: Decimal!
    //
    // Query:
    // apy7days: apy(period: LAST7_DAYS)
    // apy30days: apy(period: LAST30_DAYS)
    // Schema:
    // apy(period: ApyPeriod!): Float
}

#[Object]
impl PassiveDelegation {
    async fn pool_rewards(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<DescendingI64, PaydayPoolReward>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.pool_rewards_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            PaydayPoolReward,
            "SELECT * FROM (
                SELECT
                    payday_block_height as block_height,
                    slot_time,
                    pool_owner,
                    payday_total_transaction_rewards as total_transaction_rewards,
                    payday_delegators_transaction_rewards as delegators_transaction_rewards,
                    payday_total_baking_rewards as total_baking_rewards,
                    payday_delegators_baking_rewards as delegators_baking_rewards,
                    payday_total_finalization_rewards as total_finalization_rewards,
                    payday_delegators_finalization_rewards as delegators_finalization_rewards
                FROM bakers_payday_pool_rewards
                    JOIN blocks ON blocks.height = payday_block_height
                WHERE pool_owner_for_primary_key = -1
                    AND payday_block_height > $2 AND payday_block_height < $1
                ORDER BY
                    (CASE WHEN $4 THEN payday_block_height END) ASC,
                    (CASE WHEN NOT $4 THEN payday_block_height END) DESC
                LIMIT $3
                ) AS rewards
            ORDER BY rewards.block_height DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);
        while let Some(rewards) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(rewards.block_height.into(), rewards));
        }

        if let (Some(edge_min_index), Some(edge_max_index)) =
            (connection.edges.last(), connection.edges.first())
        {
            let result = sqlx::query!(
                "
                    SELECT 
                        MIN(payday_block_height) as min_index,
                        MAX(payday_block_height) as max_index
                    FROM bakers_payday_pool_rewards
                    WHERE pool_owner_for_primary_key = -1
                "
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.max_index.map_or(false, |db_max| db_max > edge_max_index.node.block_height);
            connection.has_next_page =
                result.min_index.map_or(false, |db_min| db_min < edge_min_index.node.block_height);
        }

        Ok(connection)
    }

    // Passive delegators are sorted descending by `staked_amount`.
    async fn delegators(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<DescendingI64, DelegationSummary>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.delegators_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            DelegationSummary,
            "SELECT * FROM (
                SELECT
                    index,
                    address as account_address,
                    delegated_restake_earnings as restake_earnings,
                    delegated_stake as staked_amount
                FROM accounts
                WHERE delegated_target_baker_id IS NULL AND
                    accounts.index > $2 AND accounts.index < $1
                ORDER BY
                    (CASE WHEN $4 THEN accounts.index END) ASC,
                    (CASE WHEN NOT $4 THEN accounts.index END) DESC
                LIMIT $3
            ) AS delegators
            ORDER BY delegators.staked_amount DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        while let Some(delegator) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(delegator.index.into(), delegator));
        }

        if let (Some(edge_min_index), Some(edge_max_index)) =
            (connection.edges.last(), connection.edges.first())
        {
            let result = sqlx::query!(
                "
                SELECT 
                    MAX(index) as min_index,
                    MIN(index) as max_index
                FROM accounts 
                WHERE delegated_target_baker_id IS NULL
            "
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.max_index.map_or(false, |db_max| db_max > edge_max_index.node.index);
            connection.has_next_page =
                result.min_index.map_or(false, |db_min| db_min < edge_min_index.node.index);
        }

        Ok(connection)
    }
}
