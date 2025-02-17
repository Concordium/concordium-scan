use super::{baker, block::Block, contract, get_config, get_pool, todo_api, token, ApiError, ApiResult, ConnectionQuery};
use crate::graphql_api::account::Account;
use crate::graphql_api::node_status::NodeStatus;
use crate::graphql_api::transaction::Transaction;
use crate::{
    transaction_event::Event,
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType,
        CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use async_graphql::{connection, Context, Object};
use futures::TryStreamExt;
use regex::Regex;
use std::cmp::{max, min};
use std::str::FromStr;
use crate::connection::DescendingI64;

pub struct SearchResult {
    pub query: String,
}

#[Object]
impl SearchResult {
    async fn contracts<'a>(
        &self,
        _ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, contract::Contract>> {
        todo_api!()
    }

    //    async fn modules(
    //        &self,
    //        #[graphql(desc = "Returns the first _n_ elements from the list.")]
    // _first: Option<i32>,        #[graphql(desc = "Returns the elements in the
    //     list that come after the specified cursor.")]
    //        _after: Option<String>,
    //        #[graphql(desc = "Returns the last _n_ elements from the list.")]
    // _last: Option<i32>,        #[graphql(desc = "Returns the elements in the
    // list that come before the     specified cursor.")]
    //        _before: Option<String>,
    //    ) -> ApiResult<connection::Connection<String, Module>> {
    //        todo_api!()
    //    }

    async fn blocks(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Block>> {
        let block_hash_regex: Regex = Regex::new(r"^[a-fA-F0-9]{1,64}$")
            .map_err(|_| ApiError::InternalError("Invalid regex".to_string()))?;
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query =
            ConnectionQuery::<i64>::new(first, after, last, before, config.block_connection_limit)?;
        let mut connection = connection::Connection::new(false, false);
        if !block_hash_regex.is_match(&self.query) {
            return Ok(connection);
        }
        let lower_case_query = self.query.to_lowercase();
        let mut rows = sqlx::query_as!(
            Block,
            "SELECT * FROM (
                SELECT
                    hash,
                    height,
                    slot_time,
                    block_time,
                    finalization_time,
                    baker_id,
                    total_amount
                FROM blocks
                WHERE
                    height = $5
                    OR starts_with(hash, $6)
                    AND height > $1
                    AND height < $2
                ORDER BY
                    (CASE WHEN $4 THEN height END) ASC,
                    (CASE WHEN NOT $4 THEN height END) DESC
                LIMIT $3
            ) ORDER BY height DESC",
            query.from,
            query.to,
            query.limit,
            query.is_last,
            lower_case_query.parse::<i64>().ok(),
            lower_case_query
        )
            .fetch(pool);

        let mut min_height = None;
        let mut max_height = None;
        while let Some(block) = rows.try_next().await? {
            min_height = Some(match min_height {
                None => block.height,
                Some(current_min) => min(current_min, block.height),
            });

            max_height = Some(match max_height {
                None => block.height,
                Some(current_max) => max(current_max, block.height),
            });
            connection.edges.push(connection::Edge::new(block.height.to_string(), block));
        }

        if let (Some(page_min_height), Some(page_max_height)) = (min_height, max_height) {
            let result = sqlx::query!(
                r#"
                    SELECT MAX(height) as max_height, MIN(height) as min_height
                    FROM blocks
                    WHERE
                        height = $1
                        OR starts_with(hash, $2)
                "#,
                lower_case_query.parse::<i64>().ok(),
                lower_case_query,
            )
                .fetch_one(pool)
                .await?;
            connection.has_previous_page =
                result.max_height.map_or(false, |db_max| db_max > page_max_height);
            connection.has_next_page =
                result.min_height.map_or(false, |db_min| db_min < page_min_height);
        }
        Ok(connection)
    }

    async fn transactions(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        let transaction_hash_regex: Regex = Regex::new(r"^[a-fA-F0-9]{1,64}$")
            .map_err(|_| ApiError::InternalError("Invalid regex".to_string()))?;
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query =
            ConnectionQuery::<DescendingI64>::new(first, after, last, before, config.transaction_connection_limit)?;
        let mut connection = connection::Connection::new(false, false);
        if !transaction_hash_regex.is_match(&self.query) {
            return Ok(connection);
        }
        let lower_case_query = self.query.to_lowercase();
        let mut row_stream = sqlx::query_as!(
            Transaction,
            r#"SELECT * FROM (
                SELECT
                    index,
                    block_height,
                    hash,
                    ccd_cost,
                    energy_cost,
                    sender_index,
                    type as "tx_type: DbTransactionType",
                    type_account as "type_account: AccountTransactionType",
                    type_credential_deployment as "type_credential_deployment: CredentialDeploymentTransactionType",
                    type_update as "type_update: UpdateTransactionType",
                    success,
                    events as "events: sqlx::types::Json<Vec<Event>>",
                    reject as "reject: sqlx::types::Json<TransactionRejectReason>"
                FROM transactions
                WHERE
                    starts_with(hash, $5)
                    AND $2 < index
                    AND index < $1
                ORDER BY
                    (CASE WHEN $3 THEN index END) ASC,
                    (CASE WHEN NOT $3 THEN index END) DESC
                LIMIT $4
            ) ORDER BY index DESC"#,
            i64::from(query.from),
            i64::from(query.to),
            query.is_last,
            query.limit,
            lower_case_query
        )
        .fetch(pool);

        while let Some(tx) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(tx.index.to_string(), tx));
        }
        if let (Some(page_min), Some(page_max)) =
            (connection.edges.last(), connection.edges.first())
        {
            let result =
                sqlx::query!("SELECT MAX(index) as max_id, MIN(index) as min_id FROM transactions")
                    .fetch_one(pool)
                    .await?;
            connection.has_next_page =
                result.min_id.map_or(false, |db_min| db_min < page_min.node.index);
            connection.has_previous_page =
                result.max_id.map_or(false, |db_max| db_max > page_max.node.index);
        }
        Ok(connection)
    }

    async fn tokens(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, token::Token>> {
        todo_api!()
    }

    async fn accounts(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Account>> {
        let account_address_regex: Regex = Regex::new(r"^[1-9A-HJ-NP-Za-km-z]{1,50}$")
            .map_err(|_| ApiError::InternalError("Invalid regex".to_string()))?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(first, after, last, before, 10)?;
        let mut connection = connection::Connection::new(false, false);
        if !account_address_regex.is_match(&self.query) {
            return Ok(connection);
        }

        if let Ok(parsed_address) =
            concordium_rust_sdk::common::types::AccountAddress::from_str(&self.query)
        {
            if let Some(account) = sqlx::query_as!(
                Account,
                "SELECT
                index,
                transaction_index,
                address,
                amount,
                delegated_stake,
                num_txs,
                delegated_restake_earnings,
                delegated_target_baker_id
            FROM accounts
            WHERE
                address = $1",
                parsed_address.to_string()
            )
                .fetch_optional(pool)
                .await?
            {
                connection.edges.push(connection::Edge::new(account.index.to_string(), account));
            }
            return Ok(connection);
        };
        let accounts = sqlx::query_as!(
            Account,
            r#"
                SELECT * FROM (SELECT
                    index,
                    transaction_index,
                    address,
                    amount,
                    delegated_stake,
                    num_txs,
                    delegated_restake_earnings,
                    delegated_target_baker_id
                FROM accounts
                WHERE
                    address LIKE $5 || '%'
                    AND index > $1
                    AND index < $2
                ORDER BY
                    (CASE WHEN $4 THEN index END) DESC,
                    (CASE WHEN NOT $4 THEN index END) ASC
                LIMIT $3
                ) ORDER BY index ASC"#,
            query.from,
            query.to,
            query.limit,
            query.is_last,
            self.query
        )
            .fetch_all(pool)
            .await?;

        let mut min_index = None;
        let mut max_index = None;
        for account in accounts {
            min_index = Some(match min_index {
                None => account.index,
                Some(current_min) => min(current_min, account.index),
            });

            max_index = Some(match max_index {
                None => account.index,
                Some(current_max) => max(current_max, account.index),
            });
            connection.edges.push(connection::Edge::new(account.index.to_string(), account));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (min_index, max_index) {
            let result = sqlx::query!(
                r#"
                    SELECT MAX(index) as max_id, MIN(index) as min_id
                    FROM accounts
                    WHERE
                        address LIKE $1 || '%'
                "#,
                &self.query
            )
                .fetch_one(pool)
                .await?;

            connection.has_previous_page =
                result.min_id.map_or(false, |db_min| db_min < page_min_id);
            connection.has_next_page = result.max_id.map_or(false, |db_max| db_max > page_max_id);
        }
        Ok(connection)
    }

    async fn bakers(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, baker::Baker>> {
        todo_api!()
    }

    async fn node_statuses(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, NodeStatus>> {
        todo_api!()
    }
}
