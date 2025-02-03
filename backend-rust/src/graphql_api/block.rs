use super::{get_config, get_pool, ApiError, ApiResult, ConnectionQuery};
use crate::{
    block_special_event::{SpecialEvent, SpecialEventTypeFilter},
    graphql_api::Transaction,
    scalar_types::{Amount, BakerId, BlockHash, BlockHeight, DateTime},
    transaction_event::Event,
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use async_graphql::{connection, types, Context, Object, SimpleObject};
use futures::TryStreamExt;
use std::cmp::{max, min};

#[derive(Default)]
pub(crate) struct QueryBlocks;

#[Object]
impl QueryBlocks {
    async fn block<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Block> {
        let height: BlockHeight = id.try_into().map_err(ApiError::InvalidIdInt)?;
        Block::query_by_height(get_pool(ctx)?, height).await
    }

    async fn block_by_block_hash<'a>(
        &self,
        ctx: &Context<'a>,
        block_hash: BlockHash,
    ) -> ApiResult<Block> {
        Block::query_by_hash(get_pool(ctx)?, block_hash).await
    }

    /// Query the list of blocks ordered descendingly by block height.
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
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<BlockHeight>::new(
            first,
            after,
            last,
            before,
            config.block_connection_limit,
        )?;
        // The CCDScan front-end currently expects an DESC order of the nodes/edges
        // returned (outer `ORDER BY`). If the `last` input parameter is set,
        // the inner `ORDER BY` reverses the block order to allow the range be applied
        // starting from the last element.
        let rows = sqlx::query_as!(
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
                WHERE height > $1 AND height < $2
                ORDER BY
                    (CASE WHEN $4 THEN height END) ASC,
                    (CASE WHEN NOT $4 THEN height END) DESC
                LIMIT $3
            ) ORDER BY height DESC",
            query.from,
            query.to,
            query.limit,
            query.desc
        )
        .fetch_all(pool)
        .await?;
        let has_prev_page = if let Some(first) = rows.first() {
            sqlx::query_scalar!("SELECT true FROM blocks WHERE height > $1 LIMIT 1", first.height)
                .fetch_optional(pool)
                .await?
                .flatten()
                .unwrap_or_default()
        } else {
            false
        };
        let has_next_page = if let Some(last) = rows.last() {
            // Genesis block have height 0, so we check whether the last block is higher.
            last.height > 0
        } else {
            false
        };
        let mut connection = connection::Connection::new(has_prev_page, has_next_page);
        for row in rows {
            connection.edges.push(connection::Edge::new(row.height.to_string(), row));
        }
        Ok(connection)
    }
}

#[derive(Debug, Clone)]
pub struct Block {
    pub(crate) hash:              BlockHash,
    pub(crate) height:            BlockHeight,
    /// Time of the block being baked.
    pub(crate) slot_time:         DateTime,
    /// Number of milliseconds between the `slot_time` of this block and its
    /// parent.
    pub(crate) block_time:        i32,
    /// If this block is finalized, the number of milliseconds between the
    /// `slot_time` of this block and the first block that contains a
    /// finalization proof or quorum certificate that justifies this block
    /// being finalized.
    pub(crate) finalization_time: Option<i32>,
    pub(crate) baker_id:          Option<i64>,
    pub(crate) total_amount:      i64,
}

impl Block {
    pub async fn query_by_height(pool: &sqlx::PgPool, height: BlockHeight) -> ApiResult<Self> {
        sqlx::query_as!(
            Block,
            "SELECT
                hash,
                height,
                slot_time,
                block_time,
                finalization_time,
                baker_id,
                total_amount
            FROM blocks
            WHERE height=$1",
            height
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)
    }

    pub async fn query_by_hash(pool: &sqlx::PgPool, block_hash: BlockHash) -> ApiResult<Self> {
        sqlx::query_as!(
            Block,
            "SELECT
                hash,
                height,
                slot_time,
                block_time,
                finalization_time,
                baker_id,
                total_amount
            FROM blocks
            WHERE hash=$1",
            block_hash
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)
    }
}

#[Object]
impl Block {
    // chain_parameters: ChainParameters,
    // balance_statistics: BalanceStatistics,

    /// Absolute block height.
    async fn id(&self) -> types::ID { types::ID::from(self.height) }

    async fn block_hash(&self) -> &BlockHash { &self.hash }

    async fn block_height(&self) -> &BlockHeight { &self.height }

    async fn baker_id(&self) -> Option<BakerId> { self.baker_id.map(BakerId::from) }

    async fn total_amount(&self) -> ApiResult<Amount> { Ok(self.total_amount.try_into()?) }

    /// Time of the block being baked.
    async fn block_slot_time(&self) -> &DateTime { &self.slot_time }

    /// Whether the block is finalized.
    async fn finalized(&self) -> bool { true }

    /// The block statistics:
    ///   - The time difference from the parent block.
    ///   - The time difference to the block that justifies the block being
    ///     finalized.
    async fn block_statistics(&self) -> BlockStatistics {
        BlockStatistics {
            block_time:        self.block_time as f64 / 1000.0,
            finalization_time: self.finalization_time.map(|f| f as f64 / 1000.0),
        }
    }

    /// Number of transactions included in this block.
    async fn transaction_count<'a>(&self, ctx: &Context<'a>) -> ApiResult<i64> {
        let result =
            sqlx::query!("SELECT COUNT(*) FROM transactions WHERE block_height = $1", self.height)
                .fetch_one(get_pool(ctx)?)
                .await?;
        Ok(result.count.unwrap_or(0))
    }

    /// Query the special events (aka. special transaction outcomes) associated
    /// with this block.
    async fn special_events(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Filter special events by special event type. Set to null to return \
                          all special events (no filtering).")]
        include_filter: Option<Vec<SpecialEventTypeFilter>>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, SpecialEvent>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.special_events_per_block_connection_limit,
        )?;
        let disable_filtering = include_filter.is_none();
        let filters = include_filter.unwrap_or_default();
        let mut row_stream = sqlx::query_as!(
            BlockSpecialTransactionOutcome,
            r#"
            SELECT * FROM (
                SELECT
                    block_outcome_index,
                    outcome as "outcome: _"
                FROM block_special_transaction_outcomes
                WHERE
                    block_height = $5
                    AND ($6 OR outcome_type = ANY($7))
                AND block_outcome_index > $1 AND block_outcome_index < $2
                ORDER BY
                    CASE WHEN $3 THEN block_outcome_index END DESC,
                    CASE WHEN NOT $3 THEN block_outcome_index END ASC
                LIMIT $4
            ) ORDER BY block_outcome_index ASC"#,
            query.from,
            query.to,
            query.desc,
            query.limit,
            self.height,
            disable_filtering,
            filters.as_ref() as &[SpecialEventTypeFilter]
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        let mut page_max_index = None;
        let mut page_min_index = None;
        while let Some(row) = row_stream.try_next().await? {
            page_max_index = Some(match page_max_index {
                None => row.block_outcome_index,
                Some(current_max) => max(current_max, row.block_outcome_index),
            });
            page_min_index = Some(match page_min_index {
                None => row.block_outcome_index,
                Some(current_min) => min(current_min, row.block_outcome_index),
            });
            let cursor = row.block_outcome_index.to_string();
            connection.edges.push(connection::Edge::new(cursor, row.outcome.0));
        }
        if let (Some(page_min_id), Some(page_max_id)) = (page_min_index, page_max_index) {
            let row = sqlx::query!(
                "SELECT
                     MAX(block_outcome_index), MIN(block_outcome_index)
                 FROM block_special_transaction_outcomes
                 WHERE block_height = $1 AND ($2 OR outcome_type = ANY($3))",
                self.height,
                disable_filtering,
                filters.as_ref() as &[SpecialEventTypeFilter]
            )
            .fetch_one(pool)
            .await?;
            connection.has_previous_page = row.min.unwrap_or(0) < page_min_id;
            connection.has_next_page = row.max.unwrap_or(0) > page_max_id;
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
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.transactions_per_block_connection_limit,
        )?;

        // Retrieves the transactions within the specified block. The transactions are
        // ordered in ascending order (outer `ORDER BY`). If the `last` input
        // parameter is set, the inner `ORDER BY` reverses the transaction order
        // to allow the range be applied starting from the last element.
        let mut row_stream = sqlx::query_as!(
            Transaction,
            r#"
            SELECT * FROM (
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
                WHERE transactions.block_height = $5
                AND index > $1 AND index < $2
                ORDER BY
                    CASE WHEN $3 THEN index END DESC,
                    CASE WHEN NOT $3 THEN index END ASC
                LIMIT $4
            ) ORDER BY index ASC"#,
            query.from,
            query.to,
            query.desc,
            query.limit,
            self.height
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
                    WHERE transactions.block_height = $1
                ",
                &self.height
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

#[derive(SimpleObject)]
struct BlockStatistics {
    /// Number of seconds between block slot time of this block and previous
    /// block.
    block_time:        f64,
    /// Number of seconds between the block slot time of this block and the
    /// block containing the finalization proof for this block.
    ///
    /// This is an objective measure of the finalization time (determined by
    /// chain data alone) and will at least be the block time. The actual
    /// finalization time will usually be lower than that but can only be
    /// determined in a subjective manner by each node: That is the time a
    /// node has first seen a block finalized. This is defined as the
    /// difference between when a finalization proof is first constructed,
    /// and the block slot time. However the time when a finalization proof
    /// is first constructed is subjective, some nodes will receive the
    /// necessary messages before others. Also, this number cannot be
    /// reconstructed for blocks finalized before extracting data from the
    /// node.
    ///
    /// Value will initially be `None` until the block containing the
    /// finalization proof for this block is itself finalized.
    finalization_time: Option<f64>,
}

struct BlockSpecialTransactionOutcome {
    block_outcome_index: i64,
    outcome:             sqlx::types::Json<SpecialEvent>,
}

#[derive(SimpleObject)]
struct BalanceStatistics {
    /// The total CCD in existence
    total_amount: Amount,
    /// The total CCD Released. This is total CCD supply not counting the
    /// balances of non circulating accounts.
    total_amount_released: Amount,
    /// The total CCD Unlocked according to the Concordium promise published on
    /// deck.concordium.com. Will be null for blocks with slot time before the
    /// published release schedule.
    total_amount_unlocked: Amount,
    /// The total CCD in encrypted balances.
    total_amount_encrypted: Amount,
    /// The total CCD locked in release schedules (from transfers with
    /// schedule).
    total_amount_locked_in_release_schedules: Amount,
    /// The total CCD staked.
    total_amount_staked: Amount,
    /// The amount in the baking reward account.
    baking_reward_account: Amount,
    /// The amount in the finalization reward account.
    finalization_reward_account: Amount,
    /// The amount in the GAS account.
    gas_account: Amount,
}

#[derive(SimpleObject)]
struct ExchangeRate {
    numerator:   u64,
    denominator: u64,
}
