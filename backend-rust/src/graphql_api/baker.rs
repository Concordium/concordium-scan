use super::{
    account::Account, get_config, get_pool, todo_api, transaction::Transaction, ApiError,
    ApiResult, ConnectionQuery,
};
use crate::{
    scalar_types::{Amount, BakerId, DateTime, Decimal, MetadataUrl},
    transaction_event::{baker::BakerPoolOpenStatus, Event},
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use async_graphql::{connection, types, Context, Enum, InputObject, Object, SimpleObject, Union};
use concordium_rust_sdk::types::AmountFraction;
use futures::TryStreamExt;
use sqlx::PgPool;
use std::cmp::{max, min};

#[derive(Default)]
pub struct QueryBaker;

#[Object]
impl QueryBaker {
    async fn baker<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Baker> {
        let id = IdBaker::try_from(id)?.baker_id;
        Baker::query_by_id(get_pool(ctx)?, id).await
    }

    async fn baker_by_baker_id<'a>(
        &self,
        ctx: &Context<'a>,
        baker_id: BakerId,
    ) -> ApiResult<Baker> {
        Baker::query_by_id(get_pool(ctx)?, baker_id).await
    }

    #[allow(clippy::too_many_arguments)]
    async fn bakers(
        &self,
        #[graphql(default)] _sort: BakerSort,
        _filter: BakerFilterInput,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        todo_api!()
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

pub struct Baker {
    id: BakerId,
    staked: i64,
    restake_earnings: bool,
    open_status: Option<BakerPoolOpenStatus>,
    metadata_url: Option<MetadataUrl>,
    transaction_commission: Option<i64>,
    baking_commission: Option<i64>,
    finalization_commission: Option<i64>,
}
impl Baker {
    async fn query_by_id(pool: &PgPool, baker_id: BakerId) -> ApiResult<Self> {
        sqlx::query_as!(
            Baker,
            r#"SELECT
    id,
    staked,
    restake_earnings,
    open_status as "open_status: BakerPoolOpenStatus",
    metadata_url,
    transaction_commission,
    baking_commission,
    finalization_commission
 FROM bakers WHERE id=$1
"#,
            i64::from(baker_id)
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)
    }
}
#[Object]
impl Baker {
    async fn id(&self) -> types::ID { types::ID::from(self.id.to_string()) }

    async fn baker_id(&self) -> BakerId { self.id }

    async fn state<'a>(&'a self) -> ApiResult<BakerState<'a>> {
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

        let out = BakerState::ActiveBakerState(ActiveBakerState {
            staked_amount:    Amount::try_from(self.staked)?,
            restake_earnings: self.restake_earnings,
            pool:             BakerPool {
                open_status:      self.open_status,
                commission_rates: CommissionRates {
                    transaction_commission,
                    baking_commission,
                    finalization_commission,
                },
                metadata_url:     self.metadata_url.as_deref(),
            },
            pending_change:   None, // This is not used starting from P7.
        });
        Ok(out)
    }

    async fn account<'a>(&self, ctx: &Context<'a>) -> ApiResult<Account> {
        Account::query_by_index(get_pool(ctx)?, i64::from(self.id)).await?.ok_or(ApiError::NotFound)
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

        // Retrieves the transactions initated by a baker account. The transactions are
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
                WHERE transactions.sender_index = $5
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
            self.id.0
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
                    WHERE transactions.sender_index = $1
                ",
                &self.id.0
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

#[derive(Union)]
enum BakerState<'a> {
    ActiveBakerState(ActiveBakerState<'a>),
    RemovedBakerState(RemovedBakerState),
}

#[derive(SimpleObject)]
struct ActiveBakerState<'a> {
    // /// The status of the bakers node. Will be null if no status for the node
    // /// exists.
    // node_status:      NodeStatus,
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

#[derive(InputObject)]
struct BakerFilterInput {
    open_status_filter: BakerPoolOpenStatus,
    include_removed:    bool,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, Default)]
enum BakerSort {
    #[default]
    BakerIdAsc,
    BakerIdDesc,
    BakerStakedAmountAsc,
    BakerStakedAmountDesc,
    TotalStakedAmountAsc,
    TotalStakedAmountDesc,
    DelegatorCountAsc,
    DelegatorCountDesc,
    BakerApy30DaysDesc,
    DelegatorApy30DaysDesc,
    BlockCommissionsAsc,
    BlockCommissionsDesc,
}

#[derive(SimpleObject)]
struct BakerPool<'a> {
    // /// Total stake of the baker pool as a percentage of all CCDs in existence.
    // /// Value may be null for brand new bakers where statistics have not
    // /// been calculated yet. This should be rare and only a temporary
    // /// condition.
    // total_stake_percentage:  Decimal,
    // lottery_power:           Decimal,
    // payday_commission_rates: CommissionRates,
    open_status:      Option<BakerPoolOpenStatus>,
    commission_rates: CommissionRates,
    metadata_url:     Option<&'a str>,
    // /// The total amount staked by delegation to this baker pool.
    // delegated_stake:         Amount,
    // /// The maximum amount that may be delegated to the pool, accounting for
    // /// leverage and stake limits.
    // delegated_stake_cap:     Amount,
    // /// The total amount staked in this baker pool. Includes both baker stake
    // /// and delegated stake.
    // total_stake:             Amount,
    // delegator_count:         i32,
    // /// Ranking of the baker pool by total staked amount. Value may be null for
    // /// brand new bakers where statistics have not been calculated yet. This
    // /// should be rare and only a temporary condition.
    // ranking_by_total_stake:  Ranking,
    // TODO: apy(period: ApyPeriod!): PoolApy!
    // TODO: delegators("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): DelegatorsConnection
    // TODO: poolRewards("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): PaydayPoolRewardConnection
}

#[derive(SimpleObject)]
struct CommissionRates {
    transaction_commission:  Option<Decimal>,
    finalization_commission: Option<Decimal>,
    baking_commission:       Option<Decimal>,
}
