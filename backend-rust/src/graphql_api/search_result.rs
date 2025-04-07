use super::{
    baker::{self, Baker},
    block::Block,
    contract::{self, Contract, ContractSnapshot},
    get_config, get_pool, todo_api, token, ApiError, ApiResult, ConnectionQuery,
};
use crate::{
    connection::DescendingI64,
    graphql_api::{
        account::Account, baker::CurrentBaker, node_status::NodeStatus, transaction::Transaction,
    },
    transaction_event::Event,
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use async_graphql::{
    connection::{self, CursorType},
    Context, Object,
};
use futures::TryStreamExt;
use regex::Regex;
use std::str::FromStr;

pub struct SearchResult {
    pub query: String,
}

#[Object]
impl SearchResult {
    async fn contracts<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, contract::Contract>> {
        let contract_index_regex: Regex = Regex::new("^[0-9]+$")
            .map_err(|e| ApiError::InternalError(format!("Invalid regex: {}", e)))?;
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.contract_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);
        if !contract_index_regex.is_match(&self.query) {
            return Ok(connection);
        }
        let lower_case_query = self.query.to_lowercase();

        let mut row_stream = sqlx::query!(
            "SELECT * FROM (
                SELECT
                    contracts.index as index,
                    sub_index,
                    module_reference,
                    name as contract_name,
                    contracts.amount,
                    blocks.slot_time as block_slot_time,
                    transactions.block_height,
                    transactions.hash as transaction_hash,
                    accounts.address as creator
                FROM contracts
                    JOIN transactions ON transaction_index = transactions.index
                    JOIN blocks ON transactions.block_height = blocks.height
                    JOIN accounts ON transactions.sender_index = accounts.index
                WHERE 
                    contracts.index = $5 AND
                    contracts.index > $1 AND 
                    contracts.index < $2
                ORDER BY
                    (CASE WHEN $4 THEN contracts.index END) ASC,
                    (CASE WHEN NOT $4 THEN contracts.index END) DESC
                LIMIT $3
            ) AS contract_data
            ORDER BY contract_data.index DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last,
            lower_case_query.parse::<i64>().ok(),
        )
        .fetch(pool);

        while let Some(row) = row_stream.try_next().await? {
            let contract_address_index =
                row.index.try_into().map_err(|e: <u64 as TryFrom<i64>>::Error| {
                    ApiError::InternalError(e.to_string())
                })?;
            let contract_address_sub_index =
                row.sub_index.try_into().map_err(|e: <u64 as TryFrom<i64>>::Error| {
                    ApiError::InternalError(e.to_string())
                })?;

            let snapshot = ContractSnapshot {
                block_height: row.block_height,
                contract_address_index,
                contract_address_sub_index,
                contract_name: row.contract_name,
                module_reference: row.module_reference,
                amount: row.amount.try_into()?,
            };

            let contract = Contract {
                contract_address_index,
                contract_address_sub_index,
                contract_address: format!(
                    "<{},{}>",
                    contract_address_index, contract_address_sub_index
                ),
                creator: row.creator.into(),
                block_height: row.block_height,
                transaction_hash: row.transaction_hash,
                block_slot_time: row.block_slot_time,
                snapshot,
            };

            connection
                .edges
                .push(connection::Edge::new(contract.contract_address_index.to_string(), contract));
        }

        if let (Some(page_min_id), Some(page_max_id)) =
            (connection.edges.first(), connection.edges.last())
        {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as db_max_index, MIN(index) as db_min_index
                    FROM contracts
                    WHERE contracts.index = $1
                ",
                lower_case_query.parse::<i64>().ok()
            )
            .fetch_one(pool)
            .await?;

            let page_max: i64 =
                page_max_id.node.contract_address_index.0.try_into().map_err(|e| {
                    ApiError::InternalError(format!("A contract index is too large: {}", e))
                })?;
            let page_min: i64 =
                page_min_id.node.contract_address_index.0.try_into().map_err(|e| {
                    ApiError::InternalError(format!("A contract index is too large: {}", e))
                })?;

            connection.has_previous_page =
                result.db_max_index.map_or(false, |db_max| db_max > page_max);
            connection.has_next_page =
                result.db_min_index.map_or(false, |db_min| db_min < page_min);
        }

        Ok(connection)
    }

    // async fn modules(
    //     &self,
    //     #[graphql(desc = "Returns the first _n_ elements from the list.")]
    // _first: Option<i32>,     #[graphql(desc = "Returns the elements in the
    // list that come after the specified cursor.")]     _after: Option<String>,
    //     #[graphql(desc = "Returns the last _n_ elements from the list.")] _last:
    // Option<i32>,     #[graphql(desc = "Returns the elements in the list that
    // come before the     specified cursor.")]     _before: Option<String>,
    // ) -> ApiResult<connection::Connection<String, Module>> {
    //     todo_api!()
    // }

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

        while let Some(block) = rows.try_next().await? {
            connection.edges.push(connection::Edge::new(block.height.to_string(), block));
        }

        if let (Some(page_min_height), Some(page_max_height)) =
            (connection.edges.first(), connection.edges.last())
        {
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
                result.max_height.map_or(false, |db_max| db_max > page_max_height.node.height);
            connection.has_next_page =
                result.min_height.map_or(false, |db_min| db_min < page_min_height.node.height);
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
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.transaction_connection_limit,
        )?;
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

        for account in accounts {
            connection.edges.push(connection::Edge::new(account.index.to_string(), account));
        }

        if let (Some(page_min_id), Some(page_max_id)) =
            (connection.edges.first(), connection.edges.last())
        {
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
                result.min_id.map_or(false, |db_min| db_min < page_min_id.node.index);
            connection.has_next_page =
                result.max_id.map_or(false, |db_max| db_max > page_max_id.node.index);
        }
        Ok(connection)
    }

    async fn bakers(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, baker::Baker>> {
        let baker_index_regex: Regex = Regex::new("^[0-9]+$")
            .map_err(|e| ApiError::InternalError(format!("Invalid regex: {}", e)))?;
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query =
            ConnectionQuery::<i64>::new(first, after, last, before, config.baker_connection_limit)?;
        let mut connection = connection::Connection::new(false, false);
        if !baker_index_regex.is_match(&self.query) {
            return Ok(connection);
        }
        let lower_case_query = self.query.to_lowercase();

        let mut row_stream = sqlx::query_as!(
            CurrentBaker,
            r#"SELECT * FROM (
                SELECT
                    bakers.id AS id,
                    staked,
                    restake_earnings,
                    open_status as "open_status: _",
                    metadata_url,
                    self_suspended,
                    inactive_suspended,
                    primed_for_suspension,
                    transaction_commission,
                    baking_commission,
                    finalization_commission,
                    payday_transaction_commission as "payday_transaction_commission?",
                    payday_baking_commission as "payday_baking_commission?",
                    payday_finalization_commission as "payday_finalization_commission?",
                    payday_lottery_power as "payday_lottery_power?",
                    payday_ranking_by_lottery_powers as "payday_ranking_by_lottery_powers?",
                    (SELECT MAX(payday_ranking_by_lottery_powers) FROM bakers_payday_lottery_powers) as "payday_total_ranking_by_lottery_powers?",
                    pool_total_staked,
                    pool_delegator_count,
                    baker_apy,
                    delegators_apy
                FROM bakers
                    LEFT JOIN latest_baker_apy_30_days
                        ON latest_baker_apy_30_days.id = bakers.id
                    LEFT JOIN bakers_payday_commission_rates
                        ON bakers_payday_commission_rates.id = bakers.id
                    LEFT JOIN bakers_payday_lottery_powers
                        ON bakers_payday_lottery_powers.id = bakers.id
                WHERE
                    bakers.id = $5 AND
                    (bakers.id > $1 AND 
                    bakers.id < $2)
                ORDER BY
                    (CASE WHEN $3     THEN bakers.id END) DESC,
                    (CASE WHEN NOT $3 THEN bakers.id END) ASC
                LIMIT $4
            ) ORDER BY id ASC"#,
            query.from,                                        // $1
            query.to,                                          // $2
            query.is_last,                                     // $3
            query.limit,                                       // $4
            lower_case_query.parse::<i64>().ok()               // $5
        )
        .fetch(pool);
        while let Some(row) = row_stream.try_next().await? {
            let cursor = row.id.encode_cursor();
            connection.edges.push(connection::Edge::new(cursor, Baker::Current(Box::new(row))));
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return.
            return Ok(connection);
        };

        {
            let bounds = sqlx::query!(
                "SELECT
                    MAX(id),
                    MIN(id)
                FROM bakers
                WHERE
                    bakers.id = $1
                ",
                lower_case_query.parse::<i64>().ok()
            )
            .fetch_one(pool)
            .await?;
            if let (Some(min), Some(max)) = (bounds.min, bounds.max) {
                connection.has_previous_page = min < first_item.node.get_id();
                connection.has_next_page = max > last_item.node.get_id();
            }
        }
        Ok(connection)
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
