use async_graphql::{connection, types, Context, Object};
use bigdecimal::BigDecimal;
use num_traits::ToPrimitive;

use crate::{
    address::AccountAddress,
    connection::DescendingI64,
    graphql_api::account::Account,
    scalar_types::{
        ModuleReference, PltIndex, TokenId, TokenIndex, TransactionHash, TransactionIndex,
    },
    transaction_event::protocol_level_tokens::{
        TokenAmount, TokenEventDetails, TokenUpdateEventType, TokenUpdateModuleType,
    },
};

use super::{
    block::Block, get_config, get_pool, ApiError, ApiResult, ConnectionQuery, InternalError,
};

use futures::TryStreamExt;
use sqlx::{types::Json, PgPool};
use std::str::FromStr;

#[derive(Default)]
pub struct QueryPLTEvent;

#[Object]
impl QueryPLTEvent {
    async fn plt_event(&self, ctx: &Context<'_>, id: types::ID) -> ApiResult<PLTEvent> {
        let index: i64 = id.try_into().map_err(ApiError::InvalidIdInt)?;
        PLTEvent::query_by_index(get_pool(ctx)?, index).await?.ok_or(ApiError::NotFound)
    }

    async fn plt_event_by_transaction_index(
        &self,
        ctx: &Context<'_>,
        transaction_index: types::ID,
    ) -> ApiResult<PLTEvent> {
        let index: i64 = transaction_index.try_into().map_err(ApiError::InvalidIdInt)?;
        PLTEvent::query_by_transaction_index(get_pool(ctx)?, index).await?.ok_or(ApiError::NotFound)
    }

    async fn plt_events<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, PLTEvent>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.plt_token_events_collection_limit,
        )?;

        let mut row_stream = sqlx::query_as!(
            PLTEvent,
            r#"SELECT 
                id,
                transaction_index,
                token_index,
                event_type as "event_type: TokenUpdateEventType",
                token_module_type as "token_module_type: TokenUpdateModuleType",
                token_event as "token_event: sqlx::types::Json<serde_json::Value>"
            FROM plt_events 
            WHERE $2 < id AND id < $1
            ORDER BY 
                CASE WHEN $3 THEN id END ASC,
                CASE WHEN NOT $3 THEN id END DESC
            LIMIT $4"#,
            i64::from(query.from),
            i64::from(query.to),
            query.is_last,
            query.limit,
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        while let Some(tx) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(tx.id.to_string(), tx));
        }
        if let (Some(page_min), Some(page_max)) =
            (connection.edges.last(), connection.edges.first())
        {
            let result =
                sqlx::query!("SELECT MAX(id) as max_id, MIN(id) as min_id FROM plt_events")
                    .fetch_one(pool)
                    .await?;
            connection.has_next_page =
                result.min_id.is_some_and(|db_min| db_min < page_min.node.id);
            connection.has_previous_page =
                result.max_id.is_some_and(|db_max| db_max > page_max.node.id);
        }
        Ok(connection)
    }

    async fn plt_events_by_token_id<'a>(
        &self,
        ctx: &Context<'a>,
        id: types::ID,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, PLTEvent>> {
        let token_id = TokenId::from_str(id.as_ref()).map_err(|e| {
            ApiError::InternalServerError(InternalError::InternalError(format!(
                "Failed to parse token ID: {}",
                e
            )))
        })?;
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.plt_token_events_collection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            PLTEvent,
            r#"SELECT 
                    e.id,
                    e.transaction_index,
                    e.token_index,
                    e.event_type AS "event_type: TokenUpdateEventType",
                    e.token_module_type AS "token_module_type: TokenUpdateModuleType",
                    e.token_event AS "token_event: sqlx::types::Json<serde_json::Value>"
                FROM plt_events e
                JOIN plt_tokens t ON e.token_index = t.index
                WHERE $2 < e.id AND e.id < $1 AND t.token_id = $3
                ORDER BY 
                    (CASE WHEN $4 THEN e.id END) ASC,
                    (CASE WHEN NOT $4 THEN e.id END) DESC
                LIMIT $5"#,
            i64::from(query.from),
            i64::from(query.to),
            token_id.to_string(),
            query.is_last,
            query.limit,
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        while let Some(tx) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(tx.id.to_string(), tx));
        }
        Ok(connection)
    }
}

#[derive(Debug, Clone)]
pub struct PLTEvent {
    pub id:                PltIndex,
    pub transaction_index: TransactionIndex,
    pub event_type:        Option<TokenUpdateEventType>,
    pub token_module_type: Option<TokenUpdateModuleType>,
    pub token_index:       TokenIndex,
    pub token_event:       Json<serde_json::Value>,
}

impl PLTEvent {
    pub async fn query_by_index(pool: &PgPool, index: i64) -> ApiResult<Option<Self>> {
        let plt_event: Option<PLTEvent> = sqlx::query_as!(
            PLTEvent,
            r#"SELECT 
                id,
                transaction_index,
                token_index,
                event_type as "event_type: TokenUpdateEventType",
                token_module_type as "token_module_type: TokenUpdateModuleType",
                token_event as "token_event: sqlx::types::Json<serde_json::Value>"
            FROM plt_events WHERE id = $1"#,
            index
        )
        .fetch_optional(pool)
        .await?;
        Ok(plt_event)
    }

    pub async fn query_by_transaction_index(
        pool: &PgPool,
        transaction_index: i64,
    ) -> ApiResult<Option<Self>> {
        let plt_event: Option<PLTEvent> = sqlx::query_as!(
            PLTEvent,
            r#"SELECT 
                id,
                transaction_index,
                token_index,
                event_type as "event_type: TokenUpdateEventType",
                token_module_type as "token_module_type: TokenUpdateModuleType",
                token_event as "token_event: sqlx::types::Json<serde_json::Value>"
            FROM plt_events WHERE transaction_index = $1"#,
            transaction_index
        )
        .fetch_optional(pool)
        .await?;
        Ok(plt_event)
    }
}

#[Object]
impl PLTEvent {
    async fn id(&self) -> ApiResult<PltIndex> { Ok(self.id) }

    async fn transaction_index(&self) -> ApiResult<TransactionIndex> { Ok(self.transaction_index) }

    async fn event_type(&self) -> ApiResult<Option<TokenUpdateEventType>> { Ok(self.event_type) }

    async fn token_module_type(&self) -> ApiResult<Option<TokenUpdateModuleType>> {
        Ok(self.token_module_type)
    }

    async fn token_id<'a>(&self, ctx: &Context<'a>) -> ApiResult<TokenId> {
        let token_index = self.token_index;
        let result = sqlx::query!("SELECT token_id FROM plt_tokens WHERE index = $1", token_index)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(result.token_id)
    }

    async fn token_event(&self) -> ApiResult<TokenEventDetails> {
        let details: TokenEventDetails = serde_json::from_value(self.token_event.0.clone())
            .map_err(|e| {
                ApiError::InternalServerError(InternalError::InternalError(e.to_string()))
            })?;
        Ok(details)
    }

    async fn transaction_hash<'a>(&self, ctx: &Context<'a>) -> ApiResult<TransactionHash> {
        let result =
            sqlx::query!("SELECT hash FROM transactions WHERE index = $1", self.transaction_index)
                .fetch_one(get_pool(ctx)?)
                .await?;
        Ok(result.hash)
    }

    async fn block<'a>(&self, ctx: &Context<'a>) -> ApiResult<Block> {
        let transaction_index = self.transaction_index;
        let result = sqlx::query!(
            "SELECT block_height FROM transactions WHERE index = $1",
            transaction_index
        )
        .fetch_one(get_pool(ctx)?)
        .await?;

        Block::query_by_height(get_pool(ctx)?, result.block_height).await
    }

    async fn token_name<'a>(&self, ctx: &Context<'a>) -> ApiResult<Option<String>> {
        let token_index = self.token_index;
        let result = sqlx::query!("SELECT name FROM plt_tokens WHERE index = $1", token_index)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(Some(result.name))
    }
}

// --------------------------

#[derive(Default)]
pub struct QueryPLT;

#[Object]
impl QueryPLT {
    async fn plt_token(&self, ctx: &Context<'_>, id: types::ID) -> ApiResult<PLTToken> {
        let token_id = TokenId::from_str(id.as_ref()).map_err(|e| {
            ApiError::InternalServerError(InternalError::InternalError(format!(
                "Failed to parse token ID: {}",
                e
            )))
        })?;
        PLTToken::query_by_id(get_pool(ctx)?, token_id).await?.ok_or(ApiError::NotFound)
    }

    async fn plt_tokens<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, PLTToken>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.plt_token_events_collection_limit,
        )?;

        let mut row_stream = sqlx::query_as!(
            PLTToken,
            r#"SELECT name,
                index,
                token_id,
                transaction_index,
                issuer_index,
                module_reference,
                metadata as "metadata: sqlx::types::Json<sqlx::types::JsonValue>",
                initial_supply,
                total_minted,
                total_burned,
                decimal
            FROM plt_tokens 
            WHERE $2 < index AND index < $1
            ORDER BY 
                CASE WHEN $3 THEN index END ASC,
                CASE WHEN NOT $3 THEN index END DESC
            LIMIT $4"#,
            i64::from(query.from),
            i64::from(query.to),
            query.is_last,
            query.limit
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        while let Some(token) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(token.index.to_string(), token));
        }
        if let (Some(page_min), Some(page_max)) =
            (connection.edges.last(), connection.edges.first())
        {
            let result = sqlx::query!(
                "SELECT MAX(index) as max_index, MIN(index) as min_index FROM plt_tokens"
            )
            .fetch_one(pool)
            .await?;
            connection.has_next_page =
                result.min_index.is_some_and(|db_min| db_min < page_min.node.index);
            connection.has_previous_page =
                result.max_index.is_some_and(|db_max| db_max > page_max.node.index);
        }
        Ok(connection)
    }
}

pub struct PLTToken {
    index:             TokenIndex,
    name:              Option<String>,
    token_id:          TokenId,
    transaction_index: TransactionIndex,
    issuer_index:      i64,
    module_reference:  Option<ModuleReference>,
    metadata:          Option<sqlx::types::Json<sqlx::types::JsonValue>>,
    initial_supply:    Option<BigDecimal>,
    total_minted:      Option<BigDecimal>,
    total_burned:      Option<BigDecimal>,
    decimal:           Option<i32>,
}

impl PLTToken {
    pub async fn query_by_id(pool: &PgPool, token_id: TokenId) -> ApiResult<Option<Self>> {
        let result = sqlx::query_as!(
            PLTToken,
            r#"SELECT name,
                    index,
                    token_id,
                    transaction_index,
                    issuer_index,
                    module_reference,
                    metadata as "metadata: sqlx::types::Json<sqlx::types::JsonValue>",
                    initial_supply,
                    total_minted,
                    total_burned,
                    decimal
            FROM plt_tokens WHERE token_id = $1"#,
            token_id.to_string()
        )
        .fetch_optional(pool)
        .await?;
        Ok(result)
    }
}

#[Object]
impl PLTToken {
    async fn name(&self) -> ApiResult<Option<String>> { Ok(self.name.clone()) }

    async fn token_id(&self) -> ApiResult<TokenId> { Ok(self.token_id.clone()) }

    async fn transaction_hash<'a>(&self, ctx: &Context<'a>) -> ApiResult<TransactionHash> {
        let transaction_index = self.transaction_index;
        let result =
            sqlx::query!("SELECT hash FROM transactions WHERE index = $1", transaction_index)
                .fetch_one(get_pool(ctx)?)
                .await?;
        Ok(result.hash)
    }

    async fn block<'a>(&self, ctx: &Context<'a>) -> ApiResult<Block> {
        let transaction_index = self.transaction_index;
        let result = sqlx::query!(
            "SELECT block_height FROM transactions WHERE index = $1",
            transaction_index
        )
        .fetch_one(get_pool(ctx)?)
        .await?;

        Block::query_by_height(get_pool(ctx)?, result.block_height).await
    }

    async fn issuer<'a>(&self, ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        let issuer_index = self.issuer_index;
        let result = sqlx::query!("SELECT address FROM accounts WHERE index = $1", issuer_index)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(result.address.into())
    }

    async fn module_reference(&self) -> ApiResult<Option<String>> {
        Ok(self.module_reference.clone())
    }

    async fn metadata(&self) -> ApiResult<Option<async_graphql::Json<serde_json::Value>>> {
        Ok(self.metadata.as_ref().map(|json| async_graphql::Json(json.0.clone())))
    }

    async fn initial_supply(&self) -> ApiResult<Option<i64>> {
        let value = self.initial_supply.clone().and_then(|supply| supply.to_i64());
        Ok(value)
    }

    async fn total_supply(&self) -> ApiResult<Option<u64>> {
        let total_supply = self
            .total_minted
            .clone()
            .and_then(|minted| self.total_burned.clone().map(|burned| minted - burned));
        Ok(total_supply.and_then(|supply| supply.to_u64()))
    }

    async fn total_minted(&self) -> ApiResult<Option<u64>> {
        Ok(self.total_minted.clone().and_then(|supply| supply.to_u64()))
    }

    async fn total_burned(&self) -> ApiResult<Option<u64>> {
        Ok(self.total_burned.clone().and_then(|supply| supply.to_u64()))
    }

    async fn decimal(&self) -> ApiResult<Option<i32>> { Ok(self.decimal) }

    async fn index(&self) -> ApiResult<i64> { Ok(self.index) }

    async fn total_unique_holders<'a>(&self, ctx: &Context<'a>) -> ApiResult<i64> {
        let pool = get_pool(ctx)?;
        let unique_holder = PLTAccountAmount::query_by_token_id(pool, self.token_id.clone())
            .await
            .map_err(|e| {
                ApiError::InternalServerError(InternalError::InternalError(e.to_string()))
            })?
            .len() as i64;

        Ok(unique_holder)
    }
}

// --------------

#[derive(Default)]
pub struct QueryPLTAccountAmount;

#[Object]
impl QueryPLTAccountAmount {
    async fn plt_accounts(
        &self,
        ctx: &Context<'_>,
        account: types::ID,
        token_id: types::ID,
    ) -> ApiResult<Option<PLTAccountAmount>> {
        let pool = get_pool(ctx)?;
        let account_address = AccountAddress::from(account.to_string());
        let token_id: TokenId = token_id.to_string();
        PLTAccountAmount::query_by_account_and_token(pool, account_address, token_id)
            .await
            .map_err(|e| ApiError::InternalServerError(InternalError::InternalError(e.to_string())))
    }

    async fn plt_accounts_by_token_id(
        &self,
        ctx: &Context<'_>,
        token_id: types::ID,
    ) -> ApiResult<Vec<PLTAccountAmount>> {
        let pool = get_pool(ctx)?;
        let token_id: TokenId = token_id.to_string();
        Ok(PLTAccountAmount::query_by_token_id(pool, token_id).await?)
    }
}

pub struct PLTAccountAmount {
    pub account_index: i64,
    pub token_index:   TokenIndex,
    pub amount:        Option<BigDecimal>,
    pub decimal:       Option<i32>,
}

impl PLTAccountAmount {
    pub async fn query_by_account_and_token(
        pool: &PgPool,
        account: AccountAddress,
        token_id: TokenId,
    ) -> ApiResult<Option<Self>> {
        let account: Account = Account::query_by_address(pool, account.to_string())
            .await?
            .ok_or(ApiError::NotFound)?;
        let token: PLTToken =
            PLTToken::query_by_id(pool, token_id).await?.ok_or(ApiError::NotFound)?;
        let result = sqlx::query_as!(
            PLTAccountAmount,
            r#"SELECT 
                account_index,
                token_index,
                amount,
                decimal
            FROM plt_accounts         
            WHERE account_index = $1 AND token_index = $2"#,
            account.index,
            token.index
        )
        .fetch_optional(pool)
        .await?;
        Ok(result)
    }

    pub async fn query_by_token_id(pool: &PgPool, token_id: TokenId) -> ApiResult<Vec<Self>> {
        let token: PLTToken =
            PLTToken::query_by_id(pool, token_id).await?.ok_or(ApiError::NotFound)?;
        let result = sqlx::query_as!(
            PLTAccountAmount,
            r#"SELECT 
                account_index,
                token_index,
                amount,
                decimal
            FROM plt_accounts         
            WHERE token_index = $1"#,
            token.index
        )
        .fetch_all(pool)
        .await?;
        Ok(result)
    }
}

#[Object]
impl PLTAccountAmount {
    async fn account_address<'a>(&self, ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        let result =
            sqlx::query!("SELECT address FROM accounts WHERE index = $1", self.account_index)
                .fetch_one(get_pool(ctx)?)
                .await?;
        Ok(result.address.into())
    }

    async fn token_id(&self, ctx: &Context<'_>) -> ApiResult<TokenId> {
        let result =
            sqlx::query!("SELECT token_id FROM plt_tokens WHERE index = $1", self.token_index)
                .fetch_one(get_pool(ctx)?)
                .await?;
        Ok(result.token_id)
    }

    async fn amount(&self) -> ApiResult<TokenAmount> {
        let value = self.amount.clone().and_then(|amt| amt.to_u64()).unwrap_or(0);
        let decimals = self.decimal.unwrap_or(0) as u8;
        Ok(TokenAmount {
            value:    value.to_string(),
            decimals: decimals.to_string(),
        })
    }
}
