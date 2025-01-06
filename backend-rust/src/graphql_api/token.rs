use super::{get_pool, transaction::Transaction, ApiError, ApiResult, CollectionSegmentInfo};
use crate::{
    address::ContractIndex,
    scalar_types::{BigInteger, TransactionIndex},
};
use async_graphql::{ComplexObject, Context, Object, SimpleObject};

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

        let token = sqlx::query_as!(
            Token,
            "SELECT
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
            contract_index.0 as i64,
            contract_sub_index.0 as i64,
            token_id
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        Ok(token)
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
pub struct Token {
    #[graphql(skip)]
    pub init_transaction_index: TransactionIndex,
    pub contract_index:         i64,
    pub contract_sub_index:     i64,
    pub token_id:               String,
    pub metadata_url:           Option<String>,
    #[graphql(skip)]
    pub raw_total_supply:       bigdecimal::BigDecimal,
    pub token_address:          String,
    // TODO accounts(skip: Int take: Int): AccountsCollectionSegment
    // TODO tokenEvents(skip: Int take: Int): TokenEventsCollectionSegment
}

#[ComplexObject]
impl Token {
    async fn initial_transaction(&self, ctx: &Context<'_>) -> ApiResult<Transaction> {
        Transaction::query_by_index(get_pool(ctx)?, self.init_transaction_index).await?.ok_or(
            ApiError::InternalError("Token: No transaction at init_transaction_index".to_string()),
        )
    }

    async fn total_supply(&self, ctx: &Context<'_>) -> ApiResult<BigInteger> {
        Ok(BigInteger::from(self.raw_total_supply.clone()))
    }

    async fn contract_address_formatted(&self, ctx: &Context<'_>) -> ApiResult<String> {
        Ok(format!("<{},{}>", self.contract_index, self.contract_sub_index))
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
