use super::{
    account::Account,
    baker_and_delegator_types::{CommissionRates, DelegationSummary, PaydayPoolReward},
    get_config, get_pool,
    transaction::Transaction,
    ApiError, ApiResult, ApiServiceConfig, ApyPeriod, ConnectionQuery,
};
use crate::{
    connection::{
        ConcatCursor, ConnectionBounds, DescendingI64, F64Cursor, NestedCursor, OptionCursor,
        Reversed,
    },
    graphql_api::node_status::{NodeInfoReceiver, NodeStatus},
    scalar_types::{Amount, BakerId, DateTime, Decimal, MetadataUrl},
    transaction_event::{baker::BakerPoolOpenStatus, Event},
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use async_graphql::{
    connection::{self, CursorType},
    types, Context, Enum, InputObject, Object, SimpleObject, Union,
};
use bigdecimal::BigDecimal;
use concordium_rust_sdk::types::AmountFraction;
use futures::TryStreamExt;
use sqlx::PgPool;
use std::cmp::{max, min, Ordering};

#[derive(Default)]
pub struct QueryBaker;

#[Object]
impl QueryBaker {
    async fn baker<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Baker> {
        let id = IdBaker::try_from(id)?.baker_id.into();
        Baker::query_by_id(get_pool(ctx)?, id).await?.ok_or(ApiError::NotFound)
    }

    async fn baker_by_baker_id<'a>(
        &self,
        ctx: &Context<'a>,
        baker_id: BakerId,
    ) -> ApiResult<Baker> {
        Baker::query_by_id(get_pool(ctx)?, baker_id.into()).await?.ok_or(ApiError::NotFound)
    }

    #[allow(clippy::too_many_arguments)]
    async fn bakers(
        &self,
        ctx: &Context<'_>,
        #[graphql(default)] sort: BakerSort,
        filter: Option<BakerFilterInput>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let open_status_filter = filter.and_then(|input| input.open_status_filter);
        let include_removed_filter =
            filter.and_then(|input| input.include_removed).unwrap_or(false);
        match sort {
            BakerSort::BakerIdAsc => {
                Baker::id_asc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::BakerIdDesc => {
                Baker::id_desc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::TotalStakedAmountDesc => {
                Baker::total_staked_desc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::DelegatorCountDesc => {
                Baker::delegator_count_desc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::BakerApy30DaysDesc => {
                Baker::baker_apy_desc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::DelegatorApy30DaysDesc => {
                Baker::delegator_apy_desc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::BlockCommissionsAsc => {
                Baker::block_commission_asc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
            BakerSort::BlockCommissionsDesc => {
                Baker::block_commission_desc_connection(
                    config,
                    pool,
                    first,
                    after,
                    last,
                    before,
                    open_status_filter,
                    include_removed_filter,
                )
                .await
            }
        }
    }
}

/// Cursor for `Query::bakers` when sorting by the baker/validator id.
type BakerIdCursor = i64;

/// Cursor for `Query::bakers` when sorting by the baker/validator by some
/// field, like the delegator count or the total staked amount.
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
struct BakerFieldDescCursor {
    /// The cursor value of the relevant sorting field.
    field:    i64,
    /// The baker id representing the pool of the cursor.
    baker_id: i64,
}
impl BakerFieldDescCursor {
    fn total_staked_cursor(row: &CurrentBaker) -> Self {
        BakerFieldDescCursor {
            field:    row.pool_total_staked,
            baker_id: row.id,
        }
    }

    fn delegator_count_cursor(row: &CurrentBaker) -> Self {
        BakerFieldDescCursor {
            field:    row.pool_delegator_count,
            baker_id: row.id,
        }
    }

    fn payday_baking_commission_rate(row: &CurrentBaker) -> Self {
        BakerFieldDescCursor {
            field:    row.payday_baking_commission_rate(),
            baker_id: row.id,
        }
    }
}

impl connection::CursorType for BakerFieldDescCursor {
    type Error = DecodeBakerFieldCursor;

    fn decode_cursor(s: &str) -> Result<Self, Self::Error> {
        let (before, after) = s.split_once(':').ok_or(DecodeBakerFieldCursor::NoSemicolon)?;
        let field: i64 = before.parse().map_err(DecodeBakerFieldCursor::ParseField)?;
        let baker_id: i64 = after.parse().map_err(DecodeBakerFieldCursor::ParseBakerId)?;
        Ok(Self {
            field,
            baker_id,
        })
    }

    fn encode_cursor(&self) -> String { format!("{}:{}", self.field, self.baker_id) }
}

#[derive(Debug, thiserror::Error)]
enum DecodeBakerFieldCursor {
    #[error("Cursor must contain a semicolon.")]
    NoSemicolon,
    #[error("Cursor must contain valid validator ID.")]
    ParseBakerId(std::num::ParseIntError),
    #[error("Cursor must contain valid field.")]
    ParseField(std::num::ParseIntError),
}

impl ConnectionBounds for BakerFieldDescCursor {
    const END_BOUND: Self = Self {
        baker_id: i64::MIN,
        field:    i64::MIN,
    };
    const START_BOUND: Self = Self {
        baker_id: i64::MAX,
        field:    i64::MAX,
    };
}

impl PartialOrd for BakerFieldDescCursor {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> { Some(self.cmp(other)) }
}

impl Ord for BakerFieldDescCursor {
    fn cmp(&self, other: &Self) -> std::cmp::Ordering {
        let ordering = other.field.cmp(&self.field);
        if let Ordering::Equal = ordering {
            other.baker_id.cmp(&self.baker_id)
        } else {
            ordering
        }
    }
}

#[repr(transparent)]
struct IdBaker {
    baker_id: BakerId,
}
impl std::str::FromStr for IdBaker {
    type Err = ApiError;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        let baker_id = value.parse()?;
        Ok(IdBaker {
            baker_id,
        })
    }
}
impl TryFrom<types::ID> for IdBaker {
    type Error = ApiError;

    fn try_from(value: types::ID) -> Result<Self, Self::Error> { value.0.parse() }
}

#[derive(Debug)]
pub enum Baker {
    Current(Box<CurrentBaker>),
    Previously(PreviouslyBaker),
}

impl Baker {
    pub fn get_id(&self) -> i64 {
        match self {
            Baker::Current(existing_baker) => existing_baker.id,
            Baker::Previously(removed) => removed.id,
        }
    }

    /// Compare bakers. Current bakers are compared using provided `compare`
    /// function and then ordered by ID. Removed bakers are considered last
    /// and ordered by ID
    fn cmp_baker_field<F>(&self, other: &Baker, compare: F) -> Ordering
    where
        F: Fn(&CurrentBaker, &CurrentBaker) -> Ordering, {
        match (self, other) {
            (Baker::Current(left), Baker::Current(right)) => {
                let ord = compare(left, right);
                if ord.is_eq() {
                    left.id.cmp(&right.id).reverse()
                } else {
                    ord
                }
            }
            (Baker::Current(_), Baker::Previously(_)) => Ordering::Less,
            (Baker::Previously(_), Baker::Current(_)) => Ordering::Greater,
            (Baker::Previously(left), Baker::Previously(right)) => left.id.cmp(&right.id).reverse(),
        }
    }

    pub async fn query_by_id(pool: &PgPool, baker_id: i64) -> ApiResult<Option<Self>> {
        let baker = if let Some(baker) = CurrentBaker::query_by_id(pool, baker_id).await? {
            Some(Baker::Current(Box::new(baker)))
        } else {
            PreviouslyBaker::query_by_id(pool, baker_id).await?.map(Baker::Previously)
        };
        Ok(baker)
    }

    #[allow(clippy::too_many_arguments)]
    async fn id_asc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        let query = ConnectionQuery::<BakerIdCursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);
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
                    (bakers.id > $1 AND bakers.id < $2)
                    -- filter if provided
                    AND ($5::pool_open_status IS NULL OR open_status = $5::pool_open_status)
                ORDER BY
                    (CASE WHEN $3     THEN bakers.id END) DESC,
                    (CASE WHEN NOT $3 THEN bakers.id END) ASC
                LIMIT $4
            ) ORDER BY id ASC"#,
            query.from,                                        // $1
            query.to,                                          // $2
            query.is_last,                                     // $3
            query.limit,                                       // $4
            open_status_filter as Option<BakerPoolOpenStatus>  // $5
        )
        .fetch(pool);
        while let Some(row) = row_stream.try_next().await? {
            let cursor = row.id.encode_cursor();
            connection.edges.push(connection::Edge::new(cursor, Baker::Current(Box::new(row))));
        }

        if include_removed_filter {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $1 AND id < $2
                    ORDER BY
                        (CASE WHEN $3     THEN id END) DESC,
                        (CASE WHEN NOT $3 THEN id END) ASC
                    LIMIT $4
                ) ORDER BY id ASC",
                query.from,
                query.to,
                query.is_last,
                query.limit,
            )
            .fetch(pool);

            while let Some(row) = row_stream.try_next().await? {
                let cursor = row.id.encode_cursor();
                connection.edges.push(connection::Edge::new(cursor, Baker::Previously(row)));
            }
            // Sort again after adding the removed bakers and truncate to the desired limit.
            connection.edges.sort_by_key(|edge| edge.node.get_id());
            // Remove from either ends of the current edges, since we might be above the
            // limit.
            if query.is_last {
                let offset = connection.edges.len().saturating_sub(usize::try_from(query.limit)?);
                connection.edges.drain(..offset);
            } else {
                connection.edges.truncate(query.limit.try_into()?);
            }
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
                    $1::pool_open_status IS NULL
                    OR open_status = $1::pool_open_status",
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_one(pool)
            .await?;
            if let (Some(min), Some(max)) = (bounds.min, bounds.max) {
                connection.has_previous_page = min < first_item.node.get_id();
                connection.has_next_page = max > last_item.node.get_id();
            }
        }
        if include_removed_filter {
            let bounds = sqlx::query!(
                "SELECT
                    MAX(id),
                    MIN(id)
                FROM bakers_removed",
            )
            .fetch_one(pool)
            .await?;
            if let (Some(min), Some(max)) = (bounds.min, bounds.max) {
                connection.has_previous_page =
                    connection.has_previous_page || min < first_item.node.get_id();
                connection.has_next_page =
                    connection.has_next_page || max > last_item.node.get_id();
            }
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn id_desc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        let query = ConnectionQuery::<Reversed<BakerIdCursor>>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);
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
                    (bakers.id > $2 AND bakers.id < $1) AND
                    -- filter if provided
                    ($5::pool_open_status IS NULL OR open_status = $5::pool_open_status)
                ORDER BY
                    (CASE WHEN $3     THEN bakers.id END) ASC,
                    (CASE WHEN NOT $3 THEN bakers.id END) DESC
                LIMIT $4
            ) ORDER BY id DESC"#,
            &query.from.cursor,                                // $1
            &query.to.cursor,                                  // $2
            query.is_last,                                     // $3
            query.limit,                                       // $4
            open_status_filter as Option<BakerPoolOpenStatus>  // $5
        )
        .fetch(pool);
        while let Some(row) = row_stream.try_next().await? {
            let cursor = row.id.encode_cursor();
            connection.edges.push(connection::Edge::new(cursor, Baker::Current(Box::new(row))));
        }
        if include_removed_filter {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $2 AND id < $1
                    ORDER BY
                        (CASE WHEN $3     THEN id END) ASC,
                        (CASE WHEN NOT $3 THEN id END) DESC
                    LIMIT $4
                ) ORDER BY id DESC",
                query.from.cursor,
                query.to.cursor,
                query.is_last,
                query.limit,
            )
            .fetch(pool);

            while let Some(row) = row_stream.try_next().await? {
                let cursor = row.id.encode_cursor();
                connection.edges.push(connection::Edge::new(cursor, Baker::Previously(row)));
            }
            // Sort again after adding the removed bakers.
            connection.edges.sort_by_key(|edge| i64::MAX - edge.node.get_id());

            // Remove from either ends of the current edges, since we might be above the
            // limit.
            if query.is_last {
                let offset = connection.edges.len().saturating_sub(usize::try_from(query.limit)?);
                connection.edges.drain(..offset);
            } else {
                connection.edges.truncate(query.limit.try_into()?);
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return.
            return Ok(connection);
        };
        let first_item_id = first_item.node.get_id();
        let last_item_id = last_item.node.get_id();
        {
            let bounds = sqlx::query!(
                "SELECT
                    MAX(id),
                    MIN(id)
                FROM bakers
                WHERE
                    $1::pool_open_status IS NULL
                    OR open_status = $1::pool_open_status",
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_one(pool)
            .await?;
            if let (Some(min), Some(max)) = (bounds.min, bounds.max) {
                connection.has_previous_page = max > first_item_id;
                connection.has_next_page = min < last_item_id;
            }
        }
        if include_removed_filter {
            let bounds = sqlx::query!("SELECT MAX(id), MIN(id) FROM bakers_removed",)
                .fetch_one(pool)
                .await?;
            if let (Some(min), Some(max)) = (bounds.min, bounds.max) {
                connection.has_previous_page = connection.has_previous_page || max > first_item_id;
                connection.has_next_page = connection.has_next_page || min < last_item_id;
            }
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn total_staked_desc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        type RemovedBakerCursor = Reversed<BakerIdCursor>;
        type Cursor = ConcatCursor<BakerFieldDescCursor, RemovedBakerCursor>;

        /// Internal helper function for reusing the query for the current
        /// bakers sorted by a field.
        async fn query_current_bakers(
            query: ConnectionQuery<BakerFieldDescCursor>,
            open_status_filter: Option<BakerPoolOpenStatus>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
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
                    (
                       -- Start outer bound for page
                       (pool_total_staked < $1
                       -- End outer bound for page
                       AND pool_total_staked > $2)
                       -- When outer bounds are not equal, filter separate for each inner bound.
                       OR (
                           $1 != $2
                           AND (
                                -- Start inner bound for page.
                                (pool_total_staked = $1 AND bakers.id < $3)
                                -- End inner bound for page.
                                 OR (pool_total_staked = $2 AND bakers.id > $4)
                           )
                       )
                       -- When outer bounds are equal, use one filter for both bounds.
                       OR (
                           $1 = $2
                           AND (pool_total_staked = $1
                           AND bakers.id < $3 AND bakers.id > $4))

                    )
                    -- filter if provided
                    AND ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
                ORDER BY
                    (CASE WHEN $5     THEN pool_total_staked END) ASC,
                    (CASE WHEN $5     THEN bakers.id            END) ASC,
                    (CASE WHEN NOT $5 THEN pool_total_staked END) DESC,
                    (CASE WHEN NOT $5 THEN bakers.id            END) DESC
                LIMIT $6
            ) ORDER BY pool_total_staked DESC, id DESC"#,
                query.from.field,                                  // $1
                query.to.field,                                    // $2
                query.from.baker_id,                               // $3
                query.to.baker_id,                                 // $4
                query.is_last,                                     // $5
                query.limit,                                       // $6
                open_status_filter as Option<BakerPoolOpenStatus>  // $7
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor = Cursor::First(BakerFieldDescCursor::total_staked_cursor(&row));
                connection.edges.push(connection::Edge::new(
                    cursor.encode_cursor(),
                    Baker::Current(Box::new(row)),
                ));
            }
            Ok(())
        }

        /// Internal helper function for reusing the query for the removed
        /// bakers sorted by id in descending order.
        async fn query_removed_baker(
            query: ConnectionQuery<Reversed<BakerIdCursor>>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $2 AND id < $1
                    ORDER BY
                        (CASE WHEN $3     THEN id END) ASC,
                        (CASE WHEN NOT $3 THEN id END) DESC
                    LIMIT $4
                ) ORDER BY id DESC",
                query.from.cursor,
                query.to.cursor,
                query.is_last,
                query.limit,
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = Cursor::Second(Reversed::new(row.id));
                connection
                    .edges
                    .push(connection::Edge::new(cursor.encode_cursor(), Baker::Previously(row)));
            }
            Ok(())
        }

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // In this connection there are potentially two collections/tables involved,
        // firstly the current bakers sorted by some field, secondly removed bakers when
        // enabled. The strategy is then to query one collection and if the result is
        // below the limit, we query the second collection. The order of which
        // collection to query first and second will depend on the whether the
        // `last` parameter was provided in the top level query.
        if query.is_last {
            if include_removed_filter {
                if let Some(removed_baker_query) = query.subquery_second() {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if remains_to_limit > 0 {
                if let Some(current_baker_query) = query.subquery_first_with_limit(remains_to_limit)
                {
                    query_current_bakers(
                        current_baker_query,
                        open_status_filter,
                        &mut connection,
                        pool,
                    )
                    .await?;
                }
            }
            if include_removed_filter {
                // Since we might already have some removed bakers in the page, we sort to make
                // sure these are last after adding current bakers.
                connection.edges.sort_by(|left, right| {
                    left.node.cmp_baker_field(&right.node, |left, right| {
                        left.pool_total_staked.cmp(&right.pool_total_staked).reverse()
                    })
                });
            }
        } else {
            if let Some(current_baker_query) = query.subquery_first() {
                query_current_bakers(
                    current_baker_query,
                    open_status_filter,
                    &mut connection,
                    pool,
                )
                .await?;
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if include_removed_filter && remains_to_limit > 0 {
                if let Some(removed_baker_query) =
                    query.subquery_second_with_limit(remains_to_limit)
                {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };
        {
            let collection_ends = sqlx::query!(
                "WITH
                    starting_baker as (
                        SELECT id, pool_total_staked FROM bakers
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY pool_total_staked DESC, id DESC
                        LIMIT 1
                    ),
                    ending_baker as (
                        SELECT id, pool_total_staked FROM bakers
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY pool_total_staked ASC, id ASC
                        LIMIT 1
                    )
                SELECT
                    starting_baker.id AS start_id,
                    starting_baker.pool_total_staked AS start_total_staked,
                    ending_baker.id AS end_id,
                    ending_baker.pool_total_staked AS end_total_staked
                FROM starting_baker, ending_baker",
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_optional(pool)
            .await?;
            if let Some(collection_ends) = collection_ends {
                connection.has_previous_page = if let Baker::Current(first_baker) = &first_item.node
                {
                    let collection_start_cursor = BakerFieldDescCursor {
                        baker_id: collection_ends.start_id,
                        field:    collection_ends.start_total_staked,
                    };
                    collection_start_cursor < BakerFieldDescCursor::total_staked_cursor(first_baker)
                } else {
                    true
                };
                if let Baker::Current(last_item) = &last_item.node {
                    let collection_end_cursor = BakerFieldDescCursor {
                        baker_id: collection_ends.end_id,
                        field:    collection_ends.end_total_staked,
                    };
                    connection.has_next_page = collection_end_cursor
                        > BakerFieldDescCursor::total_staked_cursor(last_item);
                }
            }
        }
        if include_removed_filter {
            let min_removed_baker_id =
                sqlx::query_scalar!("SELECT MIN(id) FROM bakers_removed",).fetch_one(pool).await?;
            connection.has_next_page = if let Some(min_removed_baker_id) = min_removed_baker_id {
                last_item.node.get_id() != min_removed_baker_id
            } else {
                false
            }
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn delegator_count_desc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        type RemovedBakerCursor = Reversed<BakerIdCursor>;
        type Cursor = ConcatCursor<BakerFieldDescCursor, RemovedBakerCursor>;

        /// Internal helper function for querying the current bakers sorted
        /// by the pool_delegator_count in descending order.
        async fn query_current_bakers(
            query: ConnectionQuery<BakerFieldDescCursor>,
            open_status_filter: Option<BakerPoolOpenStatus>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
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
                   (
                       -- Start outer bound for page
                       (pool_delegator_count < $1
                       -- End outer bound for page
                       AND pool_delegator_count > $2)
                       -- When outer bounds are not equal, filter separate for each inner bound.
                       OR (
                           $1 != $2
                           AND (
                                -- Start inner bound for page.
                                (pool_delegator_count = $1 AND bakers.id < $3)
                                -- End inner bound for page.
                                 OR (pool_delegator_count = $2 AND bakers.id > $4)
                           )
                       )
                       -- When outer bounds are equal, use one filter for both bounds.
                       OR (
                           $1 = $2
                           AND (pool_delegator_count = $1
                           AND bakers.id < $3 AND bakers.id > $4))
                    )
                    -- filter if provided
                    AND ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
                ORDER BY
                    (CASE WHEN $5     THEN pool_delegator_count END) ASC,
                    (CASE WHEN $5     THEN bakers.id            END) ASC,
                    (CASE WHEN NOT $5 THEN pool_delegator_count END) DESC,
                    (CASE WHEN NOT $5 THEN bakers.id            END) DESC
                LIMIT $6
            ) ORDER BY pool_delegator_count DESC, id DESC"#,
                query.from.field,                                  // $1
                query.to.field,                                    // $2
                query.from.baker_id,                               // $3
                query.to.baker_id,                                 // $4
                query.is_last,                                     // $5
                query.limit,                                       // $6
                open_status_filter as Option<BakerPoolOpenStatus>  // $7
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor = Cursor::First(BakerFieldDescCursor::delegator_count_cursor(&row));
                connection.edges.push(connection::Edge::new(
                    cursor.encode_cursor(),
                    Baker::Current(Box::new(row)),
                ));
            }
            Ok(())
        }

        /// Internal helper function for querying the removed bakers sorted
        /// by the baker ID in descending order.
        async fn query_removed_baker(
            query: ConnectionQuery<Reversed<BakerIdCursor>>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $2 AND id < $1
                    ORDER BY
                        (CASE WHEN $3     THEN id END) ASC,
                        (CASE WHEN NOT $3 THEN id END) DESC
                    LIMIT $4
                ) ORDER BY id DESC",
                query.from.cursor,
                query.to.cursor,
                query.is_last,
                query.limit,
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = Cursor::Second(Reversed::new(row.id));
                connection
                    .edges
                    .push(connection::Edge::new(cursor.encode_cursor(), Baker::Previously(row)));
            }
            Ok(())
        }

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // In this connection there are potentially two collections/tables involved,
        // firstly the current bakers sorted by some field, secondly removed bakers when
        // enabled. The strategy is then to query one collection and if the result is
        // below the limit, we query the second collection. The order of which
        // collection to query first and second will depend on the whether the
        // `last` parameter was provided in the top level query.
        if query.is_last {
            if include_removed_filter {
                if let Some(removed_baker_query) = query.subquery_second() {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if remains_to_limit > 0 {
                if let Some(current_baker_query) = query.subquery_first_with_limit(remains_to_limit)
                {
                    query_current_bakers(
                        current_baker_query,
                        open_status_filter,
                        &mut connection,
                        pool,
                    )
                    .await?;
                }
            }
            if include_removed_filter {
                // Since we might already have some removed bakers in the page, we sort to make
                // sure these are last after adding current bakers.
                connection.edges.sort_by(|left, right| {
                    left.node.cmp_baker_field(&right.node, |left, right| {
                        left.pool_delegator_count.cmp(&right.pool_delegator_count).reverse()
                    })
                });
            }
        } else {
            if let Some(current_baker_query) = query.subquery_first() {
                query_current_bakers(
                    current_baker_query,
                    open_status_filter,
                    &mut connection,
                    pool,
                )
                .await?;
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if include_removed_filter && remains_to_limit > 0 {
                if let Some(removed_baker_query) =
                    query.subquery_second_with_limit(remains_to_limit)
                {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };
        {
            let collection_ends = sqlx::query!(
                "WITH
                    starting_baker as (
                        SELECT id, pool_delegator_count FROM bakers
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY pool_delegator_count DESC, id DESC
                        LIMIT 1
                    ),
                    ending_baker as (
                        SELECT id, pool_delegator_count FROM bakers
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY pool_delegator_count ASC, id ASC
                        LIMIT 1
                    )
                SELECT
                    starting_baker.id AS start_id,
                    starting_baker.pool_delegator_count AS start_delegator_count,
                    ending_baker.id AS end_id,
                    ending_baker.pool_delegator_count AS end_delegator_count
                FROM starting_baker, ending_baker",
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_optional(pool)
            .await?;
            if let Some(collection_ends) = collection_ends {
                connection.has_previous_page = if let Baker::Current(first_baker) = &first_item.node
                {
                    let collection_start_cursor = BakerFieldDescCursor {
                        baker_id: collection_ends.start_id,
                        field:    collection_ends.start_delegator_count,
                    };
                    collection_start_cursor
                        < BakerFieldDescCursor::delegator_count_cursor(first_baker)
                } else {
                    true
                };
                if let Baker::Current(last_item) = &last_item.node {
                    let collection_end_cursor = BakerFieldDescCursor {
                        baker_id: collection_ends.end_id,
                        field:    collection_ends.end_delegator_count,
                    };
                    connection.has_next_page = collection_end_cursor
                        > BakerFieldDescCursor::delegator_count_cursor(last_item);
                }
            }
        }
        if include_removed_filter {
            let min_removed_baker_id =
                sqlx::query_scalar!("SELECT MIN(id) FROM bakers_removed",).fetch_one(pool).await?;
            connection.has_next_page = if let Some(min_removed_baker_id) = min_removed_baker_id {
                last_item.node.get_id() != min_removed_baker_id
            } else {
                false
            }
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn block_commission_desc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        type RemovedBakerCursor = Reversed<BakerIdCursor>;
        type Cursor = ConcatCursor<BakerFieldDescCursor, RemovedBakerCursor>;

        /// Internal helper function for querying the current bakers sorted
        /// by the block_commission in descending order.
        async fn query_current_bakers(
            query: ConnectionQuery<BakerFieldDescCursor>,
            open_status_filter: Option<BakerPoolOpenStatus>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
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
                WHERE (
                       -- Start outer bound for page
                       (COALESCE(payday_baking_commission, 0) < $1
                       -- End outer bound for page
                       AND COALESCE(payday_baking_commission, 0) > $2)
                       -- When outer bounds are not equal, filter separate for each inner bound.
                       OR (
                           $1 != $2
                           AND (
                                -- Start inner bound for page.
                                (COALESCE(payday_baking_commission, 0) = $1 AND bakers.id < $3)
                                -- End inner bound for page.
                                 OR (COALESCE(payday_baking_commission, 0) = $2 AND bakers.id > $4)
                           )
                       )
                       -- When outer bounds are equal, use one filter for both bounds.
                       OR (
                           $1 = $2
                           AND (COALESCE(payday_baking_commission, 0) = $1
                           AND bakers.id < $3 AND bakers.id > $4))
                    )
                    -- filter if provided
                    AND ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
                ORDER BY
                    (CASE WHEN $5     THEN payday_baking_commission END) ASC NULLS FIRST,
                    (CASE WHEN $5     THEN bakers.id                END) ASC,
                    (CASE WHEN NOT $5 THEN payday_baking_commission END) DESC NULLS LAST,
                    (CASE WHEN NOT $5 THEN bakers.id                END) DESC
                LIMIT $6
            ) ORDER BY "payday_baking_commission?" DESC NULLS LAST, id DESC"#,
                query.from.field,                                  // $1
                query.to.field,                                    // $2
                query.from.baker_id,                               // $3
                query.to.baker_id,                                 // $4
                query.is_last,                                     // $5
                query.limit,                                       // $6
                open_status_filter as Option<BakerPoolOpenStatus>  // $7
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor =
                    Cursor::First(BakerFieldDescCursor::payday_baking_commission_rate(&row));
                connection.edges.push(connection::Edge::new(
                    cursor.encode_cursor(),
                    Baker::Current(Box::new(row)),
                ));
            }
            Ok(())
        }

        /// Internal helper function for querying the removed bakers sorted
        /// by the baker ID in descending order.
        async fn query_removed_baker(
            query: ConnectionQuery<Reversed<BakerIdCursor>>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $2 AND id < $1
                    ORDER BY
                        (CASE WHEN $3     THEN id END) ASC,
                        (CASE WHEN NOT $3 THEN id END) DESC
                    LIMIT $4
                ) ORDER BY id DESC",
                query.from.cursor,
                query.to.cursor,
                query.is_last,
                query.limit,
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = Cursor::Second(Reversed::new(row.id));
                connection
                    .edges
                    .push(connection::Edge::new(cursor.encode_cursor(), Baker::Previously(row)));
            }
            Ok(())
        }

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // In this connection there are potentially two collections/tables involved,
        // firstly the current bakers sorted by some field, secondly removed bakers when
        // enabled. The strategy is then to query one collection and if the result is
        // below the limit, we query the second collection. The order of which
        // collection to query first and second will depend on the whether the
        // `last` parameter was provided in the top level query.
        if query.is_last {
            if include_removed_filter {
                if let Some(removed_baker_query) = query.subquery_second() {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if remains_to_limit > 0 {
                if let Some(current_baker_query) = query.subquery_first_with_limit(remains_to_limit)
                {
                    query_current_bakers(
                        current_baker_query,
                        open_status_filter,
                        &mut connection,
                        pool,
                    )
                    .await?;
                }
            }
            if include_removed_filter {
                // Since we might already have some removed bakers in the page, we sort to make
                // sure these are last after adding current bakers.
                connection.edges.sort_by(|left, right| {
                    left.node.cmp_baker_field(&right.node, |left, right| {
                        left.payday_baking_commission_rate()
                            .cmp(&right.payday_baking_commission_rate())
                            .reverse()
                    })
                });
            }
        } else {
            if let Some(current_baker_query) = query.subquery_first() {
                query_current_bakers(
                    current_baker_query,
                    open_status_filter,
                    &mut connection,
                    pool,
                )
                .await?;
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if include_removed_filter && remains_to_limit > 0 {
                if let Some(removed_baker_query) =
                    query.subquery_second_with_limit(remains_to_limit)
                {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };
        {
            let collection_ends = sqlx::query!(
                r#"WITH
                    starting_baker as (
                        SELECT
                            bakers.id,
                            payday_baking_commission
                        FROM bakers
                        LEFT JOIN bakers_payday_commission_rates
                            ON bakers_payday_commission_rates.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY payday_baking_commission DESC NULLS LAST, id DESC
                        LIMIT 1
                    ),
                    ending_baker as (
                        SELECT
                            bakers.id,
                            payday_baking_commission
                        FROM bakers
                        LEFT JOIN bakers_payday_commission_rates
                            ON bakers_payday_commission_rates.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY payday_baking_commission ASC NULLS FIRST, id ASC
                        LIMIT 1
                    )
                SELECT
                    starting_baker.id AS start_id,
                    starting_baker.payday_baking_commission AS "start_commission?",
                    ending_baker.id AS end_id,
                    ending_baker.payday_baking_commission AS "end_commission?"
                FROM starting_baker, ending_baker"#,
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_optional(pool)
            .await?;
            if let Some(collection_ends) = collection_ends {
                connection.has_previous_page = if let Baker::Current(first_baker) = &first_item.node
                {
                    let collection_start_cursor = BakerFieldDescCursor {
                        baker_id: collection_ends.start_id,
                        field:    collection_ends.start_commission.unwrap_or(0),
                    };
                    collection_start_cursor
                        < BakerFieldDescCursor::payday_baking_commission_rate(first_baker)
                } else {
                    true
                };
                if let Baker::Current(last_item) = &last_item.node {
                    let collection_end_cursor = BakerFieldDescCursor {
                        baker_id: collection_ends.end_id,
                        field:    collection_ends.end_commission.unwrap_or(0),
                    };
                    connection.has_next_page = collection_end_cursor
                        > BakerFieldDescCursor::payday_baking_commission_rate(last_item);
                }
            }
        }
        if include_removed_filter {
            let min_removed_baker_id =
                sqlx::query_scalar!("SELECT MIN(id) FROM bakers_removed").fetch_one(pool).await?;
            connection.has_next_page = if let Some(min_removed_baker_id) = min_removed_baker_id {
                last_item.node.get_id() != min_removed_baker_id
            } else {
                false
            }
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn block_commission_asc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        type RemovedBakerCursor = BakerIdCursor;
        type BakerCursor = Reversed<BakerFieldDescCursor>;
        type Cursor = ConcatCursor<RemovedBakerCursor, BakerCursor>;

        /// Internal helper function for querying the current bakers sorted
        /// by the block_commission in ascending order.
        async fn query_current_bakers(
            query: ConnectionQuery<Reversed<BakerFieldDescCursor>>,
            open_status_filter: Option<BakerPoolOpenStatus>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
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
                    (
                       -- Start outer bound for page
                       (COALESCE(payday_baking_commission, 0) > $1
                       -- End outer bound for page
                       AND COALESCE(payday_baking_commission, 0) < $2)
                       -- When outer bounds are not equal, filter separate for each inner bound.
                       OR (
                           $1 != $2
                           AND (
                                -- Start inner bound for page.
                                (COALESCE(payday_baking_commission, 0) = $1 AND bakers.id > $3)
                                -- End inner bound for page.
                                 OR (COALESCE(payday_baking_commission, 0) = $2 AND bakers.id < $4)
                           )
                       )
                       -- When outer bounds are equal, use one filter for both bounds.
                       OR (
                           $1 = $2
                           AND (COALESCE(payday_baking_commission, 0) = $1
                           AND bakers.id > $3 AND bakers.id < $4))
                    )
                    -- filter if provided
                    AND ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
                ORDER BY
                    (CASE WHEN $5     THEN payday_baking_commission END) DESC NULLS LAST,
                    (CASE WHEN $5     THEN bakers.id                END) DESC,
                    (CASE WHEN NOT $5 THEN payday_baking_commission END) ASC NULLS FIRST,
                    (CASE WHEN NOT $5 THEN bakers.id                END) ASC
                LIMIT $6
            ) ORDER BY "payday_baking_commission?" ASC NULLS FIRST, id ASC"#,
                query.from.cursor.field,                           // $1
                query.to.cursor.field,                             // $2
                query.from.cursor.baker_id,                        // $3
                query.to.cursor.baker_id,                          // $4
                query.is_last,                                     // $5
                query.limit,                                       // $6
                open_status_filter as Option<BakerPoolOpenStatus>  // $7
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor = Cursor::Second(Reversed::new(
                    BakerFieldDescCursor::payday_baking_commission_rate(&row),
                ));
                connection.edges.push(connection::Edge::new(
                    cursor.encode_cursor(),
                    Baker::Current(Box::new(row)),
                ));
            }
            Ok(())
        }

        /// Internal helper function for querying the removed bakers sorted
        /// by the baker ID in descending order.
        async fn query_removed_baker(
            query: ConnectionQuery<BakerIdCursor>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $1 AND id < $2
                    ORDER BY
                        (CASE WHEN $3     THEN id END) DESC,
                        (CASE WHEN NOT $3 THEN id END) ASC
                    LIMIT $4
                ) ORDER BY id ASC",
                query.from,
                query.to,
                query.is_last,
                query.limit,
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = Cursor::First(row.id);
                connection
                    .edges
                    .push(connection::Edge::new(cursor.encode_cursor(), Baker::Previously(row)));
            }
            Ok(())
        }

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // In this connection there are potentially two collections/tables involved,
        // firstly the current bakers sorted by some field, secondly removed bakers when
        // enabled. The strategy is then to query one collection and if the result is
        // below the limit, we query the second collection. The order of which
        // collection to query first and second will depend on the whether the
        // `last` parameter was provided in the top level query.
        if query.is_last {
            if let Some(current_baker_query) = query.subquery_second() {
                query_current_bakers(current_baker_query, open_status_filter, &mut connection, pool)
                    .await?
            }
            if include_removed_filter {
                let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
                if remains_to_limit > 0 {
                    if let Some(removed_baker_query) =
                        query.subquery_first_with_limit(remains_to_limit)
                    {
                        query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                    }

                    // Since we might already have some removed bakers in the page, we sort to make
                    // sure these are last after adding current bakers.
                    connection.edges.sort_by(|left, right| {
                        left.node.cmp_baker_field(&right.node, |left, right| {
                            left.payday_baking_commission_rate()
                                .cmp(&right.payday_baking_commission_rate())
                                .reverse()
                        })
                    });
                }
            }
        } else {
            if include_removed_filter {
                if let Some(removed_baker_query) = query.subquery_first() {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }

            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if remains_to_limit > 0 {
                if let Some(current_baker_query) =
                    query.subquery_second_with_limit(remains_to_limit)
                {
                    query_current_bakers(
                        current_baker_query,
                        open_status_filter,
                        &mut connection,
                        pool,
                    )
                    .await?;
                }
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };

        {
            let collection_ends = sqlx::query!(
                r#"WITH
                    starting_baker as (
                        SELECT
                            bakers.id,
                            payday_baking_commission
                        FROM bakers
                        LEFT JOIN bakers_payday_commission_rates
                            ON bakers_payday_commission_rates.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY payday_baking_commission ASC NULLS FIRST, id ASC
                        LIMIT 1
                    ),
                    ending_baker as (
                        SELECT
                            bakers.id,
                            payday_baking_commission
                        FROM bakers
                        LEFT JOIN bakers_payday_commission_rates
                            ON bakers_payday_commission_rates.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY payday_baking_commission DESC NULLS LAST, id DESC
                        LIMIT 1
                    )
                SELECT
                    starting_baker.id AS start_id,
                    starting_baker.payday_baking_commission AS "start_commission?",
                    ending_baker.id AS end_id,
                    ending_baker.payday_baking_commission AS "end_commission?"
                FROM starting_baker, ending_baker"#,
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_optional(pool)
            .await?;
            if let Some(collection_ends) = collection_ends {
                if let Baker::Current(first_baker) = &first_item.node {
                    let collection_start_cursor = Reversed::new(BakerFieldDescCursor {
                        baker_id: collection_ends.start_id,
                        field:    collection_ends.start_commission.unwrap_or(0),
                    });
                    connection.has_previous_page = collection_start_cursor
                        < Reversed::new(BakerFieldDescCursor::payday_baking_commission_rate(
                            first_baker,
                        ))
                }
                connection.has_next_page = if let Baker::Current(last_item) = &last_item.node {
                    let collection_end_cursor = Reversed::new(BakerFieldDescCursor {
                        baker_id: collection_ends.end_id,
                        field:    collection_ends.end_commission.unwrap_or(0),
                    });
                    collection_end_cursor
                        > Reversed::new(BakerFieldDescCursor::payday_baking_commission_rate(
                            last_item,
                        ))
                } else {
                    true
                }
            }
        }
        if include_removed_filter {
            let min_removed_baker_id =
                sqlx::query_scalar!("SELECT MIN(id) FROM bakers_removed",).fetch_one(pool).await?;
            connection.has_previous_page = if let Some(min_removed_baker_id) = min_removed_baker_id
            {
                first_item.node.get_id() != min_removed_baker_id
            } else {
                false
            };
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn baker_apy_desc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        /// Cursor type for the representing the removed validators ordered
        /// descendingly by ID.
        type RemovedBakerCursor = Reversed<BakerIdCursor>;
        /// Cursor type for the current validators.
        /// Ordered firstly by the optional bakers APY in descending order,
        /// putting the bakers without an APY in the end. Secondly
        /// ordered by the validators ID descendingly.
        type BakerCursor = NestedCursor<OptionCursor<Reversed<F64Cursor>>, Reversed<BakerIdCursor>>;
        /// Cursor type for this connection, which is the concatenation of the
        /// current validators then followed by the removed validators (when
        /// `include_removed_filter` is `true`).
        type Cursor = ConcatCursor<BakerCursor, RemovedBakerCursor>;

        /// Internal helper function for querying the current bakers sorted
        /// by the apy in descending order.
        async fn query_current_bakers(
            query: ConnectionQuery<BakerCursor>,
            open_status_filter: Option<BakerPoolOpenStatus>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let from_apy = query.from.outer.first().map_or(0.0, |b| b.cursor.value);
            let to_apy = query.to.outer.first().map_or(0.0, |b| b.cursor.value);
            let from_baker_id = query.from.inner.cursor;
            let to_baker_id = query.to.inner.cursor;

            let mut row_stream = sqlx::query_as!(
                CurrentBaker,
                r#"
SELECT * FROM (SELECT
    bakers.id AS id,
    staked,
    restake_earnings,
    open_status as "open_status: BakerPoolOpenStatus",
    metadata_url,
    transaction_commission,
    baking_commission,
    finalization_commission,
    payday_transaction_commission as "payday_transaction_commission?",
    payday_baking_commission as "payday_baking_commission?",
    payday_finalization_commission as "payday_finalization_commission?",
    payday_lottery_power as "payday_lottery_power?",
    payday_ranking_by_lottery_powers as "payday_ranking_by_lottery_powers?",
    (SELECT MAX(payday_ranking_by_lottery_powers) FROM bakers_payday_lottery_powers)
        AS "payday_total_ranking_by_lottery_powers?",
    pool_total_staked,
    pool_delegator_count,
    self_suspended,
    inactive_suspended,
    primed_for_suspension,
    baker_apy AS "baker_apy?",
    delegators_apy AS "delegators_apy?"
FROM bakers
    LEFT JOIN latest_baker_apy_30_days ON latest_baker_apy_30_days.id = bakers.id
    LEFT JOIN bakers_payday_lottery_powers ON bakers_payday_lottery_powers.id = bakers.id
    LEFT JOIN bakers_payday_commission_rates ON bakers_payday_commission_rates.id = bakers.id
WHERE
    (
      -- Start outer bound for page
      (COALESCE(baker_apy, 0) < $1::FLOAT8
      -- End outer bound for page
      AND COALESCE(baker_apy, 0) > $2::FLOAT8)
      -- When outer bounds are not equal, filter separate for each inner bound.
      OR (
          $1 != $2
          AND (
               -- Start inner bound for page.
               (COALESCE(baker_apy, 0) = $1 AND bakers.id < $3)
               -- End inner bound for page.
                OR (COALESCE(baker_apy, 0) = $2 AND bakers.id > $4)
          )
      )
      -- When outer bounds are equal, use one filter for both bounds.
      OR (
          $1 = $2
          AND (COALESCE(baker_apy, 0) = $1
          AND bakers.id < $3 AND bakers.id > $4))
    )
    -- filter if provided
    AND ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
ORDER BY
    (CASE WHEN $5     THEN baker_apy END) ASC NULLS FIRST,
    (CASE WHEN $5     THEN bakers.id END) ASC,
    (CASE WHEN NOT $5 THEN baker_apy END) DESC NULLS LAST,
    (CASE WHEN NOT $5 THEN bakers.id END) DESC
LIMIT $6
) ORDER BY "baker_apy?" DESC NULLS LAST, id DESC"#,
                from_apy,                                          // $1
                to_apy,                                            // $2
                from_baker_id,                                     // $3
                to_baker_id,                                       // $4
                query.is_last,                                     // $5
                query.limit,                                       // $6
                open_status_filter as Option<BakerPoolOpenStatus>  // $7
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = ConcatCursor::First(BakerCursor {
                    outer: row.baker_apy.map(|apy| Reversed::new(F64Cursor::new(apy))).into(),
                    inner: Reversed::new(row.id),
                });
                connection.edges.push(connection::Edge::new(
                    cursor.encode_cursor(),
                    Baker::Current(Box::new(row)),
                ));
            }
            Ok(())
        }

        /// Internal helper function for querying the removed bakers sorted
        /// by the baker ID in descending order.
        async fn query_removed_baker(
            query: ConnectionQuery<Reversed<BakerIdCursor>>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $2 AND id < $1
                    ORDER BY
                        (CASE WHEN $3     THEN id END) ASC,
                        (CASE WHEN NOT $3 THEN id END) DESC
                    LIMIT $4
                ) ORDER BY id DESC",
                query.from.cursor,
                query.to.cursor,
                query.is_last,
                query.limit,
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = Cursor::Second(Reversed::new(row.id));
                connection
                    .edges
                    .push(connection::Edge::new(cursor.encode_cursor(), Baker::Previously(row)));
            }
            Ok(())
        }

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // In this connection there are potentially two collections/tables involved,
        // firstly the current bakers sorted by some field, secondly removed bakers when
        // enabled. The strategy is then to query one collection and if the result is
        // below the limit, we query the second collection. The order of which
        // collection to query first and second will depend on the whether the
        // `last` parameter was provided in the top level query.
        if query.is_last {
            if include_removed_filter {
                if let Some(removed_baker_query) = query.subquery_second() {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if remains_to_limit > 0 {
                if let Some(current_baker_query) = query.subquery_first_with_limit(remains_to_limit)
                {
                    query_current_bakers(
                        current_baker_query,
                        open_status_filter,
                        &mut connection,
                        pool,
                    )
                    .await?;
                }
            }
            if include_removed_filter {
                // Since we might already have some removed bakers in the page, we sort to make
                // sure these are last after adding current bakers.
                connection.edges.sort_by_key(|edge| {
                    Cursor::decode_cursor(edge.cursor.as_str()).expect("Invalid cursor encoding")
                });
            }
        } else {
            if let Some(current_baker_query) = query.subquery_first() {
                query_current_bakers(
                    current_baker_query,
                    open_status_filter,
                    &mut connection,
                    pool,
                )
                .await?;
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if include_removed_filter && remains_to_limit > 0 {
                if let Some(removed_baker_query) =
                    query.subquery_second_with_limit(remains_to_limit)
                {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };

        {
            let collection_ends = sqlx::query!(
                r#"WITH
                    starting_baker as (
                        SELECT
                            bakers.id,
                            baker_apy
                        FROM bakers
                        LEFT JOIN latest_baker_apy_30_days
                             ON latest_baker_apy_30_days.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY baker_apy DESC NULLS LAST, id DESC
                        LIMIT 1
                    ),
                    ending_baker as (
                        SELECT
                            bakers.id,
                            baker_apy
                        FROM bakers
                        LEFT JOIN latest_baker_apy_30_days
                             ON latest_baker_apy_30_days.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY baker_apy ASC NULLS FIRST, id ASC
                        LIMIT 1
                    )
                SELECT
                    starting_baker.id AS start_id,
                    starting_baker.baker_apy AS "start_baker_apy?",
                    ending_baker.id AS end_id,
                    ending_baker.baker_apy AS "end_baker_apy?"
                FROM starting_baker, ending_baker"#,
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_optional(pool)
            .await?;
            if let Some(collection_ends) = collection_ends {
                connection.has_previous_page = if let Baker::Current(first_item) = &first_item.node
                {
                    let collection_start_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: collection_ends
                            .start_baker_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(collection_ends.start_id),
                    });
                    let page_first_item_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: first_item
                            .baker_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(first_item.id),
                    });
                    collection_start_cursor < page_first_item_cursor
                } else {
                    true
                };
                connection.has_next_page = if let Baker::Current(last_item) = &last_item.node {
                    let collection_end_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: collection_ends
                            .end_baker_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(collection_ends.end_id),
                    });
                    let page_last_item_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: last_item
                            .baker_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(last_item.id),
                    });
                    collection_end_cursor > page_last_item_cursor
                } else {
                    true
                }
            }
        }

        if include_removed_filter {
            let min_removed_baker_id =
                sqlx::query_scalar!("SELECT MIN(id) FROM bakers_removed").fetch_one(pool).await?;
            connection.has_next_page = if let Some(min_removed_baker_id) = min_removed_baker_id {
                last_item.node.get_id() != min_removed_baker_id
            } else {
                false
            }
        }
        Ok(connection)
    }

    #[allow(clippy::too_many_arguments)]
    async fn delegator_apy_desc_connection(
        config: &ApiServiceConfig,
        pool: &PgPool,
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        open_status_filter: Option<BakerPoolOpenStatus>,
        include_removed_filter: bool,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        /// Cursor type for the representing the removed validators ordered
        /// descendingly by ID.
        type RemovedBakerCursor = Reversed<BakerIdCursor>;
        /// Cursor type for the current validators.
        /// Ordered firstly by the optional delegators APY in descending order,
        /// putting the bakers without an APY in the end.
        /// Secondly ordered by the validators ID descendingly.
        type BakerCursor = NestedCursor<OptionCursor<Reversed<F64Cursor>>, Reversed<BakerIdCursor>>;
        /// Cursor type for this connection, which is the concatenation of the
        /// current validators then followed by the removed validators (when
        /// `include_removed_filter` is `true`).
        type Cursor = ConcatCursor<BakerCursor, RemovedBakerCursor>;

        /// Internal helper function for querying the current bakers sorted
        /// by the delegator apy in descending order.
        async fn query_current_bakers(
            query: ConnectionQuery<BakerCursor>,
            open_status_filter: Option<BakerPoolOpenStatus>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                CurrentBaker,
                r#"
SELECT * FROM (SELECT
    bakers.id AS id,
    staked,
    restake_earnings,
    open_status as "open_status: BakerPoolOpenStatus",
    metadata_url,
    transaction_commission,
    baking_commission,
    finalization_commission,
    payday_transaction_commission as "payday_transaction_commission?",
    payday_baking_commission as "payday_baking_commission?",
    payday_finalization_commission as "payday_finalization_commission?",
    payday_lottery_power as "payday_lottery_power?",
    payday_ranking_by_lottery_powers as "payday_ranking_by_lottery_powers?",
    (SELECT MAX(payday_ranking_by_lottery_powers) FROM bakers_payday_lottery_powers)
        AS "payday_total_ranking_by_lottery_powers?",
    pool_total_staked,
    pool_delegator_count,
    self_suspended,
    inactive_suspended,
    primed_for_suspension,
    baker_apy AS "baker_apy?",
    delegators_apy AS "delegators_apy?"
FROM bakers
    LEFT JOIN latest_baker_apy_30_days ON latest_baker_apy_30_days.id = bakers.id
    LEFT JOIN bakers_payday_lottery_powers ON bakers_payday_lottery_powers.id = bakers.id
    LEFT JOIN bakers_payday_commission_rates ON bakers_payday_commission_rates.id = bakers.id
WHERE
    (
      -- Start outer bound for page
      (COALESCE(delegators_apy, 0) < $1::FLOAT8
      -- End outer bound for page
      AND COALESCE(delegators_apy, 0) > $2::FLOAT8)
      -- When outer bounds are not equal, filter separate for each inner bound.
      OR (
          $1 != $2
          AND (
               -- Start inner bound for page.
               (COALESCE(delegators_apy, 0) = $1 AND bakers.id < $3)
               -- End inner bound for page.
               OR (COALESCE(delegators_apy, 0) = $2 AND bakers.id > $4)
          )
      )
      -- When outer bounds are equal, use one filter for both bounds.
      OR (
          $1 = $2
          AND (COALESCE(delegators_apy, 0) = $1
          AND bakers.id < $3 AND bakers.id > $4))
    )
    -- filter if provided
    AND ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
ORDER BY
    (CASE WHEN $5     THEN delegators_apy END) ASC NULLS FIRST,
    (CASE WHEN $5     THEN bakers.id END) ASC,
    (CASE WHEN NOT $5 THEN delegators_apy END) DESC NULLS LAST,
    (CASE WHEN NOT $5 THEN bakers.id END) DESC
LIMIT $6
) ORDER BY "delegators_apy?" DESC NULLS LAST, id DESC"#,
                query.from.outer.first().map_or(0.0, |b| b.cursor.value), // $1
                query.to.outer.first().map_or(0.0, |b| b.cursor.value),   // $2
                query.from.inner.cursor,                                  // $3
                query.to.inner.cursor,                                    // $4
                query.is_last,                                            // $5
                query.limit,                                              // $6
                open_status_filter as Option<BakerPoolOpenStatus>         // $7
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = ConcatCursor::First(BakerCursor {
                    outer: row.delegators_apy.map(|apy| Reversed::new(F64Cursor::new(apy))).into(),
                    inner: Reversed::new(row.id),
                });
                connection.edges.push(connection::Edge::new(
                    cursor.encode_cursor(),
                    Baker::Current(Box::new(row)),
                ));
            }
            Ok(())
        }

        /// Internal helper function for querying the removed bakers sorted
        /// by the baker ID in descending order.
        async fn query_removed_baker(
            query: ConnectionQuery<Reversed<BakerIdCursor>>,
            connection: &mut connection::Connection<String, Baker>,
            pool: &PgPool,
        ) -> ApiResult<()> {
            let mut row_stream = sqlx::query_as!(
                PreviouslyBaker,
                "SELECT * FROM (
                    SELECT
                        id,
                        slot_time AS removed_at
                    FROM bakers_removed
                        JOIN transactions
                            ON transactions.index = bakers_removed.removed_by_tx_index
                        JOIN blocks ON blocks.height = transactions.block_height
                    WHERE id > $2 AND id < $1
                    ORDER BY
                        (CASE WHEN $3     THEN id END) ASC,
                        (CASE WHEN NOT $3 THEN id END) DESC
                    LIMIT $4
                ) ORDER BY id DESC",
                query.from.cursor,
                query.to.cursor,
                query.is_last,
                query.limit,
            )
            .fetch(pool);
            while let Some(row) = row_stream.try_next().await? {
                let cursor: Cursor = Cursor::Second(Reversed::new(row.id));
                connection
                    .edges
                    .push(connection::Edge::new(cursor.encode_cursor(), Baker::Previously(row)));
            }
            Ok(())
        }

        let query = ConnectionQuery::<Cursor>::new(
            first,
            after,
            last,
            before,
            config.baker_connection_limit,
        )?;
        let mut connection = connection::Connection::new(false, false);

        // In this connection there are potentially two collections/tables involved,
        // firstly the current bakers sorted by some field, secondly removed bakers when
        // enabled. The strategy is then to query one collection and if the result is
        // below the limit, we query the second collection. The order of which
        // collection to query first and second will depend on the whether the
        // `last` parameter was provided in the top level query.
        if query.is_last {
            if include_removed_filter {
                if let Some(removed_baker_query) = query.subquery_second() {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if remains_to_limit > 0 {
                if let Some(current_baker_query) = query.subquery_first_with_limit(remains_to_limit)
                {
                    query_current_bakers(
                        current_baker_query,
                        open_status_filter,
                        &mut connection,
                        pool,
                    )
                    .await?;
                }
            }
            if include_removed_filter {
                // Since we might already have some removed bakers in the page, we sort to make
                // sure these are last after adding current bakers.
                connection.edges.sort_by_key(|edge| {
                    Cursor::decode_cursor(edge.cursor.as_str()).expect("Invalid cursor encoding")
                });
            }
        } else {
            if let Some(current_baker_query) = query.subquery_first() {
                query_current_bakers(
                    current_baker_query,
                    open_status_filter,
                    &mut connection,
                    pool,
                )
                .await?;
            }
            let remains_to_limit = query.limit - i64::try_from(connection.edges.len())?;
            if include_removed_filter && remains_to_limit > 0 {
                if let Some(removed_baker_query) =
                    query.subquery_second_with_limit(remains_to_limit)
                {
                    query_removed_baker(removed_baker_query, &mut connection, pool).await?;
                }
            }
        }

        let (Some(first_item), Some(last_item)) =
            (connection.edges.first(), connection.edges.last())
        else {
            // No items so we just return without updating next/prev page info.
            return Ok(connection);
        };

        {
            let collection_ends = sqlx::query!(
                r#"WITH
                    starting_baker as (
                        SELECT
                            bakers.id,
                            delegators_apy
                        FROM bakers
                        LEFT JOIN latest_baker_apy_30_days
                             ON latest_baker_apy_30_days.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY delegators_apy DESC NULLS LAST, id DESC
                        LIMIT 1
                    ),
                    ending_baker as (
                        SELECT
                            bakers.id,
                            delegators_apy
                        FROM bakers
                        LEFT JOIN latest_baker_apy_30_days
                             ON latest_baker_apy_30_days.id = bakers.id
                        WHERE $1::pool_open_status IS NULL OR open_status = $1::pool_open_status
                        ORDER BY delegators_apy ASC NULLS FIRST, id ASC
                        LIMIT 1
                    )
                SELECT
                    starting_baker.id AS start_id,
                    starting_baker.delegators_apy AS "start_delegators_apy?",
                    ending_baker.id AS end_id,
                    ending_baker.delegators_apy AS "end_delegators_apy?"
                FROM starting_baker, ending_baker"#,
                open_status_filter as Option<BakerPoolOpenStatus>
            )
            .fetch_optional(pool)
            .await?;
            if let Some(collection_ends) = collection_ends {
                connection.has_previous_page = if let Baker::Current(first_item) = &first_item.node
                {
                    let collection_start_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: collection_ends
                            .start_delegators_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(collection_ends.start_id),
                    });
                    let page_first_item_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: first_item
                            .delegators_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(first_item.id),
                    });
                    collection_start_cursor < page_first_item_cursor
                } else {
                    true
                };
                connection.has_next_page = if let Baker::Current(last_item) = &last_item.node {
                    let collection_end_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: collection_ends
                            .end_delegators_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(collection_ends.end_id),
                    });
                    let page_last_item_cursor: Cursor = ConcatCursor::First(NestedCursor {
                        outer: last_item
                            .delegators_apy
                            .map(|apy| Reversed::new(F64Cursor::new(apy)))
                            .into(),
                        inner: Reversed::new(last_item.id),
                    });
                    collection_end_cursor > page_last_item_cursor
                } else {
                    true
                }
            }
        }

        if include_removed_filter {
            let min_removed_baker_id =
                sqlx::query_scalar!("SELECT MIN(id) FROM bakers_removed").fetch_one(pool).await?;
            connection.has_next_page = if let Some(min_removed_baker_id) = min_removed_baker_id {
                last_item.node.get_id() != min_removed_baker_id
            } else {
                false
            }
        }
        Ok(connection)
    }
}

#[derive(Debug)]
pub struct PreviouslyBaker {
    id:         i64,
    removed_at: DateTime,
}

impl PreviouslyBaker {
    async fn query_by_id(pool: &PgPool, baker_id: i64) -> ApiResult<Option<Self>> {
        let removed = sqlx::query_as!(
            PreviouslyBaker,
            "SELECT
                id,
                slot_time AS removed_at
            FROM bakers_removed
                JOIN transactions ON transactions.index = bakers_removed.removed_by_tx_index
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE bakers_removed.id = $1",
            baker_id
        )
        .fetch_optional(pool)
        .await?;
        Ok(removed)
    }

    fn state(&self) -> ApiResult<BakerState<'_>> {
        let state = BakerState::RemovedBakerState(RemovedBakerState {
            removed_at: self.removed_at,
        });
        Ok(state)
    }
}

/// Database information for a current baker.
#[derive(Debug)]
pub struct CurrentBaker {
    pub id: i64,
    pub staked: i64,
    pub restake_earnings: bool,
    pub open_status: Option<BakerPoolOpenStatus>,
    pub metadata_url: Option<MetadataUrl>,
    pub self_suspended: Option<i64>,
    pub inactive_suspended: Option<i64>,
    pub primed_for_suspension: Option<i64>,
    pub transaction_commission: Option<i64>,
    pub baking_commission: Option<i64>,
    pub finalization_commission: Option<i64>,
    pub payday_transaction_commission: Option<i64>,
    pub payday_baking_commission: Option<i64>,
    pub payday_finalization_commission: Option<i64>,
    pub payday_lottery_power: Option<BigDecimal>,
    pub payday_ranking_by_lottery_powers: Option<i64>,
    pub payday_total_ranking_by_lottery_powers: Option<i64>,
    pub pool_total_staked: i64,
    pub pool_delegator_count: i64,
    // 30 days period APY for bakers.
    pub baker_apy: Option<f64>,
    // 30 days period APY for delegators.
    pub delegators_apy: Option<f64>,
}
impl CurrentBaker {
    /// Get the current payday baking commission rate.
    fn payday_baking_commission_rate(&self) -> i64 { self.payday_baking_commission.unwrap_or(0) }

    pub async fn query_by_id(pool: &PgPool, baker_id: i64) -> ApiResult<Option<Self>> {
        Ok(sqlx::query_as!(
            CurrentBaker,
            r#"
            SELECT
                bakers.id as id,
                staked,
                restake_earnings,
                open_status as "open_status: BakerPoolOpenStatus",
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
                LEFT JOIN latest_baker_apy_30_days ON latest_baker_apy_30_days.id = bakers.id
                LEFT JOIN bakers_payday_commission_rates ON bakers_payday_commission_rates.id = bakers.id
                LEFT JOIN bakers_payday_lottery_powers ON bakers_payday_lottery_powers.id = bakers.id
            WHERE bakers.id = $1;
            "#,
            baker_id
        )
        .fetch_optional(pool)
        .await?)
    }

    async fn state(&self, ctx: &Context<'_>) -> ApiResult<BakerState<'_>> {
        let pool = get_pool(ctx)?;

        let transaction_commission = self
            .transaction_commission
            .map(u32::try_from)
            .transpose()?
            .map(|c| AmountFraction::new_unchecked(c).into());
        let baking_commission = self
            .baking_commission
            .map(u32::try_from)
            .transpose()?
            .map(|c| AmountFraction::new_unchecked(c).into());
        let finalization_commission = self
            .finalization_commission
            .map(u32::try_from)
            .transpose()?
            .map(|c| AmountFraction::new_unchecked(c).into());

        // `payday_transaction_commission`, `payday_baking_commission` and
        // `payday_finalization_commission` are either set or not set
        // for a given baker. Hence we only check if `payday_transaction_commission` is
        // set.
        let payday_commission_rates = if self.payday_transaction_commission.is_some() {
            let payday_transaction_commission = self
                .payday_transaction_commission
                .map(u32::try_from)
                .transpose()?
                .map(|c| AmountFraction::new_unchecked(c).into());
            let payday_baking_commission = self
                .payday_baking_commission
                .map(u32::try_from)
                .transpose()?
                .map(|c| AmountFraction::new_unchecked(c).into());
            let payday_finalization_commission = self
                .payday_finalization_commission
                .map(u32::try_from)
                .transpose()?
                .map(|c| AmountFraction::new_unchecked(c).into());
            Some(CommissionRates {
                transaction_commission:  payday_transaction_commission,
                baking_commission:       payday_baking_commission,
                finalization_commission: payday_finalization_commission,
            })
        } else {
            None
        };

        let total_stake: i64 =
            sqlx::query_scalar!("SELECT total_staked FROM blocks ORDER BY height DESC LIMIT 1")
                .fetch_one(pool)
                .await?;

        let delegated_stake_of_pool = self.pool_total_staked - self.staked;

        // Division by 0 is not possible because `total_stake` is always a
        // positive number.
        let total_stake_percentage = (rust_decimal::Decimal::from(self.pool_total_staked)
            * rust_decimal::Decimal::from(100))
        .checked_div(rust_decimal::Decimal::from(total_stake))
        .ok_or_else(|| ApiError::InternalError("Division by zero".to_string()))?
        .into();

        // The code is re-implemented from the node code so that the node and the
        // CCDScan report the same values:
        // https://github.com/Concordium/concordium-node/blob/3cb759e4607f20a9df94ace017f0e3c775d4cdb3/concordium-consensus/src/Concordium/Kontrol/Bakers.hs#L53

        // The delegated capital cap in the node (called delegated stake cap in CCDScan)
        // is defined to be the minimum of the capital bound cap and the
        // leverage bound cap:

        // leverage bound cap for pool p: Lₚ = λ * Cₚ - Cₚ = (λ - 1) * Cₚ
        // capital bound cap for pool p: Bₚ = floor( (κ * (T - Dₚ) - Cₚ) / (1 - K) )

        // Where
        // κ is the capital bound
        // λ is the leverage bound
        // T is the total staked capital on the whole chain (including passive
        // delegation)
        // Dₚ is the delegated capital of pool p
        // Cₚ is the equity capital (staked by the pool owner excluding delegated stake
        // to the pool) of pool p

        // The `leverage bound cap` ensures that each baker has skin
        // in the game with respect to its delegators by providing some of the CCD
        // staked from its own funds. The `capital bound cap` helps maintain
        // network decentralization by preventing a single baker from gaining
        // excessive power in the consensus protocol.

        let current_chain_parameters = sqlx::query_as!(
            DelegatedStakeBounds,
            "
            SELECT 
                leverage_bound_numerator,
                leverage_bound_denominator,
                capital_bound 
            FROM current_chain_parameters 
            WHERE id = true
            "
        )
        .fetch_one(pool)
        .await?;

        // The `leverage_bound` and `capital_bound` are Concordium chain parameters that
        // were set adhering to the below constraints to ensure the consensus
        // algorithm works. We check these constraints here to ensure the values
        // have been saved in this format to the database.
        if current_chain_parameters.leverage_bound_numerator < 0 {
            return Err(ApiError::InternalError(
                "`leverage_bound_numerator` is negative in the database".to_string(),
            ));
        }
        if current_chain_parameters.leverage_bound_denominator <= 0 {
            return Err(ApiError::InternalError(
                "`leverage_bound_denominator` is not greater than 0 in the database".to_string(),
            ));
        }

        if current_chain_parameters.leverage_bound_numerator
            < current_chain_parameters.leverage_bound_denominator
        {
            return Err(ApiError::InternalError(
                "`leverage_bound` is smaller than 1 in the database".to_string(),
            ));
        }
        if current_chain_parameters.capital_bound <= 0 {
            return Err(ApiError::InternalError(
                "`capital_bound` is not greater than 0 in the database".to_string(),
            ));
        }

        // Calculating the `leverage_bound_cap`

        #[rustfmt::skip]
        // Transformation applied to the `leverage_bound_cap_for_pool` formula:
        //
        // `leverage_bound_cap_for_pool`
        // = (λ – 1) * Cₚ
        // = (leverage_bound_numerator / leverage_bound_denominator – 1) * Cₚ
        // = (leverage_bound_numerator / leverage_bound_denominator – (leverage_bound_denominator / leverage_bound_denominator)) * Cₚ
        // = (leverage_bound_numerator – leverage_bound_denominator) / leverage_bound_denominator) * Cₚ
        // = (leverage_bound_numerator – leverage_bound_denominator) * Cₚ / leverage_bound_denominator
        //
        // WHERE
        // λ is the leverage bound
        // Cₚ is the equity capital (staked by the pool owner excluding delegated stake
        // to the pool) of pool p
        // `leverage_bound_numerator` is the value as stored in the database
        // `leverage_bound_denominator` is the value as stored in the database

        // To reduce loss of precision, the value is computed in u128.
        let leverage_bound_cap_for_pool_numerator: u128 =
            (current_chain_parameters.leverage_bound_numerator
                - current_chain_parameters.leverage_bound_denominator) as u128
                * self.staked as u128;
        // Denominator is not zero since we checked that before.
        let leverage_bound_cap_for_pool_denominator: u128 =
            current_chain_parameters.leverage_bound_denominator as u128;

        let leverage_bound_cap_for_pool: u64 = (leverage_bound_cap_for_pool_numerator
            / leverage_bound_cap_for_pool_denominator)
            .try_into()
            .unwrap_or(u64::MAX);
        let leverage_bound_cap_for_pool: Amount = leverage_bound_cap_for_pool.into();

        // Calculating the `capital_bound_cap`

        #[rustfmt::skip]
        // Transformation applied to the `capital_bound_cap_for_pool` formula:
        //
        // `capital_bound_cap_for_pool`
        // = floor( (κ * (T - Dₚ) - Cₚ) / (1 - K) )
        // = floor( (capital_bound/100_000 * (T - Dₚ) - Cₚ) / (1 - capital_bound/100_000) )
        // (Explanation: Since the `capital_bound (from the database)` is stored as a fraction with
        // precision of `1/100_000` in the database)
        //
        // = floor( ((capital_bound / 100_000 * (T - Dₚ) - Cₚ)  / (1 - capital_bound / 100_000)) * 1 )
        // = floor( ((capital_bound / 100_000 * (T - Dₚ) - Cₚ)  / (1 - capital_bound / 100_000)) * (100_000 / 100_000) )
        // = floor( (capital_bound / 100_000 * (T - Dₚ) - Cₚ) * 100_000 / (1 - capital_bound / 100_000) * 100_000) )
        // = floor( (capital_bound * (T - Dₚ) - 100_000 * Cₚ) / (100_000 - capital_bound) )

        // WHERE
        // κ is the capital bound
        // T is the total staked capital on the whole chain (including passive
        // delegation)
        // Dₚ is the delegated capital of pool p
        // Cₚ is the equity capital (staked by the pool owner excluding delegated stake
        // to the pool) of pool p
        // `capital_bound` is the value as stored in the database

        let capital_bound: u128 = current_chain_parameters.capital_bound as u128;

        let capital_bound_cap_for_pool: Amount = if capital_bound == 100_000u128 {
            // To avoid dividing by 0 in the `capital bound cap` formula,
            // we only apply the `leverage_bound_cap_for_pool` in that case.
            u64::MAX.into()
        } else {
            // Since the `capital_bound` is stored as a fraction with precision of
            // `1/100_000` in the database, we multiply the numerator and
            // denominator by 100_000. To reduce loss of precision, the value is computed in
            // u128.
            let capital_bound_cap_for_pool_numerator = (capital_bound
                * ((total_stake - delegated_stake_of_pool) as u128))
                .saturating_sub(100_000 * (self.staked as u128));

            // Denominator is not zero since we checked that `capital_bound != 100_000`.
            let capital_bound_cap_for_pool_denominator: u128 = 100_000u128 - capital_bound;

            let capital_bound_cap_for_pool: u64 = (capital_bound_cap_for_pool_numerator
                / capital_bound_cap_for_pool_denominator)
                .try_into()
                .unwrap_or(u64::MAX);

            capital_bound_cap_for_pool.into()
        };

        let delegated_stake_cap = min(leverage_bound_cap_for_pool, capital_bound_cap_for_pool);

        // Get the rank of the baker based on its lottery power.
        // An account id that is not a baker has no `payday_ranking_by_lottery_powers`
        // in general. A new baker has as well no
        // `payday_ranking_by_lottery_powers` until the next payday. We return
        // `None` as ranking in both cases. A baker that got just removed has a
        // `payday_ranking_by_lottery_powers` until the next payday. We return a
        // ranking in that case.
        let rank = match (
            self.payday_ranking_by_lottery_powers,
            self.payday_total_ranking_by_lottery_powers,
        ) {
            (Some(rank), Some(total)) => Some(Ranking {
                rank,
                total,
            }),
            (None, _) => None,
            (Some(_), None) => {
                return Err(ApiError::InternalError(
                    "Invalid ranking state in database".to_string(),
                ))
            }
        };

        let baker_id: u64 = self.id.try_into().map_err(|_| {
            ApiError::InternalError(format!("A baker has a negative id: {}", self.id))
        })?;

        let handler = ctx.data::<NodeInfoReceiver>().map_err(ApiError::NoReceiver)?;
        let statuses_ref = handler.borrow();
        let statuses = statuses_ref.as_ref().ok_or(ApiError::InternalError(
            "Node collector backend has not responded".to_string(),
        ))?;

        let node =
            statuses.iter().find(|x| x.external.consensus_baker_id == Some(baker_id)).cloned();
        let out = BakerState::ActiveBakerState(Box::new(ActiveBakerState {
            node_status:      node,
            staked_amount:    Amount::try_from(self.staked)?,
            restake_earnings: self.restake_earnings,
            pool:             BakerPool {
                id: self.id,
                open_status: self.open_status,
                commission_rates: CommissionRates {
                    transaction_commission,
                    baking_commission,
                    finalization_commission,
                },
                rank,
                payday_commission_rates,
                payday_lottery_power: self
                    .payday_lottery_power
                    .as_ref()
                    .unwrap_or(&BigDecimal::default())
                    .try_into()
                    .map_err(|e: anyhow::Error| ApiError::InternalError(e.to_string()))?,
                metadata_url: self.metadata_url.as_deref(),
                self_suspended: self.self_suspended,
                inactive_suspended: self.inactive_suspended,
                primed_for_suspension: self.primed_for_suspension,
                total_stake_percentage,
                total_stake: Amount::try_from(self.pool_total_staked)?,
                delegated_stake: Amount::try_from(delegated_stake_of_pool)?,
                delegated_stake_cap,
                delegator_count: self.pool_delegator_count,
            },
            pending_change:   None, // This is not used starting from P7.
        }));
        Ok(out)
    }
}
#[Object]
impl Baker {
    async fn id(&self) -> types::ID { types::ID::from(self.get_id().to_string()) }

    async fn baker_id(&self) -> BakerId { self.get_id().into() }

    async fn state(&self, ctx: &Context<'_>) -> ApiResult<BakerState<'_>> {
        match self {
            Baker::Current(baker) => baker.state(ctx).await,
            Baker::Previously(removed) => removed.state(),
        }
    }

    async fn account(&self, ctx: &Context<'_>) -> ApiResult<Account> {
        Account::query_by_index(get_pool(ctx)?, self.get_id()).await?.ok_or(ApiError::NotFound)
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
    ) -> ApiResult<connection::Connection<String, InterimTransaction>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.transactions_per_block_connection_limit,
        )?;

        let account_transaction_type_filter = &[
            AccountTransactionType::AddBaker,
            AccountTransactionType::RemoveBaker,
            AccountTransactionType::UpdateBakerStake,
            AccountTransactionType::UpdateBakerRestakeEarnings,
            AccountTransactionType::UpdateBakerKeys,
            AccountTransactionType::ConfigureBaker,
        ];

        let account_index = self.get_id();

        // Retrieves the transactions related to a baker account ('AddBaker',
        // 'RemoveBaker', 'UpdateBakerStake', 'UpdateBakerRestakeEarnings',
        // 'UpdateBakerKeys', 'ConfigureBaker'). The transactions are ordered in
        // descending order (outer `ORDER BY`). If the `last` input parameter is
        // set, the inner `ORDER BY` reverses the transaction order to allow the
        // range be applied starting from the last element.
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
                WHERE transactions.sender_index = $5
                AND type_account = ANY($6)
                AND index > $1 AND index < $2
                ORDER BY
                    CASE WHEN NOT $3 THEN index END DESC,
                    CASE WHEN $3 THEN index END ASC
                LIMIT $4
            ) ORDER BY index DESC"#,
            query.from,
            query.to,
            query.is_last,
            query.limit,
            account_index,
            account_transaction_type_filter as &[AccountTransactionType]
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

            connection.edges.push(connection::Edge::new(
                tx.index.to_string(),
                InterimTransaction {
                    transaction: tx,
                },
            ));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (page_min_index, page_max_index) {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as max_id, MIN(index) as min_id
                    FROM transactions
                    WHERE transactions.sender_index = $1
                    AND type_account = ANY($2)
                ",
                account_index,
                account_transaction_type_filter as &[AccountTransactionType]
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

// Future improvement (API breaking changes): The function `Baker::transactions`
// can directly return a `Transaction` instead of the `IterimTransaction` type
// here.
#[derive(SimpleObject)]
struct InterimTransaction {
    transaction: Transaction,
}

#[derive(Union)]
enum BakerState<'a> {
    ActiveBakerState(Box<ActiveBakerState<'a>>),
    RemovedBakerState(RemovedBakerState),
}

#[derive(SimpleObject)]
struct ActiveBakerState<'a> {
    /// The status of the baker's node. Will be null if no status for the node
    /// exists.
    node_status:      Option<NodeStatus>,
    staked_amount:    Amount,
    restake_earnings: bool,
    pool:             BakerPool<'a>,
    // This will not be used starting from P7
    pending_change:   Option<PendingBakerChange>,
}

#[derive(Union)]
enum PendingBakerChange {
    PendingBakerRemoval(PendingBakerRemoval),
    PendingBakerReduceStake(PendingBakerReduceStake),
}

#[derive(SimpleObject)]
struct PendingBakerRemoval {
    effective_time: DateTime,
}

#[derive(SimpleObject)]
struct PendingBakerReduceStake {
    new_staked_amount: Amount,
    effective_time:    DateTime,
}

#[derive(SimpleObject)]
struct RemovedBakerState {
    removed_at: DateTime,
}

#[derive(InputObject, Clone, Copy)]
struct BakerFilterInput {
    open_status_filter: Option<BakerPoolOpenStatus>,
    include_removed:    Option<bool>,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, Default)]
enum BakerSort {
    #[default]
    BakerIdAsc,
    BakerIdDesc,
    TotalStakedAmountDesc,
    DelegatorCountDesc,
    #[graphql(name = "BAKER_APY30_DAYS_DESC")]
    BakerApy30DaysDesc,
    #[graphql(name = "DELEGATOR_APY30_DAYS_DESC")]
    DelegatorApy30DaysDesc,
    /// Sort ascending by the current payday baking commission rate.
    BlockCommissionsAsc,
    /// Sort descending by the current payday baking commission rate.
    BlockCommissionsDesc,
}

struct BakerPool<'a> {
    id: i64,
    /// Total stake of the baker pool as a percentage of all CCDs in existence.
    /// Includes both baker stake and delegated stake.
    total_stake_percentage: Decimal,
    /// The total amount staked in this baker pool. Includes both baker stake
    /// and delegated stake.
    total_stake: Amount,
    /// The total amount staked by delegators to this baker pool.
    delegated_stake: Amount,
    /// The number of delegators that delegate to this baker pool.
    delegator_count: i64,
    /// The `commission_rates` represent the current commission settings of a
    /// baker pool, as configured by the baker account through
    /// `bakerConfiguration` transactions. These values are updated
    /// immediatly by observing the following `BakerEvent`s in the indexer:
    /// - `BakerSetBakingRewardCommission`
    /// - `BakerSetTransactionFeeCommission`
    /// - `BakerSetFinalizationRewardCommission`
    ///
    /// Both `commission_rates` and `payday_commission_rates` are optional and
    /// usually return the same value. But at the following edge cases, they
    /// return different values:
    /// - When a validator is initially added (observed by the event
    ///   `BakerEvent::Added`), only `commission_rates` are available until the
    ///   next payday. The `payday_commission_rates` will be set at the next
    ///   payday.
    /// - When a validator is removed (observed by the event
    ///   `BakerEvent::Removed`), `commission_rates` are immediately cleared
    ///   from the bakers table upon detecting the `BakerEvent::Removed`,
    ///   whereas `payday_commission_rates` persist until the next payday.
    commission_rates: CommissionRates,
    /// The `payday_commission_rates` represent the commission settings at the
    /// last payday block. These values are retrieved from the
    /// `get_bakers_reward_period(BlockIdentifier::AbsoluteHeight(payday_block_height))`
    /// endpoint at each payday.
    ///
    /// Both `commission_rates` and `payday_commission_rates` are optional and
    /// usually return the same value. But at the following edge cases, they
    /// return different values:
    /// - When a validator is initially added (observed by the event
    ///   `BakerEvent::Added`), only `commission_rates` are available until the
    ///   next payday. The `payday_commission_rates` will be set at the next
    ///   payday.
    /// - When a validator is removed (observed by the event
    ///   `BakerEvent::Removed`), `commission_rates` are immediately cleared
    ///   from the bakers table upon detecting the `BakerEvent::Removed`,
    ///   whereas `payday_commission_rates` persist until the next payday.
    payday_commission_rates: Option<CommissionRates>,
    /// The lottery power of the baker pool during the last payday period
    /// captured from the `get_election_info` node endpoint.`
    payday_lottery_power: Decimal,
    /// Ranking of the bakers by lottery powers staring with rank 1 for the
    /// baker with the highest lottery power and ending with the rank
    /// `total` for the baker with the lowest lottery power.
    /// An account id that is not a baker has no
    /// `payday_ranking_by_lottery_powers` in general. A new baker has as
    /// well no `payday_ranking_by_lottery_powers` until the next payday.
    /// We return `None` as ranking in both cases.
    /// A baker that got just removed has a `payday_ranking_by_lottery_powers`
    /// until the next payday. We return a ranking in that case.
    rank: Option<Ranking>,
    /// The maximum amount that may be delegated to the pool, accounting for
    /// leverage and capital bounds.
    delegated_stake_cap: Amount,
    open_status: Option<BakerPoolOpenStatus>,
    metadata_url: Option<&'a str>,
    self_suspended: Option<i64>,
    inactive_suspended: Option<i64>,
    primed_for_suspension: Option<i64>,
}

#[Object]
impl<'a> BakerPool<'a> {
    async fn total_stake_percentage(&self) -> &Decimal { &self.total_stake_percentage }

    async fn total_stake(&self) -> Amount { self.total_stake }

    // Future improvement (API breaking changes): The front end queries
    // `ranking_by_total_stake` which was renamed and updated to
    // `ranking_by_lottery_power` in the backend. Migrate the name change to the
    // front-end and make this field not optional anymore.
    async fn ranking_by_total_stake(&self) -> Option<Ranking> { self.rank }

    async fn delegated_stake(&self) -> Amount { self.delegated_stake }

    async fn delegated_stake_cap(&self) -> Amount { self.delegated_stake_cap }

    async fn delegator_count(&self) -> i64 { self.delegator_count }

    async fn commission_rates(&self) -> &CommissionRates { &self.commission_rates }

    async fn payday_commission_rates(&self) -> &Option<CommissionRates> {
        &self.payday_commission_rates
    }

    async fn lottery_power(&self) -> &Decimal { &self.payday_lottery_power }

    async fn open_status(&self) -> Option<BakerPoolOpenStatus> { self.open_status }

    async fn metadata_url(&self) -> Option<&'a str> { self.metadata_url }

    async fn self_suspended(&self) -> Option<i64> { self.self_suspended }

    async fn inactive_suspended(&self) -> Option<i64> { self.inactive_suspended }

    async fn primed_for_suspension(&self) -> Option<i64> { self.primed_for_suspension }

    async fn pool_rewards(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<DescendingI64, PaydayPoolReward>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.pool_rewards_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            PaydayPoolReward,
            "SELECT * FROM (
                SELECT
                    payday_block_height as block_height,
                    slot_time,
                    pool_owner,
                    payday_total_transaction_rewards as total_transaction_rewards,
                    payday_delegators_transaction_rewards as delegators_transaction_rewards,
                    payday_total_baking_rewards as total_baking_rewards,
                    payday_delegators_baking_rewards as delegators_baking_rewards,
                    payday_total_finalization_rewards as total_finalization_rewards,
                    payday_delegators_finalization_rewards as delegators_finalization_rewards
                FROM bakers_payday_pool_rewards
                    JOIN blocks ON blocks.height = payday_block_height
                WHERE pool_owner_for_primary_key = $5 
                    AND payday_block_height > $2 AND payday_block_height < $1
                ORDER BY
                    (CASE WHEN $4 THEN payday_block_height END) ASC,
                    (CASE WHEN NOT $4 THEN payday_block_height END) DESC
                LIMIT $3
                ) AS rewards
            ORDER BY rewards.block_height DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last,
            self.id
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);
        while let Some(rewards) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(rewards.block_height.into(), rewards));
        }

        if let (Some(edge_min_index), Some(edge_max_index)) =
            (connection.edges.last(), connection.edges.first())
        {
            let result = sqlx::query!(
                "
                    SELECT 
                        MIN(payday_block_height) as min_index,
                        MAX(payday_block_height) as max_index
                    FROM bakers_payday_pool_rewards
                    WHERE pool_owner_for_primary_key = $1
                ",
                self.id
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.max_index.map_or(false, |db_max| db_max > edge_max_index.node.block_height);
            connection.has_next_page =
                result.min_index.map_or(false, |db_min| db_min < edge_min_index.node.block_height);
        }

        Ok(connection)
    }

    async fn delegators(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<DescendingI64, DelegationSummary>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.delegators_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            DelegationSummary,
            "SELECT * FROM (
                SELECT
                    index,
                    address as account_address,
                    delegated_restake_earnings as restake_earnings,
                    delegated_stake as staked_amount
                FROM accounts
                WHERE delegated_target_baker_id = $5 AND
                    accounts.index > $2 AND accounts.index < $1
                ORDER BY
                    (CASE WHEN $4 THEN accounts.index END) ASC,
                    (CASE WHEN NOT $4 THEN accounts.index END) DESC
                LIMIT $3
            ) AS delegators
            ORDER BY delegators.index DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last,
            self.id
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        while let Some(delegator) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(delegator.index.into(), delegator));
        }
        if let Some(page_max_index) = connection.edges.first() {
            if let Some(max_index) = sqlx::query_scalar!(
                "SELECT MAX(index) FROM accounts WHERE delegated_target_baker_id = $1",
                self.id
            )
            .fetch_one(pool)
            .await?
            {
                connection.has_previous_page = max_index > page_max_index.node.index;
            }
        }
        if let Some(edge) = connection.edges.last() {
            connection.has_next_page = edge.node.index != 0;
        }
        Ok(connection)
    }

    async fn apy(&self, ctx: &Context<'_>, period: ApyPeriod) -> ApiResult<PoolApy> {
        let pool = get_pool(ctx)?;
        let apy = match period {
            ApyPeriod::Last7Days => {
                sqlx::query_as!(
                    PoolApy,
                    "SELECT baker_apy, total_apy, delegators_apy
                     FROM latest_baker_apy_7_days
                     WHERE id = $1",
                    self.id
                )
                .fetch_optional(pool)
                .await?
            }
            ApyPeriod::Last30Days => {
                sqlx::query_as!(
                    PoolApy,
                    "SELECT baker_apy, total_apy, delegators_apy
                     FROM latest_baker_apy_30_days
                     WHERE id = $1",
                    self.id
                )
                .fetch_optional(pool)
                .await?
            }
        };
        Ok(apy.unwrap_or_default())
    }
}

#[derive(SimpleObject, Default)]
struct PoolApy {
    total_apy:      Option<f64>,
    baker_apy:      Option<f64>,
    delegators_apy: Option<f64>,
}

struct DelegatedStakeBounds {
    /// The leverage bound (also called leverage factor in the node API) is the
    /// maximum proportion of total stake of a baker (including the baker's
    /// own stake and the delegated stake to the baker) to the baker's own stake
    /// (excluding delegated stake to the baker) that a baker can achieve where
    /// the total stake of the baker is considered for calculating the
    /// lottery power or finalizer weight in the consensus (effective
    /// stake). Once this bound is passed, some of the baker's total stake
    /// no longer contribute to lottery power or finalizer weight in the
    /// consensus algorithm, meaning that part of the baker's total stake
    /// will no longer be considered as effective stake.
    ///
    /// The value is 1 or greater (1 <= leverage_bound).
    /// The value's numerator and denominator is stored here.
    ///
    /// The `leverage bound` ensures that each baker has skin in the game with
    /// respect to its delegators by providing some of the CCD staked from
    /// its own funds.
    leverage_bound_numerator:   i64,
    leverage_bound_denominator: i64,
    /// The capital bound is the maximum proportion of the total stake in the
    /// protocol (from all bakers including passive delegation) to the total
    /// stake of a baker (including the baker's own stake and the delegated
    /// stake to the baker) that a baker can achieve where the total
    /// stake of the baker is considered for calculating the lottery power or
    /// finalizer weight in the consensus (effective stake).
    /// Once this bound is passed, some of the baker's total stake no longer
    /// contribute to lottery power or finalizer weight in the consensus
    /// algorithm, meaning that part of the baker's total stake will no longer
    /// be considered as effective stake.
    ///
    /// The capital bound is always greater than 0 (capital_bound > 0). The
    /// value is stored as a fraction with precision of `1/100_000`. For
    /// example, a capital bound of 0.05 is stored as 5000.
    ///
    /// The `capital_bound` helps maintain network
    /// decentralization by preventing a single baker from gaining excessive
    /// power in the consensus protocol.
    capital_bound:              i64,
}

/// Ranking of the bakers by lottery powers from the last payday block staring
/// with rank 1 for the baker with the highest lottery power and ending with the
/// rank `total` for the baker with the lowest lottery power.
#[derive(SimpleObject, Clone, Copy)]
struct Ranking {
    rank:  i64,
    total: i64,
}

#[cfg(test)]
mod test {
    use super::BakerFieldDescCursor;
    use crate::connection::ConnectionBounds;
    use async_graphql::connection::CursorType;
    use std::cmp::Ordering;

    #[test]
    fn test_total_staked_desc_cursor_ordering() {
        {
            let cmp_start_end =
                BakerFieldDescCursor::START_BOUND.cmp(&BakerFieldDescCursor::END_BOUND);
            assert_eq!(cmp_start_end, Ordering::Less);
        }

        {
            let first = BakerFieldDescCursor {
                field:    10,
                baker_id: 3,
            };
            let second = BakerFieldDescCursor {
                field:    5,
                baker_id: 5,
            };
            assert!(first < second);
            assert_eq!(first.cmp(&second), Ordering::Less);
            assert_eq!(second.cmp(&first), Ordering::Greater);
            assert_eq!(first.cmp(&first), Ordering::Equal);
            assert_eq!(second.cmp(&second), Ordering::Equal);
        }

        {
            let second = BakerFieldDescCursor {
                field:    11000000000,
                baker_id: 149,
            };
            let first = BakerFieldDescCursor {
                field:    1000000000000000,
                baker_id: 3,
            };
            assert!(first < second);
            assert_eq!(second.cmp(&first), Ordering::Greater);
            assert_eq!(second.cmp(&second), Ordering::Equal);
            assert_eq!(first.cmp(&first), Ordering::Equal);
        }
    }

    #[test]
    fn test_total_staked_desc_cursor_encode_decode() {
        let cursor = BakerFieldDescCursor {
            field:    10,
            baker_id: 3,
        };
        let encode_decode = BakerFieldDescCursor::decode_cursor(cursor.encode_cursor().as_str())
            .expect("Failed decoding cursor");
        assert_eq!(cursor, encode_decode);
    }
}
