use super::{
    baker::{self, Baker},
    block::Block,
    contract::{self, Contract, ContractSnapshot},
    db, get_config, get_pool,
    module_reference_event::ModuleReferenceEvent,
    node_status::NodeInfoReceiver,
    plt::{PltToken, PltTokenParams},
    token::Token,
    ApiResult, ConnectionQuery, InternalError,
};
use crate::{
    connection::{connection_from_slice, DescendingI64, NestedCursor},
    graphql_api::{
        account::Account, baker::CurrentBaker, node_status::NodeStatus, transaction::Transaction,
    },
    scalar_types::TokenId,
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
use concordium_rust_sdk::base::contracts_common::schema::VersionedModuleSchema;
use futures::TryStreamExt;
use regex::Regex;
use sqlx::{pool::PoolConnection, Postgres};
use std::{borrow::Cow, str::FromStr, sync::LazyLock};

pub struct SearchResult {
    pub query: String,
}

/// Minimum query length we accept for searching for hashes by prefix.
/// Currently, this matches the number of characters in the short display of
/// hashes in CCD scan UI.
const MIN_HASH_QUERY_LENGTH: usize = 6;

/// Query string that can be applied to columns containing hash values using
/// the condition `starts_with(hash_column, HASH_DUMMY_QUERY)` and which will
/// make the condition always be false.
///
/// Should be combined with `force_custom_plan` (<https://www.postgresql.org/docs/current/sql-prepare.html>),
/// such that Postgres will be able to see that the condition is almost
/// certainly false via its bucket statistics and hence choose a plan that takes
/// this into account.
const HASH_DUMMY_QUERY: &str = "$";

/// Regular expression matching 256-bit hash in hexadecimal representation.
static HASH_256_REGEX: LazyLock<Regex> =
    LazyLock::new(|| Regex::new(r"^[a-fA-F0-9]{1,64}$").expect("invalid regex"));

#[Object]
impl SearchResult {
    async fn contracts<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, contract::Contract>> {
        let contract_index_regex: Regex = Regex::new("^[0-9]+$")
            .map_err(|e| InternalError::InternalError(format!("Invalid regex: {}", e)))?;
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
                    contracts.index = $5 OR 
                    starts_with(contracts.index_text, $6) AND      
                    contracts.index > $1 AND 
                    contracts.index < $2
                ORDER BY
                    (CASE WHEN $4 THEN contracts.index END) ASC,
                    (CASE WHEN NOT $4 THEN contracts.index END) DESC
                LIMIT $3
            ) AS contract_data
            ORDER BY contract_data.index DESC",
            i64::from(query.from),                // $1
            i64::from(query.to),                  // $2
            query.limit,                          // $3
            query.is_last,                        // $4
            lower_case_query.parse::<i64>().ok(), // $5
            lower_case_query                      // $6
        )
        .fetch(pool);

        while let Some(row) = row_stream.try_next().await? {
            let contract_address_index =
                row.index
                    .try_into()
                    .map_err(|e: <u64 as TryFrom<i64>>::Error| {
                        InternalError::InternalError(e.to_string())
                    })?;
            let contract_address_sub_index =
                row.sub_index
                    .try_into()
                    .map_err(|e: <u64 as TryFrom<i64>>::Error| {
                        InternalError::InternalError(e.to_string())
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

            connection.edges.push(connection::Edge::new(
                contract.contract_address_index.to_string(),
                contract,
            ));
        }

        if let (Some(page_min_id), Some(page_max_id)) =
            (connection.edges.first(), connection.edges.last())
        {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as db_max_index, MIN(index) as db_min_index
                    FROM contracts
                    WHERE 
                        contracts.index = $1 OR 
                        starts_with(contracts.index_text, $2)
                ",
                lower_case_query.parse::<i64>().ok(), // $1
                lower_case_query                      // $2
            )
            .fetch_one(pool)
            .await?;

            let page_max: i64 = page_max_id
                .node
                .contract_address_index
                .0
                .try_into()
                .map_err(|e| {
                    InternalError::InternalError(format!("A contract index is too large: {}", e))
                })?;
            let page_min: i64 = page_min_id
                .node
                .contract_address_index
                .0
                .try_into()
                .map_err(|e| {
                    InternalError::InternalError(format!("A contract index is too large: {}", e))
                })?;

            connection.has_previous_page =
                result.db_max_index.is_some_and(|db_max| db_max > page_max);
            connection.has_next_page = result.db_min_index.is_some_and(|db_min| db_min < page_min);
        }

        Ok(connection)
    }

    async fn modules(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, ModuleReferenceEvent>> {
        let mut connection = connection::Connection::new(false, false);

        let module_hash_regex: Regex = Regex::new(r"^[a-fA-F0-9]{1,64}$")
            .map_err(|_| InternalError::InternalError("Invalid regex".to_string()))?;
        if !module_hash_regex.is_match(&self.query) {
            return Ok(connection);
        }
        let lower_case_query = self.query.to_lowercase();

        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;

        // We use the `NestedCursor<Outer, Inner>` for modules,
        // where the outer cursor is the `block_height` that the module was deployed in,
        // and the inner cursor is the `transaction_index` that the module
        // was deployed in.
        type Cursor = NestedCursor<i64, i64>;

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.module_connection_limit,
        )?;

        let mut rows = sqlx::query!(
            "SELECT * FROM (
                SELECT
                    module_reference,
                    blocks.height as block_height,
                    smart_contract_modules.transaction_index as transaction_index,
                    schema as display_schema,
                    blocks.slot_time as block_slot_time,
                    transactions.hash as transaction_hash,
                    accounts.address as sender
                FROM smart_contract_modules
                    JOIN transactions ON smart_contract_modules.transaction_index = \
             transactions.index
                    JOIN blocks ON transactions.block_height = blocks.height
                    JOIN accounts ON transactions.sender_index = accounts.index
                WHERE
                    starts_with(module_reference, $7)
                    AND
                    (
                        (block_height > $1
                            AND block_height < $2
                        )
                        -- When outer bounds are not equal, filter separate for each inner bound.
                        OR (
                            $1 != $2
                            AND (
                                -- Start inner bound for page.
                                (block_height = $1 AND transactions.index < $3)
                                -- End inner bound for page.
                                OR (block_height = $2 AND transactions.index > $4)
                            )
                        )
                        -- When outer bounds are equal, use one filter for both bounds.
                        OR (
                            $1 = $2
                            AND block_height = $1
                            AND transactions.index < $3 AND transactions.index > $4
                        )
                    )
                ORDER BY
                    (CASE WHEN $6     THEN block_height END) ASC,
                    (CASE WHEN $6     THEN transactions.index END) ASC,
                    (CASE WHEN NOT $6 THEN block_height END) DESC,
                    (CASE WHEN NOT $6 THEN transactions.index END) DESC
                LIMIT $5
            ) as sub
                ORDER BY sub.block_height DESC, sub.transaction_index DESC",
            query.from.outer, // $1
            query.to.outer,   // $2
            query.from.inner, // $3
            query.to.inner,   // $4
            query.limit,      // $5
            query.is_last,    // $6
            lower_case_query  // $7
        )
        .fetch(pool);

        while let Some(module) = rows.try_next().await? {
            let cursor = NestedCursor {
                inner: module.transaction_index,
                outer: module.block_height,
            };

            let display_schema = module
                .display_schema
                .as_ref()
                .map(|s| VersionedModuleSchema::new(s, &None).map(|schema| schema.to_string()))
                .transpose()?;

            connection.edges.push(connection::Edge::new(
                cursor.encode_cursor(),
                ModuleReferenceEvent {
                    module_reference: module.module_reference,
                    sender: module.sender.into(),
                    block_height: module.block_height,
                    transaction_hash: module.transaction_hash,
                    transaction_index: module.transaction_index,
                    block_slot_time: module.block_slot_time,
                    display_schema,
                },
            ));
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };

        let collection_ends = sqlx::query!(
            "WITH
                starting_module as (
                    SELECT
                        blocks.height as block_height,
                        smart_contract_modules.transaction_index as transaction_index
                    FROM smart_contract_modules
                        JOIN transactions ON smart_contract_modules.transaction_index = \
             transactions.index
                        JOIN blocks ON transactions.block_height = blocks.height
                    WHERE starts_with(module_reference, $1)
                    ORDER BY block_height DESC, transaction_index DESC
                    LIMIT 1
                ),
                ending_module as (
                    SELECT
                        blocks.height as block_height,
                        smart_contract_modules.transaction_index as transaction_index
                    FROM smart_contract_modules
                        JOIN transactions ON smart_contract_modules.transaction_index = \
             transactions.index
                        JOIN blocks ON transactions.block_height = blocks.height
                    WHERE starts_with(module_reference, $1)
                    ORDER BY block_height ASC, transaction_index ASC
                    LIMIT 1
                )
                SELECT
                    starting_module.block_height AS start_block_height,
                    starting_module.transaction_index AS start_transaction_index,
                    ending_module.block_height AS end_block_height,
                    ending_module.transaction_index AS end_transaction_index
                FROM starting_module, ending_module",
            lower_case_query
        )
        .fetch_optional(pool)
        .await?;

        if let Some(collection_ends) = collection_ends {
            let min_start_cursor = Cursor {
                outer: collection_ends.start_block_height,
                inner: collection_ends.start_transaction_index,
            };

            let collection_start_cursor = Cursor {
                outer: first_item.node.block_height,
                inner: first_item.node.transaction_index,
            };
            connection.has_previous_page = min_start_cursor < collection_start_cursor;

            let max_end_cursor = Cursor {
                outer: collection_ends.end_block_height,
                inner: collection_ends.end_transaction_index,
            };
            let collection_start_cursor = Cursor {
                outer: last_item.node.block_height,
                inner: last_item.node.transaction_index,
            };
            connection.has_next_page = max_end_cursor > collection_start_cursor;
        }

        Ok(connection)
    }

    async fn blocks(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Block>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query =
            ConnectionQuery::<i64>::new(first, after, last, before, config.block_connection_limit)?;
        let mut connection = connection::Connection::new(false, false);
        if !HASH_256_REGEX.is_match(&self.query) {
            return Ok(connection);
        }

        let height_query = self.query.parse::<i64>().ok();
        let query_lowercase = self.query.to_lowercase();
        let hash_query: Cow<str> = if query_lowercase.len() >= MIN_HASH_QUERY_LENGTH {
            query_lowercase.into()
        } else {
            if height_query.is_none() {
                return Ok(connection);
            }
            HASH_DUMMY_QUERY.into()
        };

        db::with_force_custom_plan(
            pool,
            async |db_connection: &mut PoolConnection<Postgres>| {
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
                    height_query,
                    hash_query.as_ref()
                )
                .fetch(db_connection.as_mut());

                while let Some(block) = rows.try_next().await? {
                    connection
                        .edges
                        .push(connection::Edge::new(block.height.to_string(), block));
                }
                drop(rows);

                if let (Some(page_min_height), Some(page_max_height)) =
                    (connection.edges.first(), connection.edges.last())
                {
                    let result = sqlx::query!(
                        "
                    SELECT MAX(height) as max_height, MIN(height) as min_height
                    FROM blocks
                    WHERE
                        height = $1
                        OR starts_with(hash, $2)
                ",
                        height_query,
                        hash_query.as_ref(),
                    )
                    .fetch_one(db_connection.as_mut())
                    .await?;

                    connection.has_previous_page = result
                        .max_height
                        .is_some_and(|db_max| db_max > page_max_height.node.height);
                    connection.has_next_page = result
                        .min_height
                        .is_some_and(|db_min| db_min < page_min_height.node.height);
                }
                Ok(connection)
            },
        )
        .await
    }

    async fn transactions(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
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
        if !HASH_256_REGEX.is_match(&self.query) || self.query.len() < MIN_HASH_QUERY_LENGTH {
            return Ok(connection);
        }
        let lower_case_query = self.query.to_lowercase();

        db::with_force_custom_plan(pool, async |db_connection| {
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
            .fetch(db_connection.as_mut());

            while let Some(tx) = row_stream.try_next().await? {
                connection.edges.push(connection::Edge::new(tx.index.to_string(), tx));
            }
            drop(row_stream);

            if let (Some(page_min), Some(page_max)) =
                (connection.edges.last(), connection.edges.first())
            {
                let result = sqlx::query!(
                    r#"SELECT MAX(index) as max_id, MIN(index) as min_id 
                                FROM transactions 
                                WHERE starts_with(hash, $1)"#,
                    lower_case_query
                )
                .fetch_one(db_connection.as_mut())
                .await?;
                connection.has_next_page =
                    result.min_id.is_some_and(|db_min| db_min < page_min.node.index);
                connection.has_previous_page =
                    result.max_id.is_some_and(|db_max| db_max > page_max.node.index);
            }
            Ok(connection)
        }).await
    }

    async fn tokens(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Token>> {
        // Base58 characters
        let token_address_regex: Regex = Regex::new(r"^[1-9A-HJ-NP-Za-km-z]+$")
            .map_err(|_| InternalError::InternalError("Invalid regex".to_string()))?;

        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query =
            ConnectionQuery::<i64>::new(first, after, last, before, config.block_connection_limit)?;
        let mut connection = connection::Connection::new(false, false);
        if !token_address_regex.is_match(&self.query) {
            return Ok(connection);
        }
        let mut rows = sqlx::query_as!(
            Token,
            "SELECT * FROM (
                SELECT
                    index,
                    init_transaction_index,
                    total_supply as raw_total_supply,
                    token_id,
                    contract_index,
                    contract_sub_index,
                    token_address,
                    metadata_url
                FROM tokens
                WHERE 
                    starts_with(token_address, $5)
                    AND tokens.index > $1 
                    AND tokens.index < $2
                ORDER BY
                    (CASE WHEN $4 THEN tokens.index END) DESC,
                    (CASE WHEN NOT $4 THEN tokens.index END) ASC
                LIMIT $3
            ) AS token_data
            ORDER BY token_data.index ASC",
            query.from,    // $1
            query.to,      // $2
            query.limit,   // $3
            query.is_last, // $4
            self.query     // $5
        )
        .fetch(pool);

        while let Some(token) = rows.try_next().await? {
            connection
                .edges
                .push(connection::Edge::new(token.index.to_string(), token));
        }

        if let (Some(page_min), Some(page_max)) =
            (connection.edges.first(), connection.edges.last())
        {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as max_index, MIN(index) as min_index
                    FROM tokens
                    WHERE
                        starts_with(token_address, $1)
                ",
                self.query,
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page = result
                .max_index
                .is_some_and(|db_max| db_max > page_max.node.index);
            connection.has_next_page = result
                .min_index
                .is_some_and(|db_min| db_min < page_min.node.index);
        }
        Ok(connection)
    }

    async fn plt_tokens(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, PltToken>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.plt_token_events_collection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // Input validation for the user-provided query:
        // - name_or_token_id_regex: Accepts characters commonly used in PLT token NAMES and IDs
        //   (alphanumeric, space, underscore, '-', '.', '%').
        let name_or_token_id_regex: Regex = Regex::new(r"^[a-zA-Z0-9\s\-_\.%]+$")
            .map_err(|_| InternalError::InternalError("Invalid regex".to_string()))?;

        if self.query.trim().is_empty() || self.query.len() < 2 {
            return Ok(connection);
        }

        // Check if it could be a Base58 account address (issuer address search)
        // If this does not match, we completely skip the issuer-address branch in SQL for performance.
        let account_address_regex: Regex = Regex::new(r"^[1-9A-HJ-NP-Za-km-z]{1,50}$")
            .map_err(|_| InternalError::InternalError("Invalid regex".to_string()))?;

        let is_base58 = account_address_regex.is_match(&self.query);
        // True when the input looks like a PLT token NAME or ID prefix.
        // Used as a toggle to proceed with name/token-id search paths only for matching input.
        let matches_name_or_token_id = name_or_token_id_regex.is_match(&self.query);

        // If the input is neither a name/token-id prefix nor a base58 address,
        // we can skip the search entirely.
        if !is_base58 && !matches_name_or_token_id {
            return Ok(connection);
        }

        let lower_case_query = self.query.to_lowercase();

        // First, get the data of matching PLT tokens
        let token_rows = sqlx::query!(
            r#"SELECT 
                plt_tokens.name,
                plt_tokens.index,
                plt_tokens.token_id, 
                plt_tokens.transaction_index,
                plt_tokens.issuer_index,
                plt_tokens.module_reference,
                plt_tokens.metadata as "metadata: sqlx::types::Json<sqlx::types::JsonValue>",
                plt_tokens.initial_supply,
                plt_tokens.total_minted,
                plt_tokens.total_burned,
                plt_tokens.decimal
            FROM plt_tokens
            LEFT JOIN accounts ON plt_tokens.issuer_index = accounts.index
            WHERE (
                -- Search by token name (case insensitive)
                LOWER(plt_tokens.name) LIKE $5 || '%'
                -- Search by token ID (case insensitive) 
                OR LOWER(plt_tokens.token_id) LIKE $5 || '%'
                -- Search by issuer address if it's a valid Base58 format
                OR ($6 AND accounts.address LIKE $7 || '%')
            )
            AND $2 < plt_tokens.index 
            AND plt_tokens.index < $1
            ORDER BY
                (CASE WHEN $4 THEN plt_tokens.index END) ASC,
                (CASE WHEN NOT $4 THEN plt_tokens.index END) DESC
            LIMIT $3"#,
            i64::from(query.from), // $1
            i64::from(query.to),   // $2
            query.limit,           // $3
            query.is_last,         // $4
            lower_case_query,      // $5 - for name and token_id search
            is_base58, // $6 - flag to enable or disable address search to improve performance
            self.query  // $7 - original query for address search (case sensitive)
        )
        .fetch_all(pool)
        .await?;

        let mut first_index: Option<i64> = None;
        let mut last_index: Option<i64> = None;

        // Process the fetched PLT token rows
        for token_row in token_rows {
            // Parse the token_id we already have from the query
            let token_id: TokenId = token_row.token_id.parse().map_err(|e| {
                InternalError::InternalError(format!("Failed to parse token ID: {}", e))
            })?;

            // Construct the PltToken from the plt data fetched
            let plt_token = PltToken::new(PltTokenParams {
                index: token_row.index,
                name: Some(token_row.name),
                token_id,
                transaction_index: token_row.transaction_index,
                issuer_index: token_row.issuer_index,
                module_reference: token_row.module_reference,
                metadata: token_row.metadata,
                initial_supply: token_row.initial_supply,
                total_minted: token_row.total_minted,
                total_burned: token_row.total_burned,
                decimal: Some(token_row.decimal),
            });

            if first_index.is_none() {
                first_index = Some(token_row.index);
            }
            last_index = Some(token_row.index);

            connection.edges.push(connection::Edge::new(
                token_row.index.to_string(),
                plt_token,
            ));
        }

        if let (Some(first_idx), Some(last_idx)) = (first_index, last_index) {
            let result = sqlx::query!(
                r#"SELECT MAX(plt_tokens.index) as max_index, MIN(plt_tokens.index) as min_index
                FROM plt_tokens
                LEFT JOIN accounts ON plt_tokens.issuer_index = accounts.index
                WHERE (
                    LOWER(plt_tokens.name) LIKE $1 || '%'
                    OR LOWER(plt_tokens.token_id) LIKE $1 || '%'
                    OR ($2 AND accounts.address LIKE $3 || '%')
                )"#,
                lower_case_query, // $1
                is_base58,        // $2
                self.query        // $3
            )
            .fetch_one(pool)
            .await?;

            connection.has_next_page = result.min_index.is_some_and(|db_min| db_min < last_idx);
            connection.has_previous_page =
                result.max_index.is_some_and(|db_max| db_max > first_idx);
        }

        Ok(connection)
    }

    async fn accounts(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Account>> {
        let account_address_regex: Regex = Regex::new(r"^[1-9A-HJ-NP-Za-km-z]{1,50}$")
            .map_err(|_| InternalError::InternalError("Invalid regex".to_string()))?;
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
                connection
                    .edges
                    .push(connection::Edge::new(account.index.to_string(), account));
            }
            return Ok(connection);
        };
        let accounts = sqlx::query_as!(
            Account,
            "
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
                ) ORDER BY index ASC",
            query.from,
            query.to,
            query.limit,
            query.is_last,
            self.query
        )
        .fetch_all(pool)
        .await?;

        for account in accounts {
            connection
                .edges
                .push(connection::Edge::new(account.index.to_string(), account));
        }

        if let (Some(page_min_id), Some(page_max_id)) =
            (connection.edges.first(), connection.edges.last())
        {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as max_id, MIN(index) as min_id
                    FROM accounts
                    WHERE
                        address LIKE $1 || '%'
                ",
                &self.query
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page = result
                .min_id
                .is_some_and(|db_min| db_min < page_min_id.node.index);
            connection.has_next_page = result
                .max_id
                .is_some_and(|db_max| db_max > page_max_id.node.index);
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
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, baker::Baker>> {
        let baker_index_regex: Regex = Regex::new("^[0-9]+$")
            .map_err(|e| InternalError::InternalError(format!("Invalid regex: {}", e)))?;
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
            connection
                .edges
                .push(connection::Edge::new(cursor, Baker::Current(Box::new(row))));
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
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<usize>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, NodeStatus>> {
        let handler = ctx
            .data::<NodeInfoReceiver>()
            .map_err(InternalError::NoReceiver)?;
        let statuses = if let Some(statuses) = handler.borrow().clone() {
            statuses
        } else {
            Err(InternalError::InternalError(
                "Node collector backend has not responded".to_string(),
            ))?
        };

        let nodes: Vec<NodeStatus> = statuses
            .iter()
            .filter(|x| {
                x.external.node_name.starts_with(&self.query)
                    || x.external.node_id.starts_with(&self.query)
            })
            .cloned()
            .collect();

        connection_from_slice(nodes, first, after, last, before)
    }
}
