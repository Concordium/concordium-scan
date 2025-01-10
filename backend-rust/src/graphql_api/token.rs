use super::{
    get_config, get_pool, transaction::Transaction, Account, ApiError, ApiResult,
    CollectionSegmentInfo,
};
use crate::{
    address::ContractIndex,
    scalar_types::{BigInteger, TransactionIndex},
};
use async_graphql::{ComplexObject, Context, Object, SimpleObject};
use sqlx::PgPool;

#[derive(Default)]
pub struct QueryToken;

#[Object]
impl QueryToken {
    async fn token<'a>(
        &self,
        ctx: &Context<'a>,
        contract_index: ContractIndex,
        contract_sub_index: ContractIndex,
        token_id: String,
    ) -> ApiResult<Token> {
        let pool = get_pool(ctx)?;

        Token::query_by_contract_and_id(
            get_pool(ctx)?,
            contract_index.0 as i64,
            contract_sub_index.0 as i64,
            &token_id,
        )
        .await
    }
}

pub struct Token {
    pub index:                  i64,
    pub init_transaction_index: TransactionIndex,
    pub contract_index:         i64,
    pub contract_sub_index:     i64,
    pub token_id:               String,
    pub metadata_url:           Option<String>,
    pub raw_total_supply:       bigdecimal::BigDecimal,
    pub token_address:          String,
    // TODO tokenEvents(skip: Int take: Int): TokenEventsCollectionSegment
}

impl Token {
    async fn query_by_contract_and_id(
        pool: &PgPool,
        contract_index: i64,
        contract_sub_index: i64,
        token_id: &str,
    ) -> ApiResult<Self> {
        let token = sqlx::query_as!(
            Token,
            "SELECT
                index,
                total_supply as raw_total_supply,
                token_id,
                contract_index,
                contract_sub_index,
                token_address,
                metadata_url,
                init_transaction_index
            FROM tokens
            WHERE tokens.contract_index = $1 AND tokens.contract_sub_index = $2 AND \
             tokens.token_id = $3",
            contract_index,
            contract_sub_index,
            token_id
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        Ok(token)
    }
}

#[Object]
impl Token {
    async fn initial_transaction(&self, ctx: &Context<'_>) -> ApiResult<Transaction> {
        Transaction::query_by_index(get_pool(ctx)?, self.init_transaction_index).await?.ok_or(
            ApiError::InternalError("Token: No transaction at init_transaction_index".to_string()),
        )
    }

    async fn total_supply(&self, ctx: &Context<'_>) -> ApiResult<BigInteger> {
        Ok(BigInteger::from(self.raw_total_supply.clone()))
    }

    async fn token_address(&self) -> &String { &self.token_address }

    async fn token_id(&self) -> &String { &self.token_id }

    async fn metadata_url(&self) -> &Option<String> { &self.metadata_url }

    async fn contract_index(&self) -> i64 { self.contract_index }

    async fn contract_sub_index(&self) -> i64 { self.contract_sub_index }

    async fn contract_address_formatted(&self, ctx: &Context<'_>) -> ApiResult<String> {
        Ok(format!("<{},{}>", self.contract_index, self.contract_sub_index))
    }

    async fn accounts(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<AccountsCollectionSegment> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let min_index = i64::try_from(skip.unwrap_or(0))?;
        let limit =
            i64::try_from(take.map_or(config.token_holder_addresses_collection_limit, |t| {
                config.token_holder_addresses_collection_limit.min(t)
            }))?;

        // Tokens with 0 balance are filtered out. We still display tokens with a
        // negative balance (buggy cis2 smart contract) to help smart contract
        // developers to debug their smart contracts.
        // Excluding tokens with 0 balances does not scale well for smart contracts
        // with a large number of account token holdings currently. We might need to
        // introduce a specific index to optimize this query in the future.
        let items_interim = sqlx::query_as!(
            AccountTokenInterim,
            "WITH filtered_tokens AS (
                SELECT
                    token_id,
                    contract_index,
                    contract_sub_index,
                    balance AS raw_balance,
                    account_index AS account_id,
                    ROW_NUMBER() OVER (ORDER BY account_tokens.index) AS row_num
                FROM account_tokens
                JOIN tokens
                    ON tokens.contract_index = $1
                    AND tokens.contract_sub_index = $2
                    AND tokens.token_id = $3
                    AND tokens.index = account_tokens.token_index
                WHERE account_tokens.balance != 0
            )
            SELECT
                token_id,
                contract_index,
                contract_sub_index,
                raw_balance,
                account_id,
                row_num
            FROM filtered_tokens
            WHERE row_num > $4
            LIMIT $5
        ",
            self.contract_index,
            self.contract_sub_index,
            self.token_id,
            min_index,
            limit + 1
        )
        .fetch_all(pool)
        .await?;

        let mut items: Vec<AccountToken> = items_interim
            .into_iter()
            .map(AccountToken::try_from)
            .collect::<Result<Vec<AccountToken>, ApiError>>()?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = min_index > 0;

        // Tokens with 0 balance are filtered out. We still display tokens with a
        // negative balance (buggy cis2 smart contract) to help smart contract
        // developers to debug their smart contracts.
        // This counting approach below does not scale well for smart contracts
        // with a large number of account token holdings currently, since a large
        // number of rows would be traversed. This might have to be improved in the
        // future by indexing more.
        let total_count: i32 = sqlx::query_scalar!(
            "SELECT
                COUNT(*)
            FROM account_tokens
                JOIN tokens 
                ON tokens.contract_index = $1 
                AND tokens.contract_sub_index = $2 
                AND tokens.token_id = $3
            WHERE tokens.index = account_tokens.token_index 
                AND account_tokens.balance != 0",
            self.contract_index,
            self.contract_sub_index,
            self.token_id,
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0)
        .try_into()?;

        Ok(AccountsCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count,
            items,
        })
    }
}

/// A segment of a collection.
#[derive(SimpleObject)]
pub struct TokensCollectionSegment {
    /// Information to aid in pagination.
    pub page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    pub items:       Vec<Token>,
    pub total_count: i32,
}

/// A segment of a collection.
#[derive(SimpleObject)]
pub struct AccountsCollectionSegment {
    /// Information to aid in pagination.
    pub page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    pub items:       Vec<AccountToken>,
    pub total_count: i32,
}
#[derive(SimpleObject)]
#[graphql(complex)]
pub struct AccountToken {
    // The value is used for pagination/sorting in some queries.
    #[graphql(skip)]
    pub row_num:            i64,
    pub token_id:           String,
    pub contract_index:     i64,
    pub contract_sub_index: i64,
    #[graphql(skip)]
    pub raw_balance:        bigdecimal::BigDecimal,
    pub account_id:         i64,
}
#[ComplexObject]
impl AccountToken {
    async fn token(&self, ctx: &Context<'_>) -> ApiResult<Token> {
        Token::query_by_contract_and_id(
            get_pool(ctx)?,
            self.contract_index,
            self.contract_sub_index,
            &self.token_id,
        )
        .await
    }

    async fn account<'a>(&self, ctx: &Context<'a>) -> ApiResult<Account> {
        Account::query_by_index(get_pool(ctx)?, self.account_id).await?.ok_or(ApiError::NotFound)
    }

    async fn balance(&self, ctx: &Context<'_>) -> ApiResult<BigInteger> {
        Ok(BigInteger::from(self.raw_balance.clone()))
    }
}

// Interim struct used to fetch AccountToken data from the database
pub struct AccountTokenInterim {
    // This value is used for pagination/sorting in some queries. Although it is always guaranteed
    // to have a value in the way our queries are constructed, SQLx infers ROW_NUMBER() as
    // nullable and the corresponding `Option` type in Rust has to be used here.
    pub row_num:            Option<i64>,
    pub token_id:           String,
    pub contract_index:     i64,
    pub contract_sub_index: i64,
    pub raw_balance:        bigdecimal::BigDecimal,
    pub account_id:         i64,
}
impl TryFrom<AccountTokenInterim> for AccountToken {
    type Error = ApiError;

    fn try_from(item: AccountTokenInterim) -> Result<Self, Self::Error> {
        let row_num = item.row_num.ok_or(ApiError::InternalError(
            "Row number is missing (None) when fetching a token".to_string(),
        ))?;

        Ok(AccountToken {
            row_num,
            token_id: item.token_id,
            contract_index: item.contract_index,
            contract_sub_index: item.contract_sub_index,
            raw_balance: item.raw_balance,
            account_id: item.account_id,
        })
    }
}
