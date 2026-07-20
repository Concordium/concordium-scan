use std::collections::BTreeSet;

use async_graphql::{connection, types, Context, Enum, Object};
use bigdecimal::BigDecimal;
use chrono::Utc;
use futures::TryStreamExt;
use sqlx::{types::Json, FromRow, PgPool};

use crate::{
    address::AccountAddress,
    connection::DescendingI64,
    graphql_api::{
        account::Account, get_config, get_pool, transaction::Transaction, ApiError, ApiResult,
        ConnectionQuery,
    },
    scalar_types::{DateTime, TokenId, TokenIndex, TransactionIndex},
    transaction_event::{
        protocol_level_locks::LockCreateConfig, protocol_level_tokens::TokenAmount,
    },
};

#[derive(Default)]
pub struct QueryLock;

#[Object]
impl QueryLock {
    async fn lock(&self, ctx: &Context<'_>, lock_id: String) -> ApiResult<Lock> {
        Lock::query_by_id(get_pool(ctx)?, &lock_id)
            .await?
            .ok_or(ApiError::NotFound)
    }
}

#[derive(Debug, Clone, Copy, Enum, Eq, PartialEq)]
pub enum LockStatus {
    Active,
    Expired,
    Canceled,
}

#[derive(Debug, Clone, FromRow)]
pub struct Lock {
    pub lock_id: String,
    pub creator_account_index: Option<i64>,
    pub created_transaction_index: Option<i64>,
    pub created_at: Option<DateTime>,
    pub expiry: Option<DateTime>,
    pub canceled_transaction_index: Option<i64>,
    pub canceled_at: Option<DateTime>,
    pub config: Option<Json<LockCreateConfig>>,
    pub raw_config: Option<String>,
    pub metadata_name: Option<String>,
    pub metadata_description: Option<String>,
}

impl Lock {
    pub async fn query_by_id(pool: &PgPool, lock_id: &str) -> ApiResult<Option<Self>> {
        let lock = sqlx::query_as::<_, Lock>(
            r#"
            SELECT
                lock_id,
                creator_account_index,
                created_transaction_index,
                created_at,
                expiry,
                canceled_transaction_index,
                canceled_at,
                config,
                raw_config,
                metadata_name,
                metadata_description
            FROM plt_locks
            WHERE lock_id = $1
            "#,
        )
        .bind(lock_id)
        .fetch_optional(pool)
        .await?;
        Ok(lock)
    }

    fn compute_status(&self) -> LockStatus {
        if self.canceled_at.is_some() {
            LockStatus::Canceled
        } else if self.expiry.is_some_and(|expiry| expiry < Utc::now()) {
            LockStatus::Expired
        } else {
            LockStatus::Active
        }
    }

    async fn query_balances(&self, pool: &PgPool) -> ApiResult<Vec<LockBalance>> {
        let balances = sqlx::query_as::<_, LockBalance>(
            r#"
            SELECT
                plt_lock_balances.lock_id,
                plt_lock_balances.account_index,
                plt_lock_balances.token_index,
                plt_tokens.token_id,
                plt_lock_balances.amount,
                plt_lock_balances.decimal
            FROM plt_lock_balances
            JOIN plt_tokens ON plt_tokens.index = plt_lock_balances.token_index
            WHERE plt_lock_balances.lock_id = $1
                AND plt_lock_balances.amount <> 0
            ORDER BY plt_lock_balances.account_index ASC, plt_tokens.token_id ASC
            "#,
        )
        .bind(&self.lock_id)
        .fetch_all(pool)
        .await?;
        Ok(balances)
    }

    async fn query_history(
        &self,
        ctx: &Context<'_>,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, LockHistoryEvent>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.lock_history_connection_limit,
        )?;

        let mut row_stream = sqlx::query_as::<_, LockHistoryEvent>(
            r#"
            SELECT *
            FROM (
                SELECT
                    plt_lock_events.id,
                    plt_lock_events.transaction_index,
                    plt_lock_events.block_height,
                    plt_lock_events.slot_time,
                    plt_lock_events.operation_order,
                    plt_lock_events.event_type,
                    plt_lock_events.lock_id,
                    plt_lock_events.token_index,
                    plt_tokens.token_id,
                    plt_lock_events.account_index,
                    plt_lock_events.source_account_index,
                    plt_lock_events.recipient_account_index,
                    plt_lock_events.amount,
                    plt_lock_events.decimals,
                    plt_lock_events.memo,
                    plt_lock_events.event
                FROM plt_lock_events
                LEFT JOIN plt_tokens ON plt_tokens.index = plt_lock_events.token_index
                WHERE plt_lock_events.lock_id = $5
                    AND $2 < plt_lock_events.id
                    AND plt_lock_events.id < $1
                ORDER BY
                    CASE WHEN $4 THEN plt_lock_events.id END ASC,
                    CASE WHEN NOT $4 THEN plt_lock_events.id END DESC
                LIMIT $3
            ) events
            ORDER BY id DESC
            "#,
        )
        .bind(i64::from(query.from))
        .bind(i64::from(query.to))
        .bind(query.limit)
        .bind(query.is_last)
        .bind(&self.lock_id)
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);
        let mut min_id: Option<i64> = None;
        let mut max_id: Option<i64> = None;
        while let Some(event) = row_stream.try_next().await? {
            min_id = Some(min_id.map_or(event.id, |current| current.min(event.id)));
            max_id = Some(max_id.map_or(event.id, |current| current.max(event.id)));
            connection
                .edges
                .push(connection::Edge::new(event.id.to_string(), event));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (min_id, max_id) {
            let bounds: LockHistoryBounds = sqlx::query_as(
                r#"
                SELECT MIN(id) AS min_id, MAX(id) AS max_id
                FROM plt_lock_events
                WHERE lock_id = $1
                "#,
            )
            .bind(&self.lock_id)
            .fetch_one(pool)
            .await?;
            connection.has_previous_page = bounds.max_id.is_some_and(|db_max| db_max > page_max_id);
            connection.has_next_page = bounds.min_id.is_some_and(|db_min| db_min < page_min_id);
        }

        Ok(connection)
    }
}

#[Object]
impl Lock {
    async fn id(&self) -> types::ID {
        types::ID::from(self.lock_id.clone())
    }

    async fn lock_id(&self) -> &str {
        &self.lock_id
    }

    async fn creator(&self, ctx: &Context<'_>) -> ApiResult<Option<Account>> {
        match self.creator_account_index {
            Some(index) => Account::query_by_index(get_pool(ctx)?, index).await,
            None => Ok(None),
        }
    }

    async fn created_transaction(&self, ctx: &Context<'_>) -> ApiResult<Option<Transaction>> {
        match self.created_transaction_index {
            Some(index) => Transaction::query_by_index(get_pool(ctx)?, index).await,
            None => Ok(None),
        }
    }

    async fn created_at(&self) -> Option<DateTime> {
        self.created_at
    }

    async fn expiry(&self) -> Option<DateTime> {
        self.expiry
    }

    async fn canceled_transaction(&self, ctx: &Context<'_>) -> ApiResult<Option<Transaction>> {
        match self.canceled_transaction_index {
            Some(index) => Transaction::query_by_index(get_pool(ctx)?, index).await,
            None => Ok(None),
        }
    }

    async fn canceled_at(&self) -> Option<DateTime> {
        self.canceled_at
    }

    async fn status(&self) -> LockStatus {
        self.compute_status()
    }

    async fn config(&self) -> Option<LockCreateConfig> {
        self.config.as_ref().map(|config| config.0.clone())
    }

    async fn raw_config(&self) -> Option<&str> {
        self.raw_config.as_deref()
    }

    async fn metadata_name(&self) -> Option<&str> {
        self.metadata_name.as_deref()
    }

    async fn metadata_description(&self) -> Option<&str> {
        self.metadata_description.as_deref()
    }

    async fn balances(&self, ctx: &Context<'_>) -> ApiResult<Vec<LockBalance>> {
        self.query_balances(get_pool(ctx)?).await
    }

    async fn history(
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
    ) -> ApiResult<connection::Connection<String, LockHistoryEvent>> {
        self.query_history(ctx, first, after, last, before).await
    }
}

#[derive(Debug, Clone, FromRow)]
pub struct LockBalance {
    lock_id: String,
    account_index: i64,
    token_index: TokenIndex,
    token_id: TokenId,
    amount: BigDecimal,
    decimal: i32,
}

#[Object]
impl LockBalance {
    async fn lock_id(&self) -> &str {
        &self.lock_id
    }

    async fn account(&self, ctx: &Context<'_>) -> ApiResult<Account> {
        Account::query_by_index(get_pool(ctx)?, self.account_index)
            .await?
            .ok_or(ApiError::NotFound)
    }

    async fn account_address(&self, ctx: &Context<'_>) -> ApiResult<AccountAddress> {
        let row: AccountAddressRow =
            sqlx::query_as("SELECT address FROM accounts WHERE index = $1")
                .bind(self.account_index)
                .fetch_one(get_pool(ctx)?)
                .await?;
        Ok(row.address.into())
    }

    async fn token_index(&self) -> TokenIndex {
        self.token_index
    }

    async fn token_id(&self) -> &str {
        &self.token_id
    }

    async fn amount(&self) -> TokenAmount {
        TokenAmount {
            value: self.amount.to_string(),
            decimals: self.decimal.to_string(),
        }
    }
}

#[derive(Debug, Clone, FromRow)]
pub struct LockHistoryEvent {
    id: i64,
    transaction_index: TransactionIndex,
    block_height: i64,
    slot_time: DateTime,
    operation_order: i32,
    event_type: String,
    lock_id: String,
    token_index: Option<TokenIndex>,
    token_id: Option<TokenId>,
    account_index: Option<i64>,
    source_account_index: Option<i64>,
    recipient_account_index: Option<i64>,
    amount: Option<BigDecimal>,
    decimals: Option<i32>,
    memo: Option<Json<serde_json::Value>>,
    event: Json<serde_json::Value>,
}

#[Object]
impl LockHistoryEvent {
    async fn id(&self) -> types::ID {
        types::ID::from(self.id)
    }

    async fn transaction(&self, ctx: &Context<'_>) -> ApiResult<Transaction> {
        Transaction::query_by_index(get_pool(ctx)?, self.transaction_index)
            .await?
            .ok_or(ApiError::NotFound)
    }

    async fn transaction_index(&self) -> TransactionIndex {
        self.transaction_index
    }

    async fn block_height(&self) -> i64 {
        self.block_height
    }

    async fn slot_time(&self) -> DateTime {
        self.slot_time
    }

    async fn operation_order(&self) -> i32 {
        self.operation_order
    }

    async fn event_type(&self) -> &str {
        &self.event_type
    }

    async fn lock_id(&self) -> &str {
        &self.lock_id
    }

    async fn token_index(&self) -> Option<TokenIndex> {
        self.token_index
    }

    async fn token_id(&self) -> Option<&str> {
        self.token_id.as_deref()
    }

    async fn account(&self, ctx: &Context<'_>) -> ApiResult<Option<Account>> {
        match self.account_index {
            Some(index) => Account::query_by_index(get_pool(ctx)?, index).await,
            None => Ok(None),
        }
    }

    async fn source(&self, ctx: &Context<'_>) -> ApiResult<Option<Account>> {
        match self.source_account_index {
            Some(index) => Account::query_by_index(get_pool(ctx)?, index).await,
            None => Ok(None),
        }
    }

    async fn recipient(&self, ctx: &Context<'_>) -> ApiResult<Option<Account>> {
        match self.recipient_account_index {
            Some(index) => Account::query_by_index(get_pool(ctx)?, index).await,
            None => Ok(None),
        }
    }

    async fn amount(&self) -> Option<TokenAmount> {
        self.amount.as_ref().map(|amount| TokenAmount {
            value: amount.to_string(),
            decimals: self.decimals.unwrap_or_default().to_string(),
        })
    }

    async fn memo(&self) -> Option<async_graphql::Json<serde_json::Value>> {
        self.memo
            .as_ref()
            .map(|memo| async_graphql::Json(memo.0.clone()))
    }

    async fn event(&self) -> async_graphql::Json<serde_json::Value> {
        async_graphql::Json(self.event.0.clone())
    }
}

#[derive(Debug, Clone, FromRow)]
pub struct AccountRelatedLock {
    lock_id: String,
    creator_account_index: Option<i64>,
    created_transaction_index: Option<i64>,
    created_at: Option<DateTime>,
    expiry: Option<DateTime>,
    canceled_transaction_index: Option<i64>,
    canceled_at: Option<DateTime>,
    config: Option<Json<LockCreateConfig>>,
    raw_config: Option<String>,
    metadata_name: Option<String>,
    metadata_description: Option<String>,
    account_index: i64,
    cursor_index: i64,
}

impl AccountRelatedLock {
    pub async fn connection(
        ctx: &Context<'_>,
        account_index: i64,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountRelatedLock>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.account_related_locks_connection_limit,
        )?;

        let mut row_stream = sqlx::query_as::<_, AccountRelatedLock>(
            r#"
            SELECT *
            FROM (
                SELECT
                    locks.lock_id,
                    locks.creator_account_index,
                    locks.created_transaction_index,
                    locks.created_at,
                    locks.expiry,
                    locks.canceled_transaction_index,
                    locks.canceled_at,
                    locks.config,
                    locks.raw_config,
                    locks.metadata_name,
                    locks.metadata_description,
                    $5::BIGINT AS account_index,
                    COALESCE(locks.created_transaction_index, 0) AS cursor_index
                FROM plt_locks locks
                WHERE EXISTS (
                    SELECT 1
                    FROM plt_lock_accounts accounts
                    WHERE accounts.lock_id = locks.lock_id
                        AND accounts.account_index = $5
                )
                    AND $2 < COALESCE(locks.created_transaction_index, 0)
                    AND COALESCE(locks.created_transaction_index, 0) < $1
                ORDER BY
                    CASE WHEN $4 THEN COALESCE(locks.created_transaction_index, 0) END ASC,
                    CASE WHEN NOT $4 THEN COALESCE(locks.created_transaction_index, 0) END DESC,
                    locks.lock_id ASC
                LIMIT $3
            ) locks
            ORDER BY cursor_index DESC, lock_id ASC
            "#,
        )
        .bind(i64::from(query.from))
        .bind(i64::from(query.to))
        .bind(query.limit)
        .bind(query.is_last)
        .bind(account_index)
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);
        let mut min_index: Option<i64> = None;
        let mut max_index: Option<i64> = None;
        while let Some(lock) = row_stream.try_next().await? {
            min_index =
                Some(min_index.map_or(lock.cursor_index, |current| current.min(lock.cursor_index)));
            max_index =
                Some(max_index.map_or(lock.cursor_index, |current| current.max(lock.cursor_index)));
            connection
                .edges
                .push(connection::Edge::new(lock.cursor_index.to_string(), lock));
        }

        if let (Some(page_min_index), Some(page_max_index)) = (min_index, max_index) {
            let bounds: AccountRelatedLocksBounds = sqlx::query_as(
                r#"
                SELECT
                    MIN(COALESCE(locks.created_transaction_index, 0)) AS min_index,
                    MAX(COALESCE(locks.created_transaction_index, 0)) AS max_index
                FROM plt_locks locks
                WHERE EXISTS (
                    SELECT 1
                    FROM plt_lock_accounts accounts
                    WHERE accounts.lock_id = locks.lock_id
                        AND accounts.account_index = $1
                )
                "#,
            )
            .bind(account_index)
            .fetch_one(pool)
            .await?;
            connection.has_previous_page = bounds
                .max_index
                .is_some_and(|db_max| db_max > page_max_index);
            connection.has_next_page = bounds
                .min_index
                .is_some_and(|db_min| db_min < page_min_index);
        }

        Ok(connection)
    }

    fn as_lock(&self) -> Lock {
        Lock {
            lock_id: self.lock_id.clone(),
            creator_account_index: self.creator_account_index,
            created_transaction_index: self.created_transaction_index,
            created_at: self.created_at,
            expiry: self.expiry,
            canceled_transaction_index: self.canceled_transaction_index,
            canceled_at: self.canceled_at,
            config: self.config.clone(),
            raw_config: self.raw_config.clone(),
            metadata_name: self.metadata_name.clone(),
            metadata_description: self.metadata_description.clone(),
        }
    }

    async fn query_account_balances(&self, pool: &PgPool) -> ApiResult<Vec<LockBalance>> {
        let balances = sqlx::query_as::<_, LockBalance>(
            r#"
            SELECT
                plt_lock_balances.lock_id,
                plt_lock_balances.account_index,
                plt_lock_balances.token_index,
                plt_tokens.token_id,
                plt_lock_balances.amount,
                plt_lock_balances.decimal
            FROM plt_lock_balances
            JOIN plt_tokens ON plt_tokens.index = plt_lock_balances.token_index
            WHERE plt_lock_balances.lock_id = $1
                AND plt_lock_balances.account_index = $2
                AND plt_lock_balances.amount <> 0
            ORDER BY plt_tokens.token_id ASC
            "#,
        )
        .bind(&self.lock_id)
        .bind(self.account_index)
        .fetch_all(pool)
        .await?;
        Ok(balances)
    }

    async fn query_account_address(&self, pool: &PgPool) -> ApiResult<AccountAddress> {
        let row: AccountAddressRow =
            sqlx::query_as("SELECT address FROM accounts WHERE index = $1")
                .bind(self.account_index)
                .fetch_one(pool)
                .await?;
        Ok(row.address.into())
    }

    async fn query_roles(&self, pool: &PgPool) -> ApiResult<Vec<String>> {
        let Some(simple_v0) = self
            .config
            .as_ref()
            .and_then(|config| config.0.controller.simple_v0.as_ref())
        else {
            return Ok(Vec::new());
        };
        let account_address = self.query_account_address(pool).await?.to_string();

        Ok(simple_v0
            .grants
            .iter()
            .filter(|grant| grant.account.address.to_string() == account_address)
            .flat_map(|grant| grant.roles.iter().cloned())
            .collect::<BTreeSet<_>>()
            .into_iter()
            .collect())
    }
}

#[Object]
impl AccountRelatedLock {
    async fn lock(&self) -> Lock {
        self.as_lock()
    }

    async fn account_balances(&self, ctx: &Context<'_>) -> ApiResult<Vec<LockBalance>> {
        self.query_account_balances(get_pool(ctx)?).await
    }

    async fn roles(&self, ctx: &Context<'_>) -> ApiResult<Vec<String>> {
        self.query_roles(get_pool(ctx)?).await
    }
}

#[derive(Debug, FromRow)]
struct LockHistoryBounds {
    min_id: Option<i64>,
    max_id: Option<i64>,
}

#[derive(Debug, FromRow)]
struct AccountRelatedLocksBounds {
    min_index: Option<i64>,
    max_index: Option<i64>,
}

#[derive(Debug, FromRow)]
struct AccountAddressRow {
    address: String,
}
