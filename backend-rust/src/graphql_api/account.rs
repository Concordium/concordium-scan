use super::{
    baker::Baker, get_config, get_pool, token::AccountToken, AccountStatementEntryType, ApiError,
    ApiResult, ConnectionQuery, OrderDir,
};
use crate::{
    address::AccountAddress,
    connection::{ConnectionBounds, Reversed},
    graphql_api::{block::Block, token::AccountTokenInterim, Transaction},
    scalar_types::{AccountIndex, Amount, BlockHeight, DateTime, TransactionIndex},
    transaction_event::{
        delegation::{BakerDelegationTarget, DelegationTarget, PassiveDelegationTarget},
        Event,
    },
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use anyhow::anyhow;
use async_graphql::{
    connection::{self, CursorType},
    types, ComplexObject, Context, Enum, InputObject, Object, SimpleObject, Union,
};
use futures::TryStreamExt;
use sqlx::PgPool;
use std::cmp::{max, min, Ordering};

#[derive(Default)]
pub(crate) struct QueryAccounts;

#[Object]
#[allow(clippy::too_many_arguments)]
impl QueryAccounts {
    async fn account<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Account> {
        let index: i64 = id.try_into().map_err(ApiError::InvalidIdInt)?;
        Account::query_by_index(get_pool(ctx)?, index).await?.ok_or(ApiError::NotFound)
    }

    async fn account_by_address<'a>(
        &self,
        ctx: &Context<'a>,
        account_address: String,
    ) -> ApiResult<Account> {
        Account::query_by_address(get_pool(ctx)?, account_address).await?.ok_or(ApiError::NotFound)
    }

    async fn accounts(
        &self,
        ctx: &Context<'_>,
        #[graphql(default)] sort: AccountSort,
        filter: Option<AccountFilterInput>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Account>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;

        let order: AccountOrder = sort.into();

        let include_only_delegators = filter.map(|f| f.is_delegator).unwrap_or_default();

        if matches!(order.dir, OrderDir::Desc) {
            // Use a `descending` cursor.
            let query = ConnectionQuery::<AccountFieldDescCursor>::new(
                first,
                after,
                last,
                before,
                config.account_connection_limit,
            )?;

            let mut accounts = sqlx::query_as!(
                Account,
                r"SELECT * FROM (
                SELECT
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
                    -- Filter for only the accounts that are within the
                    -- range that correspond to the requested page.
                    -- The first condition is true only if we don't order by that field.
                    -- Then the whole OR condition will be true, so the filter for that
                    -- field will be ignored.
                    (NOT $3 OR index < $1 AND index > $2) AND
                    (NOT $4 OR 
                        (
                            amount < $10 AND amount > $11 OR 
                            (amount = $11 AND accounts.index > $2)
                            OR (amount = $10 AND accounts.index < $1)
                        )
                    ) AND
                    (NOT $5 OR 
                        (
                            num_txs < $10 AND num_txs > $11 OR 
                            (num_txs = $11 AND accounts.index > $2)
                            OR (num_txs = $10 AND accounts.index < $1)
                        )
                    ) AND
                    (NOT $6 OR 
                        (
                            delegated_stake < $10 AND delegated_stake > $11 OR 
                            (delegated_stake = $11 AND accounts.index > $2)
                            OR (delegated_stake = $10 AND accounts.index < $1)
                        )
                    ) AND
                    -- Need to filter for only delegators if the user requests this.
                    (NOT $7 OR delegated_stake > 0)
                ORDER BY
                    -- Order primarily by the field requested. Depending on the order of the collection
                    -- and whether it is the first or last being queried, this sub-query must
                    -- order by:
                    --
                    -- | Collection | Operation | Sub-query |
                    -- |------------|-----------|-----------|
                    -- | ASC        | first     | ASC       |
                    -- | DESC       | first     | DESC      |
                    -- | ASC        | last      | DESC      |
                    -- | DESC       | last      | ASC       |
                    --
                    -- Note that `$8` below represents `is_desc != is_last`.
                    --
                    -- The first condition is true if we order by that field.
                    -- Otherwise false, which makes the CASE null, which means
                    -- it will not affect the ordering at all.
                    -- The `AccountOrderField::Age` is not mention here because its 
                    -- sorting instruction is equivallent and would be repeated in the next step.
                    (CASE WHEN $4 AND $8     THEN amount          END) DESC,
                    (CASE WHEN $4 AND NOT $8 THEN amount          END) ASC,
                    (CASE WHEN $5 AND $8     THEN num_txs         END) DESC,
                    (CASE WHEN $5 AND NOT $8 THEN num_txs         END) ASC,
                    (CASE WHEN $6 AND $8     THEN delegated_stake END) DESC,
                    (CASE WHEN $6 AND NOT $8 THEN delegated_stake END) ASC,
                    -- Since after the ordring above, there may exists elements with the same field value,
                    -- apply a second ordering by the unique `account_id` (index) in addition. 
                    -- This ensures a strict ordering of elements as the `AccountFieldDescCursor` defines. 
                    (CASE WHEN $8     THEN index           END) DESC,
                    (CASE WHEN NOT $8 THEN index           END) ASC
                LIMIT $9
            )
            -- We need to order each page still, as we only use the DESC/ASC ordering above
            -- to select page items from the start/end of the range.
            -- Each page must still independently be ordered.
            -- See also https://relay.dev/graphql/connections.htm#sec-Edge-order
            ORDER BY
                (CASE WHEN $3     THEN index           END) DESC,
                (CASE WHEN $4     THEN amount          END) DESC,
                (CASE WHEN $5     THEN num_txs         END) DESC,
                (CASE WHEN $6     THEN delegated_stake END) DESC,
                index DESC
            ",
                query.from.account_id,                            // $1
                query.to.account_id,                              // $2
                matches!(order.field, AccountOrderField::Age),    // $3
                matches!(order.field, AccountOrderField::Amount), // $4
                matches!(order.field, AccountOrderField::TransactionCount), // $5
                matches!(order.field, AccountOrderField::DelegatedStake), // $6
                include_only_delegators,                          // $7
                query.is_last != true,                            // $8
                query.limit,                                      // $9
                query.from.field,                                 // $10
                query.to.field,                                   // $11
            )
            .fetch(pool);

            let mut connection = connection::Connection::new(false, false);

            while let Some(account) = accounts.try_next().await? {
                let cursor = AccountFieldDescCursor::new(order.field, &account);

                connection.edges.push(connection::Edge::new(cursor.encode_cursor(), account));
            }

            if let (Some(edge_min_index), Some(edge_max_index)) =
                (connection.edges.first(), connection.edges.last())
            {
                let bounds = sqlx::query!(
                    "WITH
                        min_account as (
                            SELECT index, 
                                CASE 
                                    WHEN $2 THEN index 
                                    WHEN $3 THEN amount
                                    WHEN $4 THEN num_txs
                                    WHEN $5 THEN delegated_stake
                                ELSE NULL 
                            END AS min_value
                            FROM accounts
                            WHERE 
                                -- Need to filter for only delegators if the user requests this.
                                (NOT $1 OR delegated_stake > 0)
                            ORDER BY min_value DESC, index DESC
                            LIMIT 1
                        ),
                        max_account as (
                            SELECT 
                                index,
                                CASE 
                                    WHEN $2 THEN index 
                                    WHEN $3 THEN amount
                                    WHEN $4 THEN num_txs
                                    WHEN $5 THEN delegated_stake
                                ELSE NULL 
                                END AS max_value
                            FROM accounts
                            WHERE 
                                -- Need to filter for only delegators if the user requests this.
                                (NOT $1 OR delegated_stake > 0)
                            ORDER BY max_value ASC, index ASC
                            LIMIT 1
                        )
                    SELECT
                        min_account.index AS min_index,
                        min_account.min_value AS min_value,
                        max_account.index AS max_index,
                        max_account.max_value AS max_value
                    FROM min_account, max_account",
                    include_only_delegators,                          // $1
                    matches!(order.field, AccountOrderField::Age),    // $2
                    matches!(order.field, AccountOrderField::Amount), // $3
                    matches!(order.field, AccountOrderField::TransactionCount), // $4
                    matches!(order.field, AccountOrderField::DelegatedStake), // $5
                )
                .fetch_optional(pool)
                .await?;

                if let Some(bounds) = bounds {
                    if let (Some(min_value), Some(max_value), max_index, min_index) =
                        (bounds.min_value, bounds.max_value, bounds.max_index, bounds.min_index)
                    {
                        let collection_start_cursor = AccountFieldDescCursor {
                            account_id: min_index,
                            field:      min_value,
                        };

                        let collection_end_cursor = AccountFieldDescCursor {
                            account_id: max_index,
                            field:      max_value,
                        };

                        let page_start_cursor =
                            AccountFieldDescCursor::new(order.field, &edge_min_index.node);
                        let page_end_cursor =
                            AccountFieldDescCursor::new(order.field, &edge_max_index.node);

                        connection.has_previous_page = collection_start_cursor < page_start_cursor;
                        connection.has_next_page = collection_end_cursor > page_end_cursor;
                    }
                }
            }
            Ok(connection)
        } else {
            // Use a `ascending` cursor.
            let query = ConnectionQuery::<Reversed<AccountFieldDescCursor>>::new(
                first,
                after,
                last,
                before,
                config.account_connection_limit,
            )?;

            let mut accounts = sqlx::query_as!(
                Account,
                r"SELECT * FROM (
                    SELECT
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
                        -- Filter for only the accounts that are within the
                        -- range that correspond to the requested page.
                        -- The first condition is true only if we don't order by that field.
                        -- Then the whole OR condition will be true, so the filter for that
                        -- field will be ignored.
                        (NOT $3 OR index > $1 AND index < $2) AND
                        (NOT $4 OR 
                            (
                                amount > $10 AND amount < $11 OR 
                                (amount = $11 AND accounts.index > $2)
                                OR (amount = $10 AND accounts.index < $1)
                            )
                        ) AND
                        (NOT $5 OR 
                            (
                                num_txs > $10 AND num_txs < $11 OR 
                                (num_txs = $11 AND accounts.index > $2)
                                OR (num_txs = $10 AND accounts.index < $1)
                            )
                        ) AND
                        (NOT $6 OR 
                            (
                                delegated_stake > $10 AND delegated_stake < $11 OR 
                                (delegated_stake = $11 AND accounts.index < $2)
                                OR (delegated_stake = $10 AND accounts.index > $1)
                            )
                        ) AND
                        -- Need to filter for only delegators if the user requests this.
                        (NOT $7 OR delegated_stake > 0)
                    ORDER BY
                        -- Order primarily by the field requested. Depending on the order of the collection
                        -- and whether it is the first or last being queried, this sub-query must
                        -- order by:
                        --
                        -- | Collection | Operation | Sub-query |
                        -- |------------|-----------|-----------|
                        -- | ASC        | first     | ASC       |
                        -- | DESC       | first     | DESC      |
                        -- | ASC        | last      | DESC      |
                        -- | DESC       | last      | ASC       |
                        --
                        -- Note that `$8` below represents `is_desc != is_last`.
                        --
                        -- The first condition is true if we order by that field.
                        -- Otherwise false, which makes the CASE null, which means
                        -- it will not affect the ordering at all.
                        -- The `AccountOrderField::Age` is not mention here because its 
                        -- sorting instruction is equivallent and would be repeated in the next step.
                        (CASE WHEN $4 AND $8     THEN amount          END) DESC,
                        (CASE WHEN $4 AND NOT $8 THEN amount          END) ASC,
                        (CASE WHEN $5 AND $8     THEN num_txs         END) DESC,
                        (CASE WHEN $5 AND NOT $8 THEN num_txs         END) ASC,
                        (CASE WHEN $6 AND $8     THEN delegated_stake END) DESC,
                        (CASE WHEN $6 AND NOT $8 THEN delegated_stake END) ASC,
                        -- Since after the ordring above, there may exists elements with the same field value,
                        -- apply a second ordering by the unique `account_id` (index) in addition. 
                        -- This ensures a strict ordering of elements as the `AccountFieldDescCursor` defines. 
                        (CASE WHEN $8     THEN index           END) DESC,
                        (CASE WHEN NOT $8 THEN index           END) ASC
                    LIMIT $9
                )
                -- We need to order each page still, as we only use the DESC/ASC ordering above
                -- to select page items from the start/end of the range.
                -- Each page must still independently be ordered.
                -- See also https://relay.dev/graphql/connections.htm#sec-Edge-order
                ORDER BY
                    (CASE WHEN $3     THEN index           END) ASC,
                    (CASE WHEN $4     THEN amount          END) ASC,
                    (CASE WHEN $5     THEN num_txs         END) ASC,
                    (CASE WHEN $6     THEN delegated_stake END) ASC,
                    index ASC
                ",
                query.from.cursor.account_id,                     // $1
                query.to.cursor.account_id,                       // $2
                matches!(order.field, AccountOrderField::Age),    // $3
                matches!(order.field, AccountOrderField::Amount), // $4
                matches!(order.field, AccountOrderField::TransactionCount), // $5
                matches!(order.field, AccountOrderField::DelegatedStake), // $6
                include_only_delegators,                          // $7
                query.is_last != false,                           // $8
                query.limit,                                      // $9
                query.from.cursor.field,                          // $10
                query.to.cursor.field,                            // $11
            )
            .fetch(pool);

            let mut connection = connection::Connection::new(false, false);

            while let Some(account) = accounts.try_next().await? {
                let cursor = Reversed::<AccountFieldDescCursor> {
                    cursor: AccountFieldDescCursor::new(order.field, &account),
                };

                connection.edges.push(connection::Edge::new(cursor.encode_cursor(), account));
            }

            if let (Some(edge_min_index), Some(edge_max_index)) =
                (connection.edges.first(), connection.edges.last())
            {
                let bounds = sqlx::query!(
                    "WITH
                        min_account as (
                            SELECT index, 
                                CASE 
                                    WHEN $2 THEN index 
                                    WHEN $3 THEN amount
                                    WHEN $4 THEN num_txs
                                    WHEN $5 THEN delegated_stake
                                ELSE NULL 
                            END AS min_value
                            FROM accounts
                            WHERE 
                                -- Need to filter for only delegators if the user requests this.
                                (NOT $1 OR delegated_stake > 0)
                            ORDER BY min_value ASC, index ASC
                            LIMIT 1
                        ),
                        max_account as (
                            SELECT 
                                index,
                                CASE 
                                    WHEN $2 THEN index 
                                    WHEN $3 THEN amount
                                    WHEN $4 THEN num_txs
                                    WHEN $5 THEN delegated_stake
                                ELSE NULL 
                                END AS max_value
                            FROM accounts
                            WHERE 
                                -- Need to filter for only delegators if the user requests this.
                                (NOT $1 OR delegated_stake > 0)
                            ORDER BY max_value DESC, index DESC
                            LIMIT 1
                        )
                    SELECT
                        min_account.index AS min_index,
                        min_account.min_value AS min_value,
                        max_account.index AS max_index,
                        max_account.max_value AS max_value
                    FROM min_account, max_account",
                    include_only_delegators,                          // $1
                    matches!(order.field, AccountOrderField::Age),    // $2
                    matches!(order.field, AccountOrderField::Amount), // $3
                    matches!(order.field, AccountOrderField::TransactionCount), // $4
                    matches!(order.field, AccountOrderField::DelegatedStake), // $5
                )
                .fetch_optional(pool)
                .await?;

                if let Some(bounds) = bounds {
                    if let (Some(min_value), Some(max_value), max_index, min_index) =
                        (bounds.min_value, bounds.max_value, bounds.max_index, bounds.min_index)
                    {
                        let collection_start_cursor = Reversed::<AccountFieldDescCursor> {
                            cursor: AccountFieldDescCursor {
                                account_id: min_index,
                                field:      min_value,
                            },
                        };

                        let collection_end_cursor = Reversed::<AccountFieldDescCursor> {
                            cursor: AccountFieldDescCursor {
                                account_id: max_index,
                                field:      max_value,
                            },
                        };

                        let page_start_cursor = Reversed::<AccountFieldDescCursor> {
                            cursor: AccountFieldDescCursor::new(order.field, &edge_min_index.node),
                        };
                        let page_end_cursor = Reversed::<AccountFieldDescCursor> {
                            cursor: AccountFieldDescCursor::new(order.field, &edge_max_index.node),
                        };

                        connection.has_previous_page = collection_start_cursor < page_start_cursor;
                        connection.has_next_page = collection_end_cursor > page_end_cursor;
                    }
                }
            }
            Ok(connection)
        }
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
pub struct AccountReward {
    #[graphql(skip)]
    id:           i64,
    #[graphql(skip)]
    block_height: BlockHeight,
    timestamp:    DateTime,
    #[graphql(skip)]
    entry_type:   AccountStatementEntryType,
    #[graphql(skip)]
    amount:       i64,
}
#[ComplexObject]
impl AccountReward {
    async fn id(&self) -> types::ID { types::ID::from(self.id) }

    async fn block(&self, ctx: &Context<'_>) -> ApiResult<Block> {
        Block::query_by_height(get_pool(ctx)?, self.block_height).await
    }

    async fn amount(&self) -> ApiResult<Amount> { Ok(self.amount.try_into()?) }

    async fn reward_type(&self) -> ApiResult<RewardType> {
        let transaction: RewardType = self.entry_type.try_into().map_err(|_| {
            ApiError::InternalError(format!(
                "AccountStatementEntryType: Not a valid reward type: {}",
                &self.entry_type
            ))
        })?;
        Ok(transaction)
    }
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "reward_type")]
#[allow(clippy::enum_variant_names)]
pub enum RewardType {
    FinalizationReward,
    FoundationReward,
    BakerReward,
    TransactionFeeReward,
}

impl TryFrom<AccountStatementEntryType> for RewardType {
    type Error = anyhow::Error;

    fn try_from(value: AccountStatementEntryType) -> Result<Self, Self::Error> {
        match value {
            AccountStatementEntryType::FinalizationReward => Ok(RewardType::FinalizationReward),
            AccountStatementEntryType::FoundationReward => Ok(RewardType::FoundationReward),
            AccountStatementEntryType::BakerReward => Ok(RewardType::BakerReward),
            AccountStatementEntryType::TransactionFeeReward => Ok(RewardType::TransactionFeeReward),
            other => Err(anyhow!(
                "AccountStatementEntryType '{}' cannot be converted to RewardType",
                other
            )),
        }
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct AccountStatementEntry {
    #[graphql(skip)]
    id:              i64,
    timestamp:       DateTime,
    entry_type:      AccountStatementEntryType,
    #[graphql(skip)]
    amount:          i64,
    #[graphql(skip)]
    account_balance: i64,
    #[graphql(skip)]
    transaction_id:  Option<TransactionIndex>,
    #[graphql(skip)]
    block_height:    BlockHeight,
}

#[ComplexObject]
impl AccountStatementEntry {
    async fn id(&self) -> types::ID { types::ID::from(self.id) }

    async fn amount(&self) -> ApiResult<Amount> { Ok(self.amount.try_into()?) }

    async fn account_balance(&self) -> ApiResult<Amount> { Ok(self.account_balance.try_into()?) }

    async fn reference(&self, ctx: &Context<'_>) -> ApiResult<BlockOrTransaction> {
        if let Some(id) = self.transaction_id {
            let transaction = Transaction::query_by_index(get_pool(ctx)?, id).await?;
            let transaction = transaction.ok_or_else(|| {
                ApiError::InternalError(
                    "AccountStatementEntry: No transaction at transaction_index".to_string(),
                )
            })?;
            Ok(BlockOrTransaction::Transaction(transaction))
        } else {
            Ok(BlockOrTransaction::Block(
                Block::query_by_height(get_pool(ctx)?, self.block_height).await?,
            ))
        }
    }
}

#[derive(SimpleObject)]
struct AccountTransactionRelation {
    transaction: Transaction,
}

type AccountReleaseScheduleItemIndex = i64;

struct AccountReleaseScheduleItem {
    /// Table index
    /// Used for the cursor in the connection
    index:             AccountReleaseScheduleItemIndex,
    transaction_index: TransactionIndex,
    timestamp:         DateTime,
    amount:            i64,
}
#[Object]
impl AccountReleaseScheduleItem {
    async fn transaction(&self, ctx: &Context<'_>) -> ApiResult<Transaction> {
        Transaction::query_by_index(get_pool(ctx)?, self.transaction_index).await?.ok_or(
            ApiError::InternalError(
                "AccountReleaseScheduleItem: No transaction at transaction_index".to_string(),
            ),
        )
    }

    async fn timestamp(&self) -> DateTime { self.timestamp }

    async fn amount(&self) -> ApiResult<Amount> { Ok(self.amount.try_into()?) }
}

/// Cursor for `Query::accounts` when sorting by the account by some
/// field, like the `delegated_stake`, `transaction_count` or `amount`.
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
struct AccountFieldDescCursor {
    /// The cursor value of the relevant sorting field.
    field:      i64,
    /// The account id of the cursor.
    account_id: i64,
}
impl AccountFieldDescCursor {
    fn delegated_stake(row: &Account) -> Self {
        AccountFieldDescCursor {
            field:      row.delegated_stake,
            account_id: row.index,
        }
    }

    fn index(row: &Account) -> Self {
        AccountFieldDescCursor {
            field:      row.index,
            account_id: row.index,
        }
    }

    fn amount(row: &Account) -> Self {
        AccountFieldDescCursor {
            field:      row.amount,
            account_id: row.index,
        }
    }

    fn transaction_count(row: &Account) -> Self {
        AccountFieldDescCursor {
            field:      row.num_txs,
            account_id: row.index,
        }
    }

    fn new(field: AccountOrderField, node: &Account) -> Self {
        match field {
            AccountOrderField::Age => Self::index(node),
            AccountOrderField::Amount => Self::amount(node),
            AccountOrderField::TransactionCount => Self::transaction_count(node),
            AccountOrderField::DelegatedStake => Self::delegated_stake(node),
        }
    }
}

/// Decode the cursor from a string.
///
/// The format of the cursor is `({before}:{after})` where `{before}` is
/// the `field` to represent the `Age`, `Amount`, `TransactionCount`
/// or `DelegatedStake`, and `{after}` is
/// the cursor for `account_id` which is unique.
impl connection::CursorType for AccountFieldDescCursor {
    type Error = DecodeAccountFieldCursor;

    fn decode_cursor(s: &str) -> Result<Self, Self::Error> {
        let (before, after) = s.split_once(':').ok_or(DecodeAccountFieldCursor::NoSemicolon)?;
        let field: i64 = before.parse().map_err(DecodeAccountFieldCursor::ParseField)?;
        let account_id: i64 = after.parse().map_err(DecodeAccountFieldCursor::ParseAccountId)?;
        Ok(Self {
            field,
            account_id,
        })
    }

    fn encode_cursor(&self) -> String { format!("{}:{}", self.field, self.account_id) }
}

#[derive(Debug, thiserror::Error)]
enum DecodeAccountFieldCursor {
    #[error("Cursor must contain a semicolon.")]
    NoSemicolon,
    #[error("Cursor must contain valid validator account ID.")]
    ParseAccountId(std::num::ParseIntError),
    #[error("Cursor must contain valid field.")]
    ParseField(std::num::ParseIntError),
}

impl ConnectionBounds for AccountFieldDescCursor {
    const END_BOUND: Self = Self {
        account_id: i64::MIN,
        field:      i64::MIN,
    };
    const START_BOUND: Self = Self {
        account_id: i64::MAX,
        field:      i64::MAX,
    };
}

impl PartialOrd for AccountFieldDescCursor {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> { Some(self.cmp(other)) }
}

/// The cursor contains a `field` to represent the `Age`, `Amount`,
/// `TransactionCount` or `DelegatedStake` which is NOT unique and an
/// `account_id` which is unique. The primary ording is defined by the field
/// value. Among elements with the same field value, the unique `account_id`
/// defines the ordering. Since the `account_id` is unique, a strict ordering of
/// elements in the `AccountFieldDescCursor` is defined.
impl Ord for AccountFieldDescCursor {
    fn cmp(&self, other: &Self) -> std::cmp::Ordering {
        let ordering = other.field.cmp(&self.field);
        if let Ordering::Equal = ordering {
            other.account_id.cmp(&self.account_id)
        } else {
            ordering
        }
    }
}

impl From<DecodeAccountFieldCursor> for ApiError {
    fn from(err: DecodeAccountFieldCursor) -> Self {
        ApiError::InvalidCursorFormat(err.to_string())
    }
}

pub struct Account {
    /// Index of the account.
    pub index: i64,
    /// Index of the transaction creating this account.
    /// Only `None` for genesis accounts.
    pub transaction_index: Option<i64>,
    /// The address of the account in Base58Check.
    pub address: AccountAddress,
    /// The total amount of CCD hold by the account.
    pub amount: i64,
    /// The total delegated stake of this account.
    pub delegated_stake: i64,
    /// The total number of transactions this account has been involved in or
    /// affected by.
    pub num_txs: i64,
    /// Flag indicating whether we are re-staking earnings. `None` means the
    /// account is not delegating to a baker and not using passive
    /// delegation.
    pub delegated_restake_earnings: Option<bool>,
    /// Target id of the baker. An account can delegate stake to at most one
    /// baker pool. When this is `None` it means that we are using
    /// passive delegation.
    pub delegated_target_baker_id: Option<i64>,
}
impl Account {
    pub async fn query_by_index(pool: &PgPool, index: AccountIndex) -> ApiResult<Option<Self>> {
        let account = sqlx::query_as!(
            Account,
            "SELECT index, transaction_index, address, amount, delegated_stake, num_txs, \
             delegated_restake_earnings, delegated_target_baker_id
            FROM accounts
            WHERE index = $1",
            index
        )
        .fetch_optional(pool)
        .await?;
        Ok(account)
    }

    pub async fn query_by_address(pool: &PgPool, address: String) -> ApiResult<Option<Self>> {
        let account = sqlx::query_as!(
            Account,
            "SELECT index, transaction_index, address, amount, delegated_stake, num_txs, \
             delegated_restake_earnings, delegated_target_baker_id
            FROM accounts
            WHERE address = $1",
            address
        )
        .fetch_optional(pool)
        .await?;
        Ok(account)
    }
}

#[Object]
impl Account {
    pub async fn id(&self) -> types::ID { types::ID::from(self.index) }

    /// The address of the account in Base58Check.
    pub async fn address(&self) -> &AccountAddress { &self.address }

    /// The total amount of CCD hold by the account.
    pub async fn amount(&self) -> ApiResult<Amount> { Ok(self.amount.try_into()?) }

    pub async fn baker(&self, ctx: &Context<'_>) -> ApiResult<Option<Baker>> {
        Ok(Baker::query_by_id(get_pool(ctx)?, self.index).await?)
    }

    async fn delegation(&self) -> ApiResult<Option<Delegation>> {
        let staked_amount = self.delegated_stake.try_into()?;
        Ok(self.delegated_restake_earnings.map(|restake_earnings| Delegation {
            delegator_id: self.index,
            restake_earnings,
            staked_amount,
            delegation_target: if let Some(target) = self.delegated_target_baker_id {
                DelegationTarget::BakerDelegationTarget(BakerDelegationTarget {
                    baker_id: target.into(),
                })
            } else {
                DelegationTarget::PassiveDelegationTarget(PassiveDelegationTarget {
                    dummy: false,
                })
            },
        }))
    }

    /// Timestamp of the block where this account was created.
    async fn created_at(&self, ctx: &Context<'_>) -> ApiResult<DateTime> {
        let slot_time = if let Some(transaction_index) = self.transaction_index {
            sqlx::query_scalar!(
                "SELECT slot_time
                FROM transactions
                JOIN blocks ON transactions.block_height = blocks.height
                WHERE transactions.index = $1",
                transaction_index
            )
            .fetch_one(get_pool(ctx)?)
            .await?
        } else {
            sqlx::query_scalar!(
                "SELECT slot_time
                FROM blocks
                WHERE height = 0"
            )
            .fetch_one(get_pool(ctx)?)
            .await?
        };
        Ok(slot_time)
    }

    /// Number of transactions the account has been involved in or
    /// affected by.
    async fn transaction_count<'a>(&self) -> ApiResult<i64> { Ok(self.num_txs) }

    /// Number of transactions where this account is the sender of the
    /// transaction. This is the `nonce` of the account. This value is
    /// currently not used by the front-end and the `COUNT(*)` will need further
    /// optimization if intended to be used.
    async fn nonce<'a>(&self, ctx: &Context<'a>) -> ApiResult<i64> {
        let count = sqlx::query_scalar!(
            "SELECT COUNT(*) FROM transactions WHERE sender_index = $1",
            self.index
        )
        .fetch_one(get_pool(ctx)?)
        .await?;
        Ok(count.unwrap_or(0))
    }

    async fn tokens(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountToken>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.contract_connection_limit,
        )?;

        // Tokens with 0 balance are filtered out. We still display tokens with a
        // negative balance (buggy cis2 smart contract) to help smart contract
        // developers to debug their smart contracts.
        // The tokens are sorted/filtered by the newest tokens transferred to an account
        // address.
        let mut row_stream = sqlx::query_as!(
            AccountTokenInterim,
            "
            SELECT * FROM (
                SELECT
                    token_id,
                    contract_index,
                    contract_sub_index,
                    balance AS raw_balance,
                    account_index AS account_id,
                    change_seq
                FROM account_tokens
                JOIN tokens
                    ON tokens.index = account_tokens.token_index
                WHERE account_tokens.balance != 0 
                    AND account_tokens.account_index = $5
                    AND $1 < change_seq 
                    AND change_seq < $2
                ORDER BY
                    CASE WHEN NOT $4 THEN change_seq END DESC,
                    CASE WHEN $4 THEN change_seq END ASC
                LIMIT $3
            ) ORDER BY change_seq DESC
            ",
            query.from,
            query.to,
            query.limit,
            query.is_last,
            &self.index
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);

        let mut page_max_index = None;
        let mut page_min_index = None;
        while let Some(token) = row_stream.try_next().await? {
            let token = AccountToken::try_from(token)?;

            page_max_index = Some(match page_max_index {
                None => token.change_seq,
                Some(current_max) => max(current_max, token.change_seq),
            });

            page_min_index = Some(match page_min_index {
                None => token.change_seq,
                Some(current_min) => min(current_min, token.change_seq),
            });

            connection.edges.push(connection::Edge::new(token.change_seq.to_string(), token));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (page_min_index, page_max_index) {
            let result = sqlx::query!(
                "
                    SELECT MAX(change_seq) as max_id, MIN(change_seq) as min_id 
                    FROM account_tokens
                    WHERE account_tokens.balance != 0
                        AND account_tokens.account_index = $1
                ",
                &self.index
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.min_id.map_or(false, |db_min| db_min < page_min_id);
            connection.has_next_page = result.max_id.map_or(false, |db_max| db_max > page_max_id);
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
    ) -> ApiResult<connection::Connection<String, AccountTransactionRelation>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.contract_connection_limit,
        )?;

        let mut txs = sqlx::query_as!(
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
                FROM transactions t
                LEFT JOIN (
                    SELECT transaction_index
                    FROM affected_accounts
                    WHERE account_index = $1
                ) a ON a.transaction_index = t.index
                WHERE
                    $2 < index
                    AND index < $3
                ORDER BY
                    (CASE WHEN $4 THEN index END) DESC,
                    (CASE WHEN NOT $4 THEN index END) ASC
                LIMIT $5
            ) ORDER BY index ASC
            "#,
            self.index,
            query.from,
            query.to,
            query.is_last,
            query.limit,
        )
        .fetch(pool);

        let has_previous_page = sqlx::query_scalar!(
            "SELECT true
            FROM transactions
            WHERE
                $1 IN (
                    SELECT account_index
                    FROM affected_accounts
                    WHERE transaction_index = index
                )
                AND index <= $2
            LIMIT 1",
            self.index,
            query.from,
        )
        .fetch_optional(pool)
        .await?
        .flatten()
        .unwrap_or_default();

        let has_next_page = sqlx::query_scalar!(
            "SELECT true
            FROM transactions
            WHERE
                $1 IN (
                    SELECT account_index
                    FROM affected_accounts
                    WHERE transaction_index = index
                )
                AND $2 <= index
            LIMIT 1",
            self.index,
            query.to,
        )
        .fetch_optional(pool)
        .await?
        .flatten()
        .unwrap_or_default();

        let mut connection = connection::Connection::new(has_previous_page, has_next_page);

        while let Some(tx) = txs.try_next().await? {
            connection.edges.push(connection::Edge::new(
                tx.index.to_string(),
                AccountTransactionRelation {
                    transaction: tx,
                },
            ));
        }

        Ok(connection)
    }

    async fn account_statement(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountStatementEntry>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.account_statements_connection_limit,
        )?;

        let mut account_statements = sqlx::query_as!(
            AccountStatementEntry,
            r#"
            SELECT *
            FROM (
                SELECT
                    id,
                    amount,
                    entry_type as "entry_type: AccountStatementEntryType",
                    blocks.slot_time as timestamp,
                    account_balance,
                    transaction_id,
                    block_height
                FROM
                    account_statements
                JOIN
                    blocks
                ON
                    blocks.height = account_statements.block_height
                WHERE
                    account_index = $5
                    AND id > $1
                    AND id < $2
                ORDER BY
                    (CASE WHEN $4 THEN id END) DESC,
                    (CASE WHEN NOT $4 THEN id END) ASC
                LIMIT $3
            )
            ORDER BY
                id ASC
            "#,
            query.from,
            query.to,
            query.limit,
            query.is_last,
            &self.index
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        let mut min_index = None;
        let mut max_index = None;
        while let Some(statement) = account_statements.try_next().await? {
            min_index = Some(match min_index {
                None => statement.id,
                Some(current_min) => min(current_min, statement.id),
            });

            max_index = Some(match max_index {
                None => statement.id,
                Some(current_max) => max(current_max, statement.id),
            });
            connection.edges.push(connection::Edge::new(statement.id.to_string(), statement));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (min_index, max_index) {
            let result = sqlx::query!(
                r#"
                    SELECT MAX(id) as max_id, MIN(id) as min_id
                    FROM account_statements
                    WHERE account_index = $1
                "#,
                &self.index
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.min_id.map_or(false, |db_min| db_min < page_min_id);
            connection.has_next_page = result.max_id.map_or(false, |db_max| db_max > page_max_id);
        }

        Ok(connection)
    }

    async fn rewards(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountReward>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.reward_connection_limit,
        )?;
        let mut rewards = sqlx::query_as!(
            AccountReward,
            r#"
            SELECT
                id as "id!",
                block_height as "block_height!",
                timestamp,
                entry_type as "entry_type!: AccountStatementEntryType",
                amount as "amount!"
            FROM (
                SELECT
                    id,
                    block_height,
                    blocks.slot_time as "timestamp",
                    entry_type,
                    amount
                FROM account_statements
                JOIN
                    blocks
                ON
                    blocks.height = account_statements.block_height
                WHERE
                    entry_type IN (
                        'FinalizationReward',
                        'FoundationReward',
                        'BakerReward',
                        'TransactionFeeReward'
                    )
                    AND account_index = $5
                    AND id > $1
                    AND id < $2
                ORDER BY
                    CASE WHEN $4 THEN id END DESC,
                    CASE WHEN NOT $4 THEN id END ASC
                LIMIT $3
            )
            ORDER BY
                id ASC
            "#,
            query.from,
            query.to,
            query.limit,
            query.is_last,
            &self.index
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);
        let mut min_index = None;
        let mut max_index = None;
        while let Some(statement) = rewards.try_next().await? {
            min_index = Some(match min_index {
                None => statement.id,
                Some(current_min) => min(current_min, statement.id),
            });

            max_index = Some(match max_index {
                None => statement.id,
                Some(current_max) => max(current_max, statement.id),
            });
            connection.edges.push(connection::Edge::new(statement.id.to_string(), statement));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (min_index, max_index) {
            let result = sqlx::query!(
                r#"
                    SELECT MAX(id) as max_id, MIN(id) as min_id
                    FROM account_statements
                    WHERE account_index = $1
                    AND entry_type IN (
                        'FinalizationReward',
                        'FoundationReward',
                        'BakerReward',
                        'TransactionFeeReward'
                    )
                "#,
                &self.index
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.min_id.map_or(false, |db_min| db_min < page_min_id);
            connection.has_next_page = result.max_id.map_or(false, |db_max| db_max > page_max_id);
        }
        Ok(connection)
    }

    async fn release_schedule(&self) -> AccountReleaseSchedule {
        AccountReleaseSchedule {
            account_index: self.index,
        }
    }
}

struct AccountReleaseSchedule {
    account_index: AccountIndex,
}
#[Object]
impl AccountReleaseSchedule {
    async fn total_amount(&self, ctx: &Context<'_>) -> ApiResult<Amount> {
        let pool = get_pool(ctx)?;
        let total_amount = sqlx::query_scalar!(
            "SELECT
               SUM(amount)::BIGINT
             FROM scheduled_releases
             WHERE account_index = $1",
            self.account_index,
        )
        .fetch_one(pool)
        .await?;
        Ok(total_amount.unwrap_or(0).try_into()?)
    }

    async fn schedule(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, AccountReleaseScheduleItem>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<AccountReleaseScheduleItemIndex>::new(
            first,
            after,
            last,
            before,
            config.account_schedule_connection_limit,
        )?;
        let rows = sqlx::query_as!(
            AccountReleaseScheduleItem,
            "SELECT * FROM (
                SELECT
                    index,
                    transaction_index,
                    release_time as timestamp,
                    amount
                FROM scheduled_releases
                WHERE account_index = $5
                      AND NOW() < release_time
                      AND index > $1 AND index < $2
                ORDER BY
                    (CASE WHEN $4 THEN index END) DESC,
                    (CASE WHEN NOT $4 THEN index END) ASC
                LIMIT $3
            ) ORDER BY index ASC",
            query.from,
            query.to,
            query.limit,
            query.is_last,
            self.account_index
        )
        .fetch_all(pool)
        .await?;

        let has_previous_page = if let Some(first_row) = rows.first() {
            sqlx::query_scalar!(
                "SELECT true
                 FROM scheduled_releases
                 WHERE
                     account_index = $1
                     AND NOW() < release_time
                     AND index < $2
                 LIMIT 1",
                self.account_index,
                first_row.index,
            )
            .fetch_optional(pool)
            .await?
            .flatten()
            .unwrap_or_default()
        } else {
            false
        };

        let has_next_page = if let Some(last_row) = rows.last() {
            sqlx::query_scalar!(
                "SELECT true
                 FROM scheduled_releases
                 WHERE
                   account_index = $1
                   AND NOW() < release_time
                   AND $2 < index
                 LIMIT 1",
                self.account_index,
                last_row.index,
            )
            .fetch_optional(pool)
            .await?
            .flatten()
            .unwrap_or_default()
        } else {
            false
        };

        let mut connection = connection::Connection::new(has_previous_page, has_next_page);
        for row in rows {
            connection.edges.push(connection::Edge::new(row.index.to_string(), row));
        }
        Ok(connection)
    }
}

#[derive(SimpleObject)]
struct Delegation {
    delegator_id:      i64,
    staked_amount:     Amount,
    restake_earnings:  bool,
    delegation_target: DelegationTarget,
}

#[derive(Union)]
enum PendingDelegationChange {
    PendingDelegationRemoval(PendingDelegationRemoval),
    PendingDelegationReduceStake(PendingDelegationReduceStake),
}

#[derive(SimpleObject)]
struct PendingDelegationRemoval {
    effective_time: DateTime,
}

#[derive(SimpleObject)]
struct PendingDelegationReduceStake {
    new_staked_amount: Amount,
    effective_time:    DateTime,
}

#[derive(Union)]
enum BlockOrTransaction {
    Transaction(Transaction),
    Block(Block),
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, Default)]
enum AccountSort {
    AgeAsc,
    #[default]
    AgeDesc,
    AmountAsc,
    AmountDesc,
    TransactionCountAsc,
    TransactionCountDesc,
    DelegatedStakeAsc,
    DelegatedStakeDesc,
}

#[derive(Debug, Clone, Copy)]
struct AccountOrder {
    field: AccountOrderField,
    dir:   OrderDir,
}

impl From<AccountSort> for AccountOrder {
    fn from(sort: AccountSort) -> Self {
        match sort {
            AccountSort::AgeAsc => Self {
                field: AccountOrderField::Age,
                dir:   OrderDir::Asc,
            },
            AccountSort::AgeDesc => Self {
                field: AccountOrderField::Age,
                dir:   OrderDir::Desc,
            },
            AccountSort::AmountAsc => Self {
                field: AccountOrderField::Amount,
                dir:   OrderDir::Asc,
            },
            AccountSort::AmountDesc => Self {
                field: AccountOrderField::Amount,
                dir:   OrderDir::Desc,
            },
            AccountSort::TransactionCountAsc => Self {
                field: AccountOrderField::TransactionCount,
                dir:   OrderDir::Asc,
            },
            AccountSort::TransactionCountDesc => Self {
                field: AccountOrderField::TransactionCount,
                dir:   OrderDir::Desc,
            },
            AccountSort::DelegatedStakeAsc => Self {
                field: AccountOrderField::DelegatedStake,
                dir:   OrderDir::Asc,
            },
            AccountSort::DelegatedStakeDesc => Self {
                field: AccountOrderField::DelegatedStake,
                dir:   OrderDir::Desc,
            },
        }
    }
}

/// The fields that may be sorted by when querying accounts.
#[derive(Debug, Clone, Copy)]
enum AccountOrderField {
    Age,
    Amount,
    TransactionCount,
    DelegatedStake,
}

#[derive(InputObject)]
struct AccountFilterInput {
    is_delegator: bool,
}
