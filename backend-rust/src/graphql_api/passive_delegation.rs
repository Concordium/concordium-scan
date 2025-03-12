use super::{
    baker::{DelegationSummary, PaydayPoolReward},
    get_config, get_pool, ApiResult,
};
use crate::{
    connection::{ConnectionQuery, DescendingI64},
    graphql_api::todo_api,
};
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
    ) -> ApiResult<connection::Connection<String, PaydayPoolReward>> {
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
            connection.edges.push(connection::Edge::new(rewards.block_height.to_string(), rewards));
        }
        if let Some(page_max_index) = connection.edges.first() {
            if let Some(max_index) = sqlx::query_scalar!(
                "SELECT MAX(payday_block_height) 
                    FROM bakers_payday_pool_rewards 
                    WHERE pool_owner_for_primary_key = -1"
            )
            .fetch_one(pool)
            .await?
            {
                connection.has_previous_page = max_index > page_max_index.node.block_height;
            }
        }
        if let Some(edge) = connection.edges.last() {
            connection.has_next_page = edge.node.block_height != 0;
        }
        Ok(connection)
    }

    // Query:
    // delegators(
    //     after: $afterDelegators
    //     before: $beforeDelegators
    //     first: $firstDelegators
    //     last: $lastDelegators
    //   ) {
    //     nodes {
    //       accountAddress {
    //         asString
    //         __typename
    //       }
    //       stakedAmount
    //       restakeEarnings
    //       __typename
    //     }
    //     pageInfo {
    //       hasNextPage
    //       hasPreviousPage
    //       startCursor
    //       endCursor
    //       __typename
    //     }
    //     __typename
    //   }
    // Schema:
    // delegators("Returns the first _n_ elements from the list." first: Int
    // "Returns the elements in the list that come after the specified cursor."
    // after: String "Returns the last _n_ elements from the list." last: Int
    // "Returns the elements in the list that come before the specified cursor."
    // before: String): DelegatorsConnection
    async fn delegators(
        &self,
        _ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, DelegationSummary>> {
        todo_api!()
    }
}
