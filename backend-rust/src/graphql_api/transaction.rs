use super::{block::Block, get_config, get_pool, ApiError, ApiResult, ConnectionQuery};
use crate::{
    address::AccountAddress,
    scalar_types::{AccountIndex, Amount, BlockHeight, Energy, TransactionHash, TransactionIndex},
    transaction_event::Event,
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransaction, AccountTransactionType, CredentialDeploymentTransaction,
        CredentialDeploymentTransactionType, DbTransactionType, TransactionType, UpdateTransaction,
        UpdateTransactionType,
    },
};
use async_graphql::{connection, types, Context, Object, SimpleObject, Union};
use futures::TryStreamExt;
use sqlx::PgPool;
use std::{
    cmp::{max, min},
    str::FromStr,
};

#[derive(Default)]
pub struct QueryTransactions;

#[Object]
impl QueryTransactions {
    async fn transaction(&self, ctx: &Context<'_>, id: types::ID) -> ApiResult<Transaction> {
        let index: i64 = id.try_into().map_err(ApiError::InvalidIdInt)?;
        Transaction::query_by_index(get_pool(ctx)?, index).await?.ok_or(ApiError::NotFound)
    }

    async fn transaction_by_transaction_hash<'a>(
        &self,
        ctx: &Context<'a>,
        transaction_hash: TransactionHash,
    ) -> ApiResult<Transaction> {
        Transaction::query_by_hash(get_pool(ctx)?, transaction_hash)
            .await?
            .ok_or(ApiError::NotFound)
    }

    async fn transactions<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.transaction_connection_limit,
        )?;
        // The CCDScan front-end currently expects an ASC order of the nodes/edges
        // returned (outer `ORDER BY`). If the `last` input parameter is set,
        // the inner `ORDER BY` reverses the transaction order to allow the range be
        // applied starting from the last element.
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
                WHERE $1 < index AND index < $2
                ORDER BY
                    (CASE WHEN $3 THEN index END) DESC,
                    (CASE WHEN NOT $3 THEN index END) ASC
                LIMIT $4
            ) ORDER BY index ASC"#,
            query.from,
            query.to,
            query.desc,
            query.limit,
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);

        let mut page_max_index = None;
        let mut page_min_index = None;
        while let Some(tx) = row_stream.try_next().await? {
            page_max_index = Some(match page_max_index {
                None => tx.index,
                Some(current_max) => max(current_max, tx.index),
            });

            page_min_index = Some(match page_min_index {
                None => tx.index,
                Some(current_min) => min(current_min, tx.index),
            });

            connection.edges.push(connection::Edge::new(tx.index.to_string(), tx));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (page_min_index, page_max_index) {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as max_id, MIN(index) as min_id 
                    FROM transactions
                "
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.min_id.map_or(false, |db_min| db_min < page_min_id);
            connection.has_next_page = result.max_id.map_or(false, |db_max| db_max > page_max_id);
        }

        Ok(connection)
    }
}

pub struct Transaction {
    pub index: TransactionIndex,
    pub block_height: BlockHeight,
    pub hash: TransactionHash,
    pub ccd_cost: i64,
    pub energy_cost: Energy,
    pub sender_index: Option<AccountIndex>,
    pub tx_type: DbTransactionType,
    pub type_account: Option<AccountTransactionType>,
    pub type_credential_deployment: Option<CredentialDeploymentTransactionType>,
    pub type_update: Option<UpdateTransactionType>,
    pub success: bool,
    pub events: Option<sqlx::types::Json<Vec<Event>>>,
    pub reject: Option<sqlx::types::Json<TransactionRejectReason>>,
}

impl Transaction {
    pub async fn query_by_index(pool: &PgPool, index: TransactionIndex) -> ApiResult<Option<Self>> {
        let transaction = sqlx::query_as!(
            Transaction,
            r#"SELECT
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
            WHERE index = $1"#,
            index
        )
        .fetch_optional(pool)
        .await?;

        Ok(transaction)
    }

    pub async fn query_by_hash(
        pool: &PgPool,
        transaction_hash: TransactionHash,
    ) -> ApiResult<Option<Self>> {
        let transaction = sqlx::query_as!(
            Transaction,
            r#"SELECT
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
            WHERE hash = $1"#,
            transaction_hash
        )
        .fetch_optional(pool)
        .await?;
        Ok(transaction)
    }
}

#[Object]
impl Transaction {
    /// Transaction index as a string.
    async fn id(&self) -> types::ID { self.index.into() }

    async fn transaction_index(&self) -> TransactionIndex { self.index }

    async fn transaction_hash(&self) -> &TransactionHash { &self.hash }

    async fn ccd_cost(&self) -> ApiResult<Amount> { Ok(self.ccd_cost.try_into()?) }

    async fn energy_cost(&self) -> Energy { self.energy_cost }

    async fn block<'a>(&self, ctx: &Context<'a>) -> ApiResult<Block> {
        Block::query_by_height(get_pool(ctx)?, self.block_height).await
    }

    async fn sender_account_address<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<Option<AccountAddress>> {
        let Some(account_index) = self.sender_index else {
            return Ok(None);
        };
        let result = sqlx::query!("SELECT address FROM accounts WHERE index=$1", account_index)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(Some(result.address.into()))
    }

    async fn transaction_type(&self) -> ApiResult<TransactionType> {
        let tt = match self.tx_type {
            DbTransactionType::Account => TransactionType::AccountTransaction(AccountTransaction {
                account_transaction_type: self.type_account,
            }),
            DbTransactionType::CredentialDeployment => {
                TransactionType::CredentialDeploymentTransaction(CredentialDeploymentTransaction {
                    credential_deployment_transaction_type: self.type_credential_deployment.ok_or(
                        ApiError::InternalError(
                            "Database invariant violated, transaction type is credential \
                             deployment, but credential deployment type is null"
                                .to_string(),
                        ),
                    )?,
                })
            }
            DbTransactionType::Update => TransactionType::UpdateTransaction(UpdateTransaction {
                update_transaction_type: self.type_update.ok_or(ApiError::InternalError(
                    "Database invariant violated, transaction type is update, but update type is \
                     null"
                        .to_string(),
                ))?,
            }),
        };
        Ok(tt)
    }

    async fn result(&self) -> ApiResult<TransactionResult<'_>> {
        if self.success {
            let events = self
                .events
                .as_ref()
                .ok_or(ApiError::InternalError("Success events is null".to_string()))?;
            Ok(TransactionResult::Success(Success {
                events,
            }))
        } else {
            let reason = self
                .reject
                .as_ref()
                .ok_or(ApiError::InternalError("Success events is null".to_string()))?;
            Ok(TransactionResult::Rejected(Rejected {
                reason,
            }))
        }
    }
}

#[derive(Union)]
enum TransactionResult<'a> {
    Success(Success<'a>),
    Rejected(Rejected<'a>),
}

struct Success<'a> {
    events: &'a Vec<Event>,
}
#[Object]
impl Success<'_> {
    async fn events(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, &Event>> {
        if first.is_some() && last.is_some() {
            return Err(ApiError::QueryConnectionFirstLast);
        }
        let mut start = if let Some(after) = after.as_ref() {
            usize::from_str(after.as_str())?
        } else {
            0
        };
        let mut end = if let Some(before) = before.as_ref() {
            usize::from_str(before.as_str())?
        } else {
            self.events.len()
        };
        if let Some(first) = first {
            end = usize::min(end, start + first);
        }
        if let Some(last) = last {
            if let Some(new_end) = end.checked_sub(last) {
                start = usize::max(start, new_end);
            }
        }
        let mut connection = connection::Connection::new(start == 0, end == self.events.len());
        connection.edges = self.events[start..end]
            .iter()
            .enumerate()
            .map(|(i, event)| connection::Edge::new(i.to_string(), event))
            .collect();
        Ok(connection)
    }
}

#[derive(SimpleObject)]
struct Rejected<'a> {
    reason: &'a TransactionRejectReason,
}
