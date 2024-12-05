//! TODO
//! - Introduce default LIMITS for connections
//! - Introduce a MAX LIMIT for connections
//! - Enable GraphiQL through flag instead of always.

#![allow(unused_variables)]

mod account_metrics;
mod transaction_metrics;

// TODO remove this macro, when done with first iteration
/// Short hand for returning API error with the message not implemented.
macro_rules! todo_api {
    () => {
        Err(ApiError::InternalError(String::from("Not implemented")))
    };
}

use account_metrics::AccountMetricsQuery;
use anyhow::Context as _;
use async_graphql::{
    http::GraphiQLSource,
    types::{self, connection},
    ComplexObject, Context, EmptyMutation, Enum, InputObject, InputValueError, InputValueResult,
    Interface, MergedObject, Object, Scalar, ScalarType, Schema, SimpleObject, Subscription, Union,
    Value,
};
use async_graphql_axum::GraphQLSubscription;
use chrono::Duration;
use concordium_rust_sdk::{
    base::{
        contracts_common::{
            schema::{Type, VersionedModuleSchema, VersionedSchemaError},
            Cursor,
        },
        smart_contracts::ReceiveName,
    },
    id::types as sdk_types,
    types::AmountFraction,
};
use futures::prelude::*;
use prometheus_client::registry::Registry;
use sqlx::{postgres::types::PgInterval, PgPool};
use std::{error::Error, mem, str::FromStr, sync::Arc};
use tokio::{net::TcpListener, sync::broadcast};
use tokio_stream::wrappers::errors::BroadcastStreamRecvError;
use tokio_util::sync::CancellationToken;
use tower_http::cors::{CorsLayer, Any};
use tracing::error;
use transaction_metrics::TransactionMetricsQuery;

const VERSION: &str = clap::crate_version!();

struct SchemaName {
    type_name:  &'static str,
    value_name: &'static str,
}

enum SmartContractSchemaNames {
    Event,
    InputParameterReceiveFunction,
}

impl SmartContractSchemaNames {
    pub const EVENT: SchemaName = SchemaName {
        type_name:  "event",
        value_name: "contract log",
    };
    pub const INPUT_PARAMETER_RECEIVE_FUNCTION: SchemaName = SchemaName {
        type_name:  "receive parameter",
        value_name: "input parameter of receive function",
    };

    pub fn value(&self) -> &'static str {
        match self {
            SmartContractSchemaNames::Event => Self::EVENT.value_name,
            SmartContractSchemaNames::InputParameterReceiveFunction => {
                Self::INPUT_PARAMETER_RECEIVE_FUNCTION.value_name
            }
        }
    }

    pub fn kind(&self) -> &'static str {
        match self {
            SmartContractSchemaNames::Event => Self::EVENT.type_name,
            SmartContractSchemaNames::InputParameterReceiveFunction => {
                Self::INPUT_PARAMETER_RECEIVE_FUNCTION.type_name
            }
        }
    }
}

#[derive(clap::Args)]
pub struct ApiServiceConfig {
    /// Account(s) that should not be considered in circulation.
    #[arg(long, env = "CCDSCAN_API_CONFIG_NON_CIRCULATING_ACCOUNTS", value_delimiter = ',')]
    non_circulating_account:            Vec<sdk_types::AccountAddress>,
    /// The most transactions which can be queried at once.
    #[arg(long, env = "CCDSCAN_API_CONFIG_TRANSACTION_CONNECTION_LIMIT", default_value = "100")]
    transaction_connection_limit:       u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_BLOCK_CONNECTION_LIMIT", default_value = "100")]
    block_connection_limit:             u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_ACCOUNT_CONNECTION_LIMIT", default_value = "100")]
    account_connection_limit:           u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_CONTRACT_CONNECTION_LIMIT", default_value = "100")]
    contract_connection_limit:          u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_CONTRACT_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    contract_events_collection_limit:   u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_TRANSACTION_EVENT_CONNECTION_LIMIT",
        default_value = "100"
    )]
    transaction_event_connection_limit: u64,
}

#[derive(MergedObject, Default)]
pub struct Query(BaseQuery, AccountMetricsQuery, TransactionMetricsQuery);

pub struct Service {
    pub schema: Schema<Query, EmptyMutation, Subscription>,
}
impl Service {
    pub fn new(
        subscription: Subscription,
        registry: &mut Registry,
        pool: PgPool,
        config: ApiServiceConfig,
    ) -> Self {
        let schema = Schema::build(Query::default(), EmptyMutation, subscription)
            .extension(async_graphql::extensions::Tracing)
            .extension(monitor::MonitorExtension::new(registry))
            .data(pool)
            .data(config)
            .finish();
        Self {
            schema,
        }
    }

    pub async fn serve(
        self,
        tcp_listener: TcpListener,
        stop_signal: CancellationToken,
    ) -> anyhow::Result<()> {
        let cors_layer = CorsLayer::new()
            .allow_origin(Any)  // Open access to selected route
            .allow_methods(Any)
            .allow_headers(Any);
        let app = axum::Router::new()
            .route("/", axum::routing::get(Self::graphiql))
            .route(
                "/api/graphql",
                axum::routing::post_service(async_graphql_axum::GraphQL::new(self.schema.clone())),
            )
            .route_service("/ws/graphql", GraphQLSubscription::new(self.schema))
            .layer(cors_layer);

        axum::serve(tcp_listener, app)
            .with_graceful_shutdown(stop_signal.cancelled_owned())
            .await?;
        Ok(())
    }

    async fn graphiql() -> impl axum::response::IntoResponse {
        axum::response::Html(
            GraphiQLSource::build()
                .endpoint("/api/graphql")
                .subscription_endpoint("/ws/graphql")
                .finish(),
        )
    }
}
/// Module containing types and logic for building an async_graphql extension
/// which allows for monitoring of the service.
mod monitor {
    use async_graphql::async_trait::async_trait;
    use futures::prelude::*;
    use prometheus_client::{
        encoding::EncodeLabelSet,
        metrics::{
            counter::Counter,
            family::Family,
            gauge::Gauge,
            histogram::{self, Histogram},
        },
        registry::Registry,
    };
    use std::sync::Arc;
    use tokio::time::Instant;

    /// Type representing the Prometheus labels used for metrics related to
    /// queries.
    #[derive(Debug, Clone, EncodeLabelSet, PartialEq, Eq, Hash)]
    struct QueryLabels {
        /// Identifier of the top level query.
        query: String,
    }
    /// Extension for async_graphql adding monitoring.
    #[derive(Clone)]
    pub struct MonitorExtension {
        /// Metric for tracking current number of requests in-flight.
        in_flight_requests:   Family<QueryLabels, Gauge>,
        /// Metric for counting total number of requests.
        total_requests:       Family<QueryLabels, Counter>,
        /// Metric for collecting execution duration for requests.
        request_duration:     Family<QueryLabels, Histogram>,
        /// Metric tracking current open subscriptions.
        active_subscriptions: Gauge,
    }
    impl MonitorExtension {
        pub fn new(registry: &mut Registry) -> Self {
            let in_flight_requests: Family<QueryLabels, Gauge> = Default::default();
            registry.register(
                "in_flight_queries",
                "Current number of queries in-flight",
                in_flight_requests.clone(),
            );
            let total_requests: Family<QueryLabels, Counter> = Default::default();
            registry.register(
                "requests",
                "Total number of requests received",
                total_requests.clone(),
            );
            let request_duration: Family<QueryLabels, Histogram> =
                Family::new_with_constructor(|| {
                    Histogram::new(histogram::exponential_buckets(0.010, 2.0, 10))
                });
            registry.register(
                "request_duration_seconds",
                "Duration of seconds used to fetch all of the block information",
                request_duration.clone(),
            );
            let active_subscriptions: Gauge = Default::default();
            registry.register(
                "active_subscription",
                "Current number of active subscriptions",
                active_subscriptions.clone(),
            );
            MonitorExtension {
                in_flight_requests,
                total_requests,
                request_duration,
                active_subscriptions,
            }
        }
    }
    impl async_graphql::extensions::ExtensionFactory for MonitorExtension {
        fn create(&self) -> Arc<dyn async_graphql::extensions::Extension> { Arc::new(self.clone()) }
    }
    #[async_trait]
    impl async_graphql::extensions::Extension for MonitorExtension {
        async fn execute(
            &self,
            ctx: &async_graphql::extensions::ExtensionContext<'_>,
            operation_name: Option<&str>,
            next: async_graphql::extensions::NextExecute<'_>,
        ) -> async_graphql::Response {
            let label = QueryLabels {
                query: operation_name.unwrap_or("<none>").to_owned(),
            };
            self.in_flight_requests.get_or_create(&label).inc();
            self.total_requests.get_or_create(&label).inc();
            let start = Instant::now();
            let response = next.run(ctx, operation_name).await;
            let duration = start.elapsed();
            self.request_duration.get_or_create(&label).observe(duration.as_secs_f64());
            self.in_flight_requests.get_or_create(&label).dec();
            response
        }

        /// Called at subscribe request.
        fn subscribe<'s>(
            &self,
            ctx: &async_graphql::extensions::ExtensionContext<'_>,
            stream: stream::BoxStream<'s, async_graphql::Response>,
            next: async_graphql::extensions::NextSubscribe<'_>,
        ) -> stream::BoxStream<'s, async_graphql::Response> {
            let stream = next.run(ctx, stream);
            let wrapped_stream = WrappedStream::new(stream, self.active_subscriptions.clone());
            wrapped_stream.boxed()
        }
    }
    /// Wrapper around a stream to update metrics when it gets dropped.
    struct WrappedStream<'s> {
        inner:                stream::BoxStream<'s, async_graphql::Response>,
        active_subscriptions: Gauge,
    }
    impl<'s> WrappedStream<'s> {
        fn new(
            stream: stream::BoxStream<'s, async_graphql::Response>,
            active_subscriptions: Gauge,
        ) -> Self {
            active_subscriptions.inc();
            Self {
                inner: stream,
                active_subscriptions,
            }
        }
    }
    impl<'a> futures::stream::Stream for WrappedStream<'a> {
        type Item = async_graphql::Response;

        fn poll_next(
            mut self: std::pin::Pin<&mut Self>,
            cx: &mut std::task::Context<'_>,
        ) -> std::task::Poll<Option<Self::Item>> {
            self.inner.poll_next_unpin(cx)
        }
    }
    impl<'a> std::ops::Drop for WrappedStream<'a> {
        fn drop(&mut self) { self.active_subscriptions.dec(); }
    }
}

/// All the errors that may be produced by the GraphQL API.
///
/// Note that `async_graphql` requires this to be `Clone`, as it is used as a
/// return type in queries. However, some of the underlying error types are not
/// `Clone`, so we wrap those in `Arc`s to make them `Clone`.
#[derive(Debug, thiserror::Error, Clone)]
enum ApiError {
    #[error("Could not find resource")]
    NotFound,
    #[error("Internal error: {}", .0.message)]
    NoDatabasePool(async_graphql::Error),
    #[error("Internal error: {}", .0.message)]
    NoServiceConfig(async_graphql::Error),
    #[error("Internal error: {0}")]
    FailedDatabaseQuery(Arc<sqlx::Error>),
    #[error("Invalid ID format: {0}")]
    InvalidIdInt(std::num::ParseIntError),
    #[error("The period cannot be converted")]
    DurationOutOfRange(Arc<Box<dyn Error + Send + Sync>>),
    #[error("The \"first\" and \"last\" parameters cannot exist at the same time")]
    QueryConnectionFirstLast,
    #[error("Internal error: {0}")]
    InternalError(String),
    #[error("Invalid integer: {0}")]
    InvalidInt(#[from] std::num::TryFromIntError),
    #[error("Invalid integer: {0}")]
    InvalidIntString(#[from] std::num::ParseIntError),
    #[error("Parse error: {0}")]
    InvalidContractVersion(#[from] InvalidContractVersionError),
    #[error("Schema in database should be a valid versioned module schema")]
    InvalidVersionedModuleSchema(#[from] VersionedSchemaError),
}

impl From<sqlx::Error> for ApiError {
    fn from(value: sqlx::Error) -> Self { ApiError::FailedDatabaseQuery(Arc::new(value)) }
}

type ApiResult<A> = Result<A, ApiError>;

/// Get the database pool from the context.
fn get_pool<'a>(ctx: &Context<'a>) -> ApiResult<&'a PgPool> {
    ctx.data::<PgPool>().map_err(ApiError::NoDatabasePool)
}

/// Get service configuration from the context.
fn get_config<'a>(ctx: &Context<'a>) -> ApiResult<&'a ApiServiceConfig> {
    ctx.data::<ApiServiceConfig>().map_err(ApiError::NoServiceConfig)
}

trait ConnectionCursor {
    const MIN: Self;
    const MAX: Self;
}
impl ConnectionCursor for i64 {
    const MAX: i64 = i64::MAX;
    const MIN: i64 = i64::MIN;
}
impl ConnectionCursor for usize {
    const MAX: usize = usize::MAX;
    const MIN: usize = usize::MIN;
}

struct ConnectionQuery<A> {
    from:  A,
    to:    A,
    limit: i64,
    desc:  bool,
}
impl<A> ConnectionQuery<A> {
    fn new<E>(
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        connection_limit: u64,
    ) -> ApiResult<Self>
    where
        A: std::str::FromStr<Err = E> + ConnectionCursor,
        E: Into<ApiError>, {
        if first.is_some() && last.is_some() {
            return Err(ApiError::QueryConnectionFirstLast);
        }

        let from = if let Some(a) = after {
            a.parse::<A>().map_err(|e| e.into())?
        } else {
            A::MIN
        };

        let to = if let Some(b) = before {
            b.parse::<A>().map_err(|e| e.into())?
        } else {
            A::MAX
        };

        let limit =
            first.or(last).map_or(connection_limit, |limit| connection_limit.min(limit)) as i64;

        Ok(Self {
            from,
            to,
            limit,
            desc: last.is_some(),
        })
    }
}

#[derive(Default)]
pub struct BaseQuery;

#[Object]
#[allow(clippy::too_many_arguments)]
impl BaseQuery {
    async fn versions(&self) -> Versions {
        Versions {
            backend_versions: VERSION.to_string(),
        }
    }

    async fn block<'a>(&self, ctx: &Context<'a>, height_id: types::ID) -> ApiResult<Block> {
        let height: BlockHeight = height_id.try_into().map_err(ApiError::InvalidIdInt)?;
        Block::query_by_height(get_pool(ctx)?, height).await
    }

    async fn block_by_block_hash<'a>(
        &self,
        ctx: &Context<'a>,
        block_hash: BlockHash,
    ) -> ApiResult<Block> {
        Block::query_by_hash(get_pool(ctx)?, block_hash).await
    }

    async fn blocks<'a>(
        &self,
        ctx: &Context<'a>,
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
        // The CCDScan front-end currently expects an ASC order of the nodes/edges
        // returned (outer `ORDER BY`), while the inner `ORDER BY` is a trick to
        // get the correct nodes/edges selected based on the `after/before` key
        // specified.
        let mut row_stream = sqlx::query_as!(
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
                    (CASE WHEN $4 THEN height END) DESC,
                    (CASE WHEN NOT $4 THEN height END) ASC
                LIMIT $3
            ) ORDER BY height ASC",
            query.from,
            query.to,
            query.limit,
            query.desc
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(true, true);
        while let Some(block) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(block.height.to_string(), block));
        }
        if last.is_some() {
            if let Some(edge) = connection.edges.last() {
                connection.has_previous_page = edge.node.height != 0;
            }
        } else if let Some(edge) = connection.edges.first() {
            connection.has_previous_page = edge.node.height != 0;
        }

        Ok(connection)
    }

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
        // returned (outer `ORDER BY`), while the inner `ORDER BY` is a trick to
        // get the correct nodes/edges selected based on the `after/before` key
        // specified.
        let mut row_stream = sqlx::query_as!(
            Transaction,
            r#"SELECT * FROM (
                SELECT
                    index,
                    block_height,
                    hash,
                    ccd_cost,
                    energy_cost,
                    sender,
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

        // TODO Update page prev/next
        let mut connection = connection::Connection::new(true, true);

        while let Some(row) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(row.index.to_string(), row));
        }

        Ok(connection)
    }

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

        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.account_connection_limit,
        )?;

        // The CCDScan front-end currently expects an ASC order of the nodes/edges
        // returned (outer `ORDER BY`), while the inner `ORDER BY` is a trick to
        // get the correct nodes/edges selected based on the `after/before` key
        // specified.
        let mut accounts = sqlx::query_as!(
            Account,
            r"SELECT * FROM (
                SELECT
                    index,
                    transaction_index,
                    address,
                    amount,
                    delegated_stake,
                    num_txs
                FROM accounts
                WHERE
                    -- Filter for only the accounts that are within the
                    -- range that correspond to the requested page.
                    -- The first condition is true only if we don't order by that field.
                    -- Then the whole OR condition will be true, so the filter for that
                    -- field will be ignored.
                    (NOT $3 OR index           > $1 AND index           < $2) AND
                    (NOT $4 OR amount          > $1 AND amount          < $2) AND
                    (NOT $5 OR num_txs         > $1 AND num_txs         < $2) AND
                    (NOT $6 OR delegated_stake > $1 AND delegated_stake < $2) AND
                    -- Need to filter for only delegators if the user requests this.
                    (NOT $7 OR delegated_stake > 0)
                ORDER BY
                    -- Order by the field requested, and by desc/asc as appropriate.
                    -- The first condition is true if we order by that field.
                    -- Otherwise false, which makes the CASE null, which means
                    -- it will not affect the ordering at all.
                    (CASE WHEN $3 AND $8     THEN index           END) DESC,
                    (CASE WHEN $3 AND NOT $8 THEN index           END) ASC,
                    (CASE WHEN $4 AND $8     THEN amount          END) DESC,
                    (CASE WHEN $4 AND NOT $8 THEN amount          END) ASC,
                    (CASE WHEN $5 AND $8     THEN num_txs         END) DESC,
                    (CASE WHEN $5 AND NOT $8 THEN num_txs         END) ASC,
                    (CASE WHEN $6 AND $8     THEN delegated_stake END) DESC,
                    (CASE WHEN $6 AND NOT $8 THEN delegated_stake END) ASC
                LIMIT $9
            )
            -- We need to order each page ASC still, we only use the DESC/ASC ordering above
            -- to select page items from the start/end of the range.
            -- Each page must still independently be ordered ascending.
            -- See also https://relay.dev/graphql/connections.htm#sec-Edge-order
            ORDER BY CASE
                WHEN $3 THEN index
                WHEN $4 THEN amount
                WHEN $5 THEN num_txs
                WHEN $6 THEN delegated_stake
            END ASC",
            query.from,
            query.to,
            matches!(order.field, AccountOrderField::Age),
            matches!(order.field, AccountOrderField::Amount),
            matches!(order.field, AccountOrderField::TransactionCount),
            matches!(order.field, AccountOrderField::DelegatedStake),
            filter.map(|f| f.is_delegator).unwrap_or_default(),
            matches!(order.dir, OrderDir::Desc),
            query.limit,
        )
        .fetch(pool);

        // TODO Update page prev/next
        let mut connection = connection::Connection::new(true, true);

        while let Some(account) = accounts.try_next().await? {
            connection.edges.push(connection::Edge::new(order.cursor(&account), account));
        }

        Ok(connection)
    }

    async fn baker<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Baker> {
        let id = IdBaker::try_from(id)?.baker_id;
        Baker::query_by_id(get_pool(ctx)?, id).await
    }

    async fn baker_by_baker_id<'a>(&self, ctx: &Context<'a>, id: BakerId) -> ApiResult<Baker> {
        Baker::query_by_id(get_pool(ctx)?, id).await
    }

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

    async fn search(&self, query: String) -> SearchResult {
        SearchResult {
            _query: query,
        }
    }

    async fn block_metrics<'a>(
        &self,
        ctx: &Context<'a>,
        period: MetricsPeriod,
    ) -> ApiResult<BlockMetrics> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let non_circulating_accounts =
            config.non_circulating_account.iter().map(|a| a.to_string()).collect::<Vec<_>>();

        let latest_block = sqlx::query!(
            r#"
WITH non_circulating_accounts AS (
    SELECT
        COALESCE(SUM(amount), 0)::BIGINT AS total_amount
    FROM accounts
    WHERE address=ANY($1)
)
SELECT
    height,
    blocks.total_amount,
    total_staked,
    (blocks.total_amount - non_circulating_accounts.total_amount)::BIGINT AS total_amount_released
FROM blocks, non_circulating_accounts
ORDER BY height DESC
LIMIT 1"#,
            non_circulating_accounts.as_slice()
        )
        .fetch_one(pool)
        .await?;

        let interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;
        let period_query = sqlx::query!(
            r#"
SELECT
    COUNT(*) as blocks_added,
    AVG(block_time)::integer as avg_block_time,
    AVG(finalization_time)::integer as avg_finalization_time
FROM blocks
WHERE slot_time > (LOCALTIMESTAMP - $1::interval)"#,
            interval
        )
        .fetch_one(pool)
        .await?;

        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;
        let bucket_query = sqlx::query!(
            "
WITH data AS (
  SELECT
    date_bin($1::interval, slot_time, TIMESTAMP '2001-01-01') as time,
    block_time,
    finalization_time,
    LAST_VALUE(total_staked) OVER (
      PARTITION BY date_bin($1::interval, slot_time, TIMESTAMP '2001-01-01')
      ORDER BY height ASC
    ) as total_staked
  FROM blocks
  ORDER BY height
)
SELECT
  time,
  COUNT(*) as y_blocks_added,
  AVG(block_time)::integer as y_block_time_avg,
  AVG(finalization_time)::integer as y_finalization_time_avg,
  MAX(total_staked) as y_last_total_micro_ccd_staked
FROM data
GROUP BY time
LIMIT 30", // WHERE slot_time > (LOCALTIMESTAMP - $1::interval)
            bucket_interval
        )
        .fetch_all(pool)
        .await?;

        let mut buckets = BlockMetricsBuckets {
            bucket_width: bucket_width.into(),
            x_time: Vec::new(),
            y_blocks_added: Vec::new(),
            y_block_time_avg: Vec::new(),
            y_finalization_time_avg: Vec::new(),
            y_last_total_micro_ccd_staked: Vec::new(),
        };
        for row in bucket_query {
            buckets.x_time.push(row.time.ok_or(ApiError::InternalError(
                "Unexpected missing time for bucket".to_string(),
            ))?);
            buckets.y_blocks_added.push(row.y_blocks_added.unwrap_or(0));
            let y_block_time_avg = row.y_block_time_avg.unwrap_or(0) as f64 / 1000.0;
            buckets.y_block_time_avg.push(y_block_time_avg);
            let y_finalization_time_avg = row.y_finalization_time_avg.unwrap_or(0) as f64 / 1000.0;
            buckets.y_finalization_time_avg.push(y_finalization_time_avg);
            buckets
                .y_last_total_micro_ccd_staked
                .push(row.y_last_total_micro_ccd_staked.unwrap_or(0));
        }

        Ok(BlockMetrics {
            blocks_added: period_query.blocks_added.unwrap_or(0),
            avg_block_time: period_query.avg_block_time.map(|i| i as f64 / 1000.0),
            avg_finalization_time: period_query.avg_finalization_time.map(|i| i as f64 / 1000.0),
            last_block_height: latest_block.height,
            last_total_micro_ccd: latest_block.total_amount,
            last_total_micro_ccd_staked: latest_block.total_staked,
            last_total_micro_ccd_released: latest_block.total_amount_released.unwrap_or(0),
            last_total_micro_ccd_unlocked: None, // TODO implement unlocking schedule
            // TODO check what format this is expected to be in.
            buckets,
        })
    }

    // bakerMetrics(period: MetricsPeriod!): BakerMetrics!
    // rewardMetrics(period: MetricsPeriod!): RewardMetrics!
    // rewardMetricsForAccount(accountId: ID! period: MetricsPeriod!):
    // RewardMetrics! poolRewardMetricsForPassiveDelegation(period:
    // MetricsPeriod!): PoolRewardMetrics!
    // poolRewardMetricsForBakerPool(bakerId: ID! period: MetricsPeriod!):
    // PoolRewardMetrics! passiveDelegation: PassiveDelegation
    // paydayStatus: PaydayStatus
    // latestChainParameters: ChainParameters
    // importState: ImportState
    // nodeStatuses(sortField: NodeSortField! sortDirection: NodeSortDirection!
    // "Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String
    // "Returns the last _n_ elements from the list." last: Int "Returns the
    // elements in the list that come before the specified cursor." before: String):
    // NodeStatusesConnection nodeStatus(id: ID!): NodeStatus
    // tokens("Returns the first _n_ elements from the list." first: Int "Returns
    // the elements in the list that come after the specified cursor." after:
    // String "Returns the last _n_ elements from the list." last: Int "Returns
    // the elements in the list that come before the specified cursor." before:
    // String): TokensConnection token(contractIndex: UnsignedLong!
    // contractSubIndex: UnsignedLong! tokenId: String!): Token!

    async fn contract<'a>(
        &self,
        ctx: &Context<'a>,
        contract_address_index: ContractIndex,
        contract_address_sub_index: ContractIndex,
    ) -> ApiResult<Contract> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            r#"SELECT
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
            JOIN accounts ON transactions.sender = accounts.index
            WHERE contracts.index = $1 AND contracts.sub_index = $2"#,
            contract_address_index.0 as i64,
            contract_address_sub_index.0 as i64,
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        let snapshot = ContractSnapshot {
            block_height: row.block_height,
            contract_address_index,
            contract_address_sub_index,
            contract_name: row.contract_name,
            module_reference: row.module_reference,
            amount: row.amount,
        };

        Ok(Contract {
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
        })
    }

    async fn contracts<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Contract>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.contract_connection_limit,
        )?;

        // The CCDScan front-end currently expects an ASC order of the nodes/edges
        // returned (outer `ORDER BY`), while the inner `ORDER BY` is a trick to
        // get the correct nodes/edges selected based on the `after/before` key
        // specified.
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
                JOIN accounts ON transactions.sender = accounts.index
                WHERE contracts.index > $1 AND contracts.index < $2
                ORDER BY
                    (CASE WHEN $4 THEN contracts.index END) DESC,
                    (CASE WHEN NOT $4 THEN contracts.index END) ASC
                LIMIT $3
            ) AS contract_data
            ORDER BY contract_data.index ASC",
            query.from,
            query.to,
            query.limit,
            query.desc
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(true, true);

        while let Some(row) = row_stream.try_next().await? {
            let contract_address_index =
                row.index.try_into().map_err(|e: <u64 as TryFrom<i64>>::Error| {
                    ApiError::InternalError(e.to_string())
                })?;
            let contract_address_sub_index =
                row.sub_index.try_into().map_err(|e: <u64 as TryFrom<i64>>::Error| {
                    ApiError::InternalError(e.to_string())
                })?;

            let snapshot = ContractSnapshot {
                block_height: row.block_height,
                contract_address_index,
                contract_address_sub_index,
                contract_name: row.contract_name,
                module_reference: row.module_reference,
                amount: row.amount,
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
            connection
                .edges
                .push(connection::Edge::new(contract.contract_address_index.to_string(), contract));
        }

        if last.is_some() {
            if let Some(edge) = connection.edges.last() {
                connection.has_previous_page = edge.node.contract_address_index.0 != 0;
            }
        } else if let Some(edge) = connection.edges.first() {
            connection.has_previous_page = edge.node.contract_address_index.0 != 0;
        }

        Ok(connection)
    }

    async fn module_reference_event<'a>(
        &self,
        ctx: &Context<'a>,
        module_reference: String,
    ) -> ApiResult<ModuleReferenceEvent> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            r#"SELECT
                blocks.height as block_height,
                smart_contract_modules.transaction_index,
                schema as display_schema,
                blocks.slot_time as block_slot_time,
                transactions.hash as transaction_hash,
                accounts.address as sender
            FROM smart_contract_modules
            JOIN transactions ON smart_contract_modules.transaction_index = transactions.index
            JOIN blocks ON transactions.block_height = blocks.height
            JOIN accounts ON transactions.sender = accounts.index
            WHERE module_reference = $1"#,
            module_reference
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        let display_schema = row
            .display_schema
            .as_ref()
            .map(|s| VersionedModuleSchema::new(s, &None).map(|schema| schema.to_string()))
            .transpose()?;

        Ok(ModuleReferenceEvent {
            module_reference,
            sender: row.sender.into(),
            block_height: row.block_height,
            transaction_hash: row.transaction_hash,
            block_slot_time: row.block_slot_time,
            display_schema,
        })
    }
}

pub struct Subscription {
    pub block_added:      broadcast::Receiver<Block>,
    pub accounts_updated: broadcast::Receiver<AccountsUpdatedSubscriptionItem>,
}

impl Subscription {
    pub fn new() -> (Self, SubscriptionContext) {
        let (block_added_sender, block_added) = broadcast::channel(100);
        let (accounts_updated_sender, accounts_updated) = broadcast::channel(100);
        (
            Subscription {
                block_added,
                accounts_updated,
            },
            SubscriptionContext {
                block_added_sender,
                accounts_updated_sender,
            },
        )
    }
}

#[Subscription]
impl Subscription {
    async fn block_added(&self) -> impl Stream<Item = Result<Block, BroadcastStreamRecvError>> {
        tokio_stream::wrappers::BroadcastStream::new(self.block_added.resubscribe())
    }

    async fn accounts_updated(
        &self,
        account_address: Option<String>,
    ) -> impl Stream<Item = Result<AccountsUpdatedSubscriptionItem, BroadcastStreamRecvError>> {
        let stream =
            tokio_stream::wrappers::BroadcastStream::new(self.accounts_updated.resubscribe());

        // Apply filtering based on `account_address`.
        stream.filter_map(
            move |item: Result<AccountsUpdatedSubscriptionItem, BroadcastStreamRecvError>| {
                let address_filter = account_address.clone();
                async move {
                    match item {
                        Ok(notification) => {
                            if let Some(filter) = address_filter {
                                if notification.address == filter {
                                    // Pass on notification.
                                    Some(Ok(notification))
                                } else {
                                    // Skip if filter does not match.
                                    None
                                }
                            } else {
                                // Pass on all notification if no filter is set.
                                Some(Ok(notification))
                            }
                        }
                        // Pass on errors.
                        Err(e) => Some(Err(e)),
                    }
                }
            },
        )
    }
}

pub struct SubscriptionContext {
    block_added_sender:      broadcast::Sender<Block>,
    accounts_updated_sender: broadcast::Sender<AccountsUpdatedSubscriptionItem>,
}

impl SubscriptionContext {
    const ACCOUNTS_UPDATED_CHANNEL: &'static str = "account_updated";
    const BLOCK_ADDED_CHANNEL: &'static str = "block_added";

    pub async fn listen(self, pool: PgPool, stop_signal: CancellationToken) -> anyhow::Result<()> {
        let mut listener = sqlx::postgres::PgListener::connect_with(&pool)
            .await
            .context("Failed to create a postgreSQL listener")?;

        listener
            .listen_all([Self::BLOCK_ADDED_CHANNEL, Self::ACCOUNTS_UPDATED_CHANNEL])
            .await
            .context("Failed to listen to postgreSQL notifications")?;

        let exit = stop_signal
            .run_until_cancelled(async move {
                loop {
                    let notification = listener.recv().await?;
                    match notification.channel() {
                        Self::BLOCK_ADDED_CHANNEL => {
                            let block_height = BlockHeight::from_str(notification.payload())
                                .context("Failed to parse payload of block added")?;
                            let block = Block::query_by_height(&pool, block_height).await?;
                            self.block_added_sender.send(block)?;
                        }

                        Self::ACCOUNTS_UPDATED_CHANNEL => {
                            self.accounts_updated_sender.send(AccountsUpdatedSubscriptionItem {
                                address: notification.payload().to_string(),
                            })?;
                        }

                        unknown => {
                            anyhow::bail!("Received notification on unknown channel: {unknown}");
                        }
                    }
                }
            })
            .await;

        if let Some(result) = exit {
            result.context("Failed listening")?;
        }

        Ok(())
    }
}

#[derive(Clone, Debug, SimpleObject)]
pub struct AccountsUpdatedSubscriptionItem {
    address: String,
}

/// The UnsignedLong scalar type represents a unsigned 64-bit numeric
/// non-fractional value greater than or equal to 0.
#[derive(
    Clone,
    Copy,
    derive_more::Display,
    Debug,
    serde::Serialize,
    serde::Deserialize,
    derive_more::From,
    derive_more::FromStr,
)]
#[repr(transparent)]
#[serde(transparent)]
struct UnsignedLong(u64);
#[Scalar]
impl ScalarType for UnsignedLong {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_u64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value { Value::Number(self.0.into()) }
}

impl TryFrom<i64> for UnsignedLong {
    type Error = <u64 as TryFrom<i64>>::Error;

    fn try_from(number: i64) -> Result<Self, Self::Error> { Ok(UnsignedLong(number.try_into()?)) }
}

/// The `Long` scalar type represents non-fractional signed whole 64-bit numeric
/// values. Long can represent values between -(2^63) and 2^63 - 1.
#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
struct Long(i64);
#[Scalar]
impl ScalarType for Long {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_i64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value { Value::Number(self.0.into()) }
}
#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
struct Byte(u8);
#[Scalar]
impl ScalarType for Byte {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        let Some(v) = number.as_u64() else {
            return Err(InputValueError::expected_type(value));
        };

        if let Ok(v) = u8::try_from(v) {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value { Value::Number(self.0.into()) }
}

#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
struct Decimal(rust_decimal::Decimal);
#[Scalar]
impl ScalarType for Decimal {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::String(string) = value else {
            return Err(InputValueError::expected_type(value));
        };
        Ok(Self(rust_decimal::Decimal::from_str(string.as_str())?))
    }

    fn to_value(&self) -> Value { Value::String(self.0.to_string()) }
}

impl From<concordium_rust_sdk::types::AmountFraction> for Decimal {
    fn from(fraction: concordium_rust_sdk::types::AmountFraction) -> Self {
        Self(concordium_rust_sdk::types::PartsPerHundredThousands::from(fraction).into())
    }
}

/// The `TimeSpan` scalar represents an ISO-8601 compliant duration type.
#[derive(serde::Serialize, serde::Deserialize, Clone)]
#[repr(transparent)]
#[serde(try_from = "String", into = "String")]
struct TimeSpan(Duration);
#[Scalar]
impl ScalarType for TimeSpan {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::String(string) = value else {
            return Err(InputValueError::expected_type(value));
        };
        Ok(Self::try_from(string)?)
    }

    fn to_value(&self) -> Value { Value::String(self.0.to_string()) }
}
impl TryFrom<String> for TimeSpan {
    type Error = anyhow::Error;

    fn try_from(value: String) -> Result<Self, Self::Error> {
        let duration = iso8601_duration::Duration::from_str(value.as_str())
            .map_err(|err| anyhow::anyhow!("Invalid duration, expected ISO-8601"))?;
        Ok(Self(duration.to_chrono().context("Failed to construct duration")?))
    }
}
impl From<TimeSpan> for String {
    fn from(time: TimeSpan) -> Self { time.0.to_string() }
}
impl From<Duration> for TimeSpan {
    fn from(duration: Duration) -> Self { TimeSpan(duration) }
}

type BlockHeight = i64;
type BlockHash = String;
type TransactionHash = String;
type BakerId = i64;
type AccountIndex = i64;
type Amount = i64; // TODO: should be UnsignedLong in graphQL
type Energy = i64; // TODO: should be UnsignedLong in graphQL
type DateTime = chrono::DateTime<chrono::Utc>; // TODO check format matches.
type ContractIndex = UnsignedLong; // TODO check format.
type BigInteger = u64; // TODO check format.
type MetadataUrl = String;

#[derive(SimpleObject)]
struct Versions {
    backend_versions: String,
}

#[derive(Debug, Clone, sqlx::FromRow)]
pub struct Block {
    hash:              BlockHash,
    height:            BlockHeight,
    /// Time of the block being baked.
    slot_time:         DateTime,
    /// Number of milliseconds between the `slot_time` of this block and its
    /// parent.
    block_time:        i32,
    /// If this block is finalized, the number of milliseconds between the
    /// `slot_time` of this block and the first block that contains a
    /// finalization proof or quorum certificate that justifies this block
    /// being finalized.
    finalization_time: Option<i32>,
    // finalized_by:      Option<BlockHeight>,
    baker_id:          Option<BakerId>,
    total_amount:      Amount,
    // total_staked:      Amount,
}

impl Block {
    async fn query_by_height(pool: &PgPool, height: BlockHeight) -> ApiResult<Self> {
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

    async fn query_by_hash(pool: &PgPool, block_hash: BlockHash) -> ApiResult<Self> {
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
    // block_statistics: BlockStatistics,

    /// Absolute block height.
    async fn id(&self) -> types::ID { types::ID::from(self.height) }

    async fn block_hash(&self) -> &BlockHash { &self.hash }

    async fn block_height(&self) -> &BlockHeight { &self.height }

    async fn baker_id(&self) -> Option<BakerId> { self.baker_id }

    async fn total_amount(&self) -> &Amount { &self.total_amount }

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

    async fn special_events(
        &self,
        #[graphql(desc = "Filter special events by special event type. Set to null to return \
                          all special events (no filtering).")]
        include_filters: Option<Vec<SpecialEventTypeFilter>>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, SpecialEvent>> {
        todo_api!()
    }

    async fn transactions(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        todo_api!()
    }
}

#[derive(Enum, Copy, Clone, PartialEq, Eq)]
enum SpecialEventTypeFilter {
    Mint,
    FinalizationRewards,
    BlockRewards,
    BakingRewards,
    PaydayAccountReward,
    BlockAccrueReward,
    PaydayFoundationReward,
    PaydayPoolReward,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct Contract {
    contract_address_index:     ContractIndex,
    contract_address_sub_index: ContractIndex,
    contract_address:           String,
    creator:                    AccountAddress,
    block_height:               BlockHeight,
    transaction_hash:           String,
    block_slot_time:            DateTime,
    snapshot:                   ContractSnapshot,
}

#[ComplexObject]
impl Contract {
    // This function returns events from the `contract_events` table as well as
    // one `init_transaction_event` from when the contract was initialized. The
    // `skip` and `take` parameters are used to paginate the events.
    async fn contract_events(
        &self,
        ctx: &Context<'_>,
        skip: u32,
        take: u32,
    ) -> ApiResult<ContractEventsCollectionSegment> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;

        // If `skip` is 0 and at least one event is taken, include the
        // `init_transaction_event`.
        let include_initial_event = skip == 0 && take > 0;
        // Adjust the `take` and `skip` values considering if the
        // `init_transaction_event` is requested to be included or not.
        let take_without_initial_event = take.saturating_sub(include_initial_event as u32);
        let skip_without_initial_event = skip.saturating_sub(1);

        // Limit the number of events to be fetched from the `contract_events` table.
        let limit = std::cmp::min(
            take_without_initial_event as u64,
            config.contract_events_collection_limit.saturating_sub(include_initial_event as u64),
        );

        let mut contract_events = vec![];
        let mut total_events_count = 0;

        // Get the events from the `contract_events` table.
        let mut rows = sqlx::query!(
            "
            SELECT * FROM (
                SELECT
                    event_index_per_contract,
                    contract_events.transaction_index,
                    trace_element_index,
                    contract_events.block_height AS event_block_height,
                    transactions.hash as transaction_hash,
                    transactions.events,
                    accounts.address as creator,
                    blocks.slot_time as block_slot_time,
                    blocks.height as block_height
                FROM contract_events
                JOIN transactions
                    ON contract_events.block_height = transactions.block_height
                    AND contract_events.transaction_index = transactions.index
                JOIN accounts
                    ON transactions.sender = accounts.index
                JOIN blocks
                    ON contract_events.block_height = blocks.height
                WHERE contract_events.contract_index = $1 AND contract_events.contract_sub_index = \
             $2
                AND event_index_per_contract >= $4
                LIMIT $3
                ) AS contract_data
                ORDER BY event_index_per_contract DESC
            ",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
            limit as i64 + 1,
            skip_without_initial_event as i64
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = rows.len() > limit as usize;

        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            rows.pop();
        }

        for row in rows {
            let Some(events) = row.events else {
                return Err(ApiError::InternalError("Missing events in database".to_string()));
            };

            let mut events: Vec<Event> = serde_json::from_value(events).map_err(|_| {
                ApiError::InternalError("Failed to deserialize events from database".to_string())
            })?;

            if row.trace_element_index as usize >= events.len() {
                return Err(ApiError::InternalError(
                    "Trace element index does not exist in events".to_string(),
                ));
            }

            // Get the associated contract event from the `events` vector.
            let event = events.swap_remove(row.trace_element_index as usize);

            match event {
                Event::Transferred(_)
                | Event::ContractInterrupted(_)
                | Event::ContractResumed(_)
                | Event::ContractUpgraded(_)
                | Event::ContractUpdated(_) => Ok(()),
                _ => Err(ApiError::InternalError(format!(
                    "Not Transferred, ContractInterrupted, ContractResumed, ContractUpgraded, or \
                     ContractUpdated event; Wrong event enum tag: {:?}",
                    mem::discriminant(&event)
                ))),
            }?;

            let contract_event = ContractEvent {
                contract_address_index: self.contract_address_index,
                contract_address_sub_index: self.contract_address_sub_index,
                sender: row.creator.into(),
                event,
                block_height: row.block_height,
                transaction_hash: row.transaction_hash,
                block_slot_time: row.block_slot_time,
            };

            contract_events.push(contract_event);
            total_events_count += 1;
        }

        // Get the `init_transaction_event`.
        if include_initial_event {
            let row = sqlx::query!(
                "
                SELECT
                    module_reference,
                    name as contract_name,
                    contracts.amount as amount,
                    contracts.transaction_index as transaction_index,
                    transactions.events,
                    transactions.hash as transaction_hash,
                    transactions.block_height as block_height,
                    blocks.slot_time as block_slot_time,
                    accounts.address as creator
                FROM contracts
                JOIN transactions ON transaction_index=transactions.index
                JOIN blocks ON block_height = blocks.height
                JOIN accounts ON transactions.sender = accounts.index
                WHERE contracts.index = $1 AND contracts.sub_index = $2
                ",
                self.contract_address_index.0 as i64,
                self.contract_address_sub_index.0 as i64
            )
            .fetch_optional(pool)
            .await?
            .ok_or(ApiError::NotFound)?;

            let Some(events) = row.events else {
                return Err(ApiError::InternalError("Missing events in database".to_string()));
            };

            let [event]: [Event; 1] = serde_json::from_value(events).map_err(|_| {
                ApiError::InternalError(
                    "Failed to deserialize events from database. Contract init transaction \
                     expects exactly one event"
                        .to_string(),
                )
            })?;

            match event {
                Event::ContractInitialized(_) => Ok(()),
                _ => Err(ApiError::InternalError(format!(
                    "Not ContractInitialized event; Wrong event enum tag: {:?}",
                    mem::discriminant(&event)
                ))),
            }?;

            let initial_event = ContractEvent {
                contract_address_index: self.contract_address_index,
                contract_address_sub_index: self.contract_address_sub_index,
                sender: row.creator.into(),
                event,
                block_height: row.block_height,
                transaction_hash: row.transaction_hash,
                block_slot_time: row.block_slot_time,
            };
            contract_events.push(initial_event);
            total_events_count += 1;
        }

        Ok(ContractEventsCollectionSegment {
            page_info:   CollectionSegmentInfo {
                has_next_page,
                has_previous_page: skip > 0,
            },
            items:       contract_events,
            total_count: total_events_count,
        })
    }

    async fn contract_reject_events(
        &self,
        _skip: u32,
        _take: u32,
    ) -> ApiResult<ContractRejectEventsCollectionSegment> {
        todo_api!()
    }

    async fn tokens(&self, skip: u32, take: u32) -> ApiResult<TokensCollectionSegment> {
        todo_api!()
    }
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct TokensCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<Token>,
    total_count: i32,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ContractRejectEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<ContractRejectEvent>,
    total_count: i32,
}

#[derive(SimpleObject)]
struct ContractRejectEvent {
    contract_address_index:     ContractIndex,
    contract_address_sub_index: ContractIndex,
    sender:                     AccountAddress,
    rejected_event:             TransactionRejectReason,
    block_height:               BlockHeight,
    transaction_hash:           TransactionHash,
    block_slot_time:            DateTime,
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum TransactionRejectReason {
    ModuleNotWf(ModuleNotWf),
    ModuleHashAlreadyExists(ModuleHashAlreadyExists),
    InvalidAccountReference(InvalidAccountReference),
    InvalidInitMethod(InvalidInitMethod),
    InvalidReceiveMethod(InvalidReceiveMethod),
    InvalidModuleReference(InvalidModuleReference),
    InvalidContractAddress(InvalidContractAddress),
    RuntimeFailure(RuntimeFailure),
    AmountTooLarge(AmountTooLarge),
    SerializationFailure(SerializationFailure),
    OutOfEnergy(OutOfEnergy),
    RejectedInit(RejectedInit),
    RejectedReceive(RejectedReceive),
    NonExistentRewardAccount(NonExistentRewardAccount),
    InvalidProof(InvalidProof),
    AlreadyABaker(AlreadyABaker),
    NotABaker(NotABaker),
    InsufficientBalanceForBakerStake(InsufficientBalanceForBakerStake),
    StakeUnderMinimumThresholdForBaking(StakeUnderMinimumThresholdForBaking),
    BakerInCooldown(BakerInCooldown),
    DuplicateAggregationKey(DuplicateAggregationKey),
    NonExistentCredentialId(NonExistentCredentialId),
    KeyIndexAlreadyInUse(KeyIndexAlreadyInUse),
    InvalidAccountThreshold(InvalidAccountThreshold),
    InvalidCredentialKeySignThreshold(InvalidCredentialKeySignThreshold),
    InvalidEncryptedAmountTransferProof(InvalidEncryptedAmountTransferProof),
    InvalidTransferToPublicProof(InvalidTransferToPublicProof),
    EncryptedAmountSelfTransfer(EncryptedAmountSelfTransfer),
    InvalidIndexOnEncryptedTransfer(InvalidIndexOnEncryptedTransfer),
    ZeroScheduledAmount(ZeroScheduledAmount),
    NonIncreasingSchedule(NonIncreasingSchedule),
    FirstScheduledReleaseExpired(FirstScheduledReleaseExpired),
    ScheduledSelfTransfer(ScheduledSelfTransfer),
    InvalidCredentials(InvalidCredentials),
    DuplicateCredIds(DuplicateCredIds),
    NonExistentCredIds(NonExistentCredIds),
    RemoveFirstCredential(RemoveFirstCredential),
    CredentialHolderDidNotSign(CredentialHolderDidNotSign),
    NotAllowedMultipleCredentials(NotAllowedMultipleCredentials),
    NotAllowedToReceiveEncrypted(NotAllowedToReceiveEncrypted),
    NotAllowedToHandleEncrypted(NotAllowedToHandleEncrypted),
    MissingBakerAddParameters(MissingBakerAddParameters),
    FinalizationRewardCommissionNotInRange(FinalizationRewardCommissionNotInRange),
    BakingRewardCommissionNotInRange(BakingRewardCommissionNotInRange),
    TransactionFeeCommissionNotInRange(TransactionFeeCommissionNotInRange),
    AlreadyADelegator(AlreadyADelegator),
    InsufficientBalanceForDelegationStake(InsufficientBalanceForDelegationStake),
    MissingDelegationAddParameters(MissingDelegationAddParameters),
    InsufficientDelegationStake(InsufficientDelegationStake),
    DelegatorInCooldown(DelegatorInCooldown),
    NotADelegator(NotADelegator),
    DelegationTargetNotABaker(DelegationTargetNotABaker),
    StakeOverMaximumThresholdForPool(StakeOverMaximumThresholdForPool),
    PoolWouldBecomeOverDelegated(PoolWouldBecomeOverDelegated),
    PoolClosed(PoolClosed),
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ModuleNotWf {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ModuleHashAlreadyExists {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidInitMethod {
    module_ref: String,
    init_name:  String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidReceiveMethod {
    module_ref:   String,
    receive_name: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidAccountReference {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidModuleReference {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidContractAddress {
    contract_address: ContractAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RuntimeFailure {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AmountTooLarge {
    address: Address,
    amount:  Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct SerializationFailure {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct OutOfEnergy {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RejectedInit {
    reject_reason: i32,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RejectedReceive {
    reject_reason:    i32,
    contract_address: ContractAddress,
    receive_name:     String,
    message_as_hex:   String,
    // TODO message: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonExistentRewardAccount {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AlreadyABaker {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotABaker {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InsufficientBalanceForBakerStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InsufficientBalanceForDelegationStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InsufficientDelegationStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct StakeUnderMinimumThresholdForBaking {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct StakeOverMaximumThresholdForPool {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerInCooldown {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DuplicateAggregationKey {
    aggregation_key: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonExistentCredentialId {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct KeyIndexAlreadyInUse {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidAccountThreshold {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidCredentialKeySignThreshold {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidEncryptedAmountTransferProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidTransferToPublicProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct EncryptedAmountSelfTransfer {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidIndexOnEncryptedTransfer {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ZeroScheduledAmount {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonIncreasingSchedule {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct FirstScheduledReleaseExpired {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ScheduledSelfTransfer {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidCredentials {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DuplicateCredIds {
    cred_ids: Vec<String>,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonExistentCredIds {
    cred_ids: Vec<String>,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RemoveFirstCredential {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialHolderDidNotSign {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotAllowedMultipleCredentials {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotAllowedToReceiveEncrypted {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotAllowedToHandleEncrypted {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct MissingBakerAddParameters {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct FinalizationRewardCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakingRewardCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct TransactionFeeCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AlreadyADelegator {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct MissingDelegationAddParameters {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegatorInCooldown {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotADelegator {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationTargetNotABaker {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PoolWouldBecomeOverDelegated {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PoolClosed {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject)]
struct ContractSnapshot {
    block_height:               BlockHeight,
    contract_address_index:     ContractIndex,
    contract_address_sub_index: ContractIndex,
    contract_name:              String,
    module_reference:           String,
    amount:                     Amount,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ContractEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<ContractEvent>,
    total_count: i32,
}

#[derive(SimpleObject)]
struct ContractEvent {
    contract_address_index: ContractIndex,
    contract_address_sub_index: ContractIndex,
    sender: AccountAddress,
    event: Event,
    block_height: BlockHeight,
    transaction_hash: String,
    block_slot_time: DateTime,
}

/// Information about the offset pagination.
#[derive(SimpleObject)]
struct CollectionSegmentInfo {
    /// Indicates whether more items exist following the set defined by the
    /// clients arguments.
    has_next_page:     bool,
    /// Indicates whether more items exist prior the set defined by the clients
    /// arguments.
    has_previous_page: bool,
}

#[derive(SimpleObject)]
struct AccountReward {
    block:       Block,
    id:          types::ID,
    timestamp:   DateTime,
    reward_type: RewardType,
    amount:      Amount,
}

#[derive(Enum, Copy, Clone, PartialEq, Eq)]
#[allow(clippy::enum_variant_names)]
enum RewardType {
    FinalizationReward,
    FoundationReward,
    BakerReward,
    TransactionFeeReward,
}

#[derive(SimpleObject)]
struct AccountStatementEntry {
    reference:       BlockOrTransaction,
    id:              types::ID,
    timestamp:       DateTime,
    entry_type:      AccountStatementEntryType,
    amount:          i64,
    account_balance: Amount,
}

#[derive(SimpleObject)]
struct AccountTransactionRelation {
    transaction: Transaction,
}

#[derive(SimpleObject)]
struct AccountAddressAmount {
    account_address: AccountAddress,
    amount:          Amount,
}

#[derive(SimpleObject)]
struct AccountReleaseScheduleItem {
    transaction: Transaction,
    timestamp:   DateTime,
    amount:      Amount,
}

#[derive(SimpleObject)]
struct AccountToken {
    contract_index:     ContractIndex,
    contract_sub_index: ContractIndex,
    token_id:           String,
    balance:            BigInteger,
    token:              Token,
    account_id:         i64,
    account:            Account,
}

#[derive(SimpleObject)]
struct Token {
    initial_transaction:        Transaction,
    contract_index:             ContractIndex,
    contract_sub_index:         ContractIndex,
    token_id:                   String,
    metadata_url:               String,
    total_supply:               BigInteger,
    contract_address_formatted: String,
    token_address:              String,
    // TODO accounts(skip: Int take: Int): AccountsCollectionSegment
    // TODO tokenEvents(skip: Int take: Int): TokenEventsCollectionSegment
}

#[derive(Union)]
#[allow(clippy::enum_variant_names)]
enum SpecialEvent {
    MintSpecialEvent(MintSpecialEvent),
    FinalizationRewardsSpecialEvent(FinalizationRewardsSpecialEvent),
    BlockRewardsSpecialEvent(BlockRewardsSpecialEvent),
    BakingRewardsSpecialEvent(BakingRewardsSpecialEvent),
    PaydayAccountRewardSpecialEvent(PaydayAccountRewardSpecialEvent),
    BlockAccrueRewardSpecialEvent(BlockAccrueRewardSpecialEvent),
    PaydayFoundationRewardSpecialEvent(PaydayFoundationRewardSpecialEvent),
    PaydayPoolRewardSpecialEvent(PaydayPoolRewardSpecialEvent),
}

#[derive(SimpleObject)]
struct MintSpecialEvent {
    baking_reward: Amount,
    finalization_reward: Amount,
    platform_development_charge: Amount,
    foundation_account_address: AccountAddress,
    id: types::ID,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct FinalizationRewardsSpecialEvent {
    remainder: Amount,
    id:        types::ID,
}

#[ComplexObject]
impl FinalizationRewardsSpecialEvent {
    async fn finalization_rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountAddressAmount>> {
        todo_api!()
    }
}

#[derive(SimpleObject)]
struct BlockRewardsSpecialEvent {
    transaction_fees: Amount,
    old_gas_account: Amount,
    new_gas_account: Amount,
    baker_reward: Amount,
    foundation_charge: Amount,
    baker_account_address: AccountAddress,
    foundation_account_address: AccountAddress,
    id: types::ID,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct BakingRewardsSpecialEvent {
    remainder: Amount,
    id:        types::ID,
}
#[ComplexObject]
impl BakingRewardsSpecialEvent {
    async fn baking_rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountAddressAmount>> {
        todo_api!()
    }
}

#[derive(SimpleObject)]
struct PaydayAccountRewardSpecialEvent {
    /// The account that got rewarded.
    account:             AccountAddress,
    /// The transaction fee reward at payday to the account.
    transaction_fees:    Amount,
    /// The baking reward at payday to the account.
    baker_reward:        Amount,
    /// The finalization reward at payday to the account.
    finalization_reward: Amount,
    id:                  types::ID,
}

#[derive(SimpleObject)]
struct BlockAccrueRewardSpecialEvent {
    /// The total fees paid for transactions in the block.
    transaction_fees:  Amount,
    /// The old balance of the GAS account.
    old_gas_account:   Amount,
    /// The new balance of the GAS account.
    new_gas_account:   Amount,
    /// The amount awarded to the baker.
    baker_reward:      Amount,
    /// The amount awarded to the passive delegators.
    passive_reward:    Amount,
    /// The amount awarded to the foundation.
    foundation_charge: Amount,
    /// The baker of the block, who will receive the award.
    baker_id:          BakerId,
    id:                types::ID,
}

#[derive(SimpleObject)]
struct PaydayFoundationRewardSpecialEvent {
    foundation_account: AccountAddress,
    development_charge: Amount,
    id:                 types::ID,
}

#[derive(SimpleObject)]
struct PaydayPoolRewardSpecialEvent {
    /// The pool awarded.
    pool:                PoolRewardTarget,
    /// Accrued transaction fees for pool.
    transaction_fees:    Amount,
    /// Accrued baking rewards for pool.
    baker_reward:        Amount,
    /// Accrued finalization rewards for pool.
    finalization_reward: Amount,
    id:                  types::ID,
}

#[derive(Union)]
enum PoolRewardTarget {
    PassiveDelegationPoolRewardTarget(PassiveDelegationPoolRewardTarget),
    BakerPoolRewardTarget(BakerPoolRewardTarget),
}

#[derive(SimpleObject)]
struct PassiveDelegationPoolRewardTarget {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct PassiveDelegationTarget {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerPoolRewardTarget {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerDelegationTarget {
    baker_id: BakerId,
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

#[derive(Interface)]
#[allow(clippy::duplicated_attributes)]
#[graphql(
    field(name = "euro_per_energy", ty = "&ExchangeRate"),
    field(name = "micro_ccd_per_euro", ty = "&ExchangeRate"),
    field(name = "account_creation_limit", ty = "&i32"),
    field(name = "foundation_account_address", ty = "&AccountAddress")
)]
enum ChainParameters {
    ChainParametersV0(ChainParametersV0),
    ChainParametersV1(ChainParametersV1),
    ChainParametersV2(ChainParametersV2),
}

#[derive(SimpleObject)]
struct ChainParametersV0 {
    // TODO
    //   electionDifficulty: Decimal!
    // bakerCooldownEpochs: UnsignedLong!
    // rewardParameters: RewardParametersV0!
    // minimumThresholdForBaking: UnsignedLong!
    euro_per_energy:            ExchangeRate,
    micro_ccd_per_euro:         ExchangeRate,
    account_creation_limit:     i32,
    foundation_account_address: AccountAddress,
}

#[derive(SimpleObject)]
struct ChainParametersV1 {
    // TODO
    // electionDifficulty: Decimal!
    //     poolOwnerCooldown: UnsignedLong!
    //     delegatorCooldown: UnsignedLong!
    //     rewardPeriodLength: UnsignedLong!
    //     mintPerPayday: Decimal!
    //     rewardParameters: RewardParametersV1!
    //     passiveFinalizationCommission: Decimal!
    //     passiveBakingCommission: Decimal!
    //     passiveTransactionCommission: Decimal!
    //     finalizationCommissionRange: CommissionRange!
    //     bakingCommissionRange: CommissionRange!
    //     transactionCommissionRange: CommissionRange!
    //     minimumEquityCapital: UnsignedLong!
    //     capitalBound: Decimal!
    //     leverageBound: LeverageFactor!
    euro_per_energy:            ExchangeRate,
    micro_ccd_per_euro:         ExchangeRate,
    account_creation_limit:     i32,
    foundation_account_address: AccountAddress,
}

#[derive(SimpleObject)]
struct ChainParametersV2 {
    // TODO
    // poolOwnerCooldown: UnsignedLong!
    // delegatorCooldown: UnsignedLong!
    // rewardPeriodLength: UnsignedLong!
    // mintPerPayday: Decimal!
    // rewardParameters: RewardParametersV2!
    // passiveFinalizationCommission: Decimal!
    // passiveBakingCommission: Decimal!
    // passiveTransactionCommission: Decimal!
    // finalizationCommissionRange: CommissionRange!
    // bakingCommissionRange: CommissionRange!
    // transactionCommissionRange: CommissionRange!
    // minimumEquityCapital: UnsignedLong!
    // capitalBound: Decimal!
    // leverageBound: LeverageFactor!
    euro_per_energy:            ExchangeRate,
    micro_ccd_per_euro:         ExchangeRate,
    account_creation_limit:     i32,
    foundation_account_address: AccountAddress,
}

#[derive(SimpleObject)]
struct ExchangeRate {
    numerator:   u64,
    denominator: u64,
}

#[derive(SimpleObject, Clone, serde::Serialize, serde::Deserialize)]
struct AccountAddress {
    as_string: String,
}

impl From<concordium_rust_sdk::common::types::AccountAddress> for AccountAddress {
    fn from(address: concordium_rust_sdk::common::types::AccountAddress) -> Self {
        address.to_string().into()
    }
}

impl From<String> for AccountAddress {
    fn from(as_string: String) -> Self {
        Self {
            as_string,
        }
    }
}

struct Transaction {
    index: i64,
    block_height: BlockHeight,
    hash: TransactionHash,
    ccd_cost: Amount,
    energy_cost: Energy,
    sender: Option<AccountIndex>,
    tx_type: DbTransactionType,
    type_account: Option<AccountTransactionType>,
    type_credential_deployment: Option<CredentialDeploymentTransactionType>,
    type_update: Option<UpdateTransactionType>,
    success: bool,
    events: Option<sqlx::types::Json<Vec<Event>>>,
    reject: Option<sqlx::types::Json<TransactionRejectReason>>,
}

impl Transaction {
    async fn query_by_index(pool: &PgPool, index: i64) -> ApiResult<Option<Self>> {
        let transaction = sqlx::query_as!(
            Transaction,
            r#"SELECT
                index,
                block_height,
                hash,
                ccd_cost,
                energy_cost,
                sender,
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

    async fn query_by_hash(
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
                sender,
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

    async fn transaction_index(&self) -> i64 { self.index }

    async fn transaction_hash(&self) -> &TransactionHash { &self.hash }

    async fn ccd_cost(&self) -> Amount { self.ccd_cost }

    async fn energy_cost(&self) -> Energy { self.energy_cost }

    async fn block<'a>(&self, ctx: &Context<'a>) -> ApiResult<Block> {
        Block::query_by_height(get_pool(ctx)?, self.block_height).await
    }

    async fn sender_account_address<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<Option<AccountAddress>> {
        let Some(account_index) = self.sender else {
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
#[allow(clippy::enum_variant_names)]
enum TransactionType {
    AccountTransaction(AccountTransaction),
    CredentialDeploymentTransaction(CredentialDeploymentTransaction),
    UpdateTransaction(UpdateTransaction),
}

#[derive(SimpleObject)]
struct AccountTransaction {
    account_transaction_type: Option<AccountTransactionType>,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "account_transaction_type")]
pub enum AccountTransactionType {
    InitializeSmartContractInstance,
    UpdateSmartContractInstance,
    SimpleTransfer,
    EncryptedTransfer,
    SimpleTransferWithMemo,
    EncryptedTransferWithMemo,
    TransferWithScheduleWithMemo,
    DeployModule,
    AddBaker,
    RemoveBaker,
    UpdateBakerStake,
    UpdateBakerRestakeEarnings,
    UpdateBakerKeys,
    UpdateCredentialKeys,
    TransferToEncrypted,
    TransferToPublic,
    TransferWithSchedule,
    UpdateCredentials,
    RegisterData,
    ConfigureBaker,
    ConfigureDelegation,
}

impl From<concordium_rust_sdk::types::TransactionType> for AccountTransactionType {
    fn from(value: concordium_rust_sdk::types::TransactionType) -> Self {
        use concordium_rust_sdk::types::TransactionType as TT;
        use AccountTransactionType as ATT;
        #[allow(deprecated)]
        match value {
            TT::DeployModule => ATT::DeployModule,
            TT::InitContract => ATT::InitializeSmartContractInstance,
            TT::Update => ATT::UpdateSmartContractInstance,
            TT::Transfer => ATT::SimpleTransfer,
            TT::AddBaker => ATT::AddBaker,
            TT::RemoveBaker => ATT::RemoveBaker,
            TT::UpdateBakerStake => ATT::UpdateBakerStake,
            TT::UpdateBakerRestakeEarnings => ATT::UpdateBakerRestakeEarnings,
            TT::UpdateBakerKeys => ATT::UpdateBakerKeys,
            TT::UpdateCredentialKeys => ATT::UpdateCredentialKeys,
            TT::EncryptedAmountTransfer => ATT::EncryptedTransfer,
            TT::TransferToEncrypted => ATT::TransferToEncrypted,
            TT::TransferToPublic => ATT::TransferToPublic,
            TT::TransferWithSchedule => ATT::TransferWithSchedule,
            TT::UpdateCredentials => ATT::UpdateCredentials,
            TT::RegisterData => ATT::RegisterData,
            TT::TransferWithMemo => ATT::SimpleTransferWithMemo,
            TT::EncryptedAmountTransferWithMemo => ATT::EncryptedTransferWithMemo,
            TT::TransferWithScheduleAndMemo => ATT::TransferWithScheduleWithMemo,
            TT::ConfigureBaker => ATT::ConfigureBaker,
            TT::ConfigureDelegation => ATT::ConfigureDelegation,
        }
    }
}

#[derive(SimpleObject)]
struct CredentialDeploymentTransaction {
    credential_deployment_transaction_type: CredentialDeploymentTransactionType,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "credential_deployment_transaction_type")]
pub enum CredentialDeploymentTransactionType {
    Initial,
    Normal,
}

impl From<concordium_rust_sdk::types::CredentialType> for CredentialDeploymentTransactionType {
    fn from(value: concordium_rust_sdk::types::CredentialType) -> Self {
        use concordium_rust_sdk::types::CredentialType;
        match value {
            CredentialType::Initial => CredentialDeploymentTransactionType::Initial,
            CredentialType::Normal => CredentialDeploymentTransactionType::Normal,
        }
    }
}

#[derive(SimpleObject)]
struct UpdateTransaction {
    update_transaction_type: UpdateTransactionType,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "update_transaction_type")]
pub enum UpdateTransactionType {
    UpdateProtocol,
    UpdateElectionDifficulty,
    UpdateEuroPerEnergy,
    UpdateMicroGtuPerEuro,
    UpdateFoundationAccount,
    UpdateMintDistribution,
    UpdateTransactionFeeDistribution,
    UpdateGasRewards,
    UpdateBakerStakeThreshold,
    UpdateAddAnonymityRevoker,
    UpdateAddIdentityProvider,
    UpdateRootKeys,
    UpdateLevel1Keys,
    UpdateLevel2Keys,
    UpdatePoolParameters,
    UpdateCooldownParameters,
    UpdateTimeParameters,
    MintDistributionCpv1Update,
    GasRewardsCpv2Update,
    TimeoutParametersUpdate,
    MinBlockTimeUpdate,
    BlockEnergyLimitUpdate,
    FinalizationCommitteeParametersUpdate,
}

impl From<concordium_rust_sdk::types::UpdateType> for UpdateTransactionType {
    fn from(value: concordium_rust_sdk::types::UpdateType) -> Self {
        use concordium_rust_sdk::types::UpdateType;
        match value {
            UpdateType::UpdateProtocol => UpdateTransactionType::UpdateProtocol,
            UpdateType::UpdateElectionDifficulty => UpdateTransactionType::UpdateElectionDifficulty,
            UpdateType::UpdateEuroPerEnergy => UpdateTransactionType::UpdateEuroPerEnergy,
            UpdateType::UpdateMicroGTUPerEuro => UpdateTransactionType::UpdateMicroGtuPerEuro,
            UpdateType::UpdateFoundationAccount => UpdateTransactionType::UpdateFoundationAccount,
            UpdateType::UpdateMintDistribution => UpdateTransactionType::UpdateMintDistribution,
            UpdateType::UpdateTransactionFeeDistribution => {
                UpdateTransactionType::UpdateTransactionFeeDistribution
            }
            UpdateType::UpdateGASRewards => UpdateTransactionType::UpdateGasRewards,
            UpdateType::UpdateAddAnonymityRevoker => {
                UpdateTransactionType::UpdateAddAnonymityRevoker
            }
            UpdateType::UpdateAddIdentityProvider => {
                UpdateTransactionType::UpdateAddIdentityProvider
            }
            UpdateType::UpdateRootKeys => UpdateTransactionType::UpdateRootKeys,
            UpdateType::UpdateLevel1Keys => UpdateTransactionType::UpdateLevel1Keys,
            UpdateType::UpdateLevel2Keys => UpdateTransactionType::UpdateLevel2Keys,
            UpdateType::UpdatePoolParameters => UpdateTransactionType::UpdatePoolParameters,
            UpdateType::UpdateCooldownParameters => UpdateTransactionType::UpdateCooldownParameters,
            UpdateType::UpdateTimeParameters => UpdateTransactionType::UpdateTimeParameters,
            UpdateType::UpdateGASRewardsCPV2 => UpdateTransactionType::GasRewardsCpv2Update,
            UpdateType::UpdateTimeoutParameters => UpdateTransactionType::TimeoutParametersUpdate,
            UpdateType::UpdateMinBlockTime => UpdateTransactionType::MinBlockTimeUpdate,
            UpdateType::UpdateBlockEnergyLimit => UpdateTransactionType::BlockEnergyLimitUpdate,
            UpdateType::UpdateFinalizationCommitteeParameters => {
                UpdateTransactionType::FinalizationCommitteeParametersUpdate
            }
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

#[derive(sqlx::FromRow)]
struct Account {
    // release_schedule: AccountReleaseSchedule,
    index:             i64,
    /// Index of the transaction creating this account.
    /// Only `None` for genesis accounts.
    transaction_index: Option<i64>,
    /// The address of the account in Base58Check.
    #[sqlx(try_from = "String")]
    address:           AccountAddress,
    /// The total amount of CCD hold by the account.
    amount:            Amount,
    /// The total delegated stake of this account.
    delegated_stake:   Amount,
    /// The total number of transactions this account has been involved in or
    /// affected by.
    num_txs:           i64,
    // Get baker information if this account is baking.
    // baker: Option<Baker>,
    // delegation: Option<Delegation>,
}
impl Account {
    async fn query_by_index(pool: &PgPool, index: AccountIndex) -> ApiResult<Option<Self>> {
        let account = sqlx::query_as!(
            Account,
            "SELECT index, transaction_index, address, amount, delegated_stake, num_txs
            FROM accounts
            WHERE index = $1",
            index
        )
        .fetch_optional(pool)
        .await?;
        Ok(account)
    }

    async fn query_by_address(pool: &PgPool, address: String) -> ApiResult<Option<Self>> {
        let account = sqlx::query_as!(
            Account,
            "SELECT index, transaction_index, address, amount, delegated_stake, num_txs
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
    async fn id(&self) -> types::ID { types::ID::from(self.index) }

    /// The address of the account in Base58Check.
    async fn address(&self) -> &AccountAddress { &self.address }

    /// The total amount of CCD hold by the account.
    async fn amount(&self) -> Amount { self.amount }

    /// Timestamp of the block where this account was created.
    async fn created_at(&self, ctx: &Context<'_>) -> ApiResult<DateTime> {
        let slot_time = sqlx::query_scalar!(
            "SELECT slot_time
            FROM transactions
            JOIN blocks ON transactions.block_height = blocks.height
            WHERE transactions.index = $1",
            self.transaction_index
        )
        .fetch_one(get_pool(ctx)?)
        .await?;

        Ok(slot_time)
    }

    /// Number of transactions where this account is used as sender.
    async fn transaction_count<'a>(&self, ctx: &Context<'a>) -> ApiResult<i64> {
        let rec = sqlx::query!("SELECT COUNT(*) FROM transactions WHERE sender=$1", self.index)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(rec.count.unwrap_or(0))
    }

    async fn tokens(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountToken>> {
        todo_api!()
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
                    sender,
                    type as "tx_type: DbTransactionType",
                    type_account as "type_account: AccountTransactionType",
                    type_credential_deployment as "type_credential_deployment: CredentialDeploymentTransactionType",
                    type_update as "type_update: UpdateTransactionType",
                    success,
                    events as "events: sqlx::types::Json<Vec<Event>>",
                    reject as "reject: sqlx::types::Json<TransactionRejectReason>"
                FROM transactions
                WHERE
                    $1 IN (
                        SELECT account_index
                        FROM affected_accounts
                        WHERE transaction_index = index
                    )
                    AND $2 < index
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
            query.desc,
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
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountStatementEntry>> {
        todo_api!()
    }

    async fn rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountReward>> {
        todo_api!()
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct AccountReleaseSchedule {
    total_amount: Amount,
}
#[ComplexObject]
impl AccountReleaseSchedule {
    async fn schedule(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountReleaseScheduleItem>> {
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

struct Baker {
    id: BakerId,
    staked: Amount,
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
            baker_id
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
            staked_amount:    self.staked,
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
        Account::query_by_index(get_pool(ctx)?, self.id).await?.ok_or(ApiError::NotFound)
    }

    // transactions("Returns the first _n_ elements from the list." first: Int
    // "Returns the elements in the list that come after the specified cursor."
    // after: String "Returns the last _n_ elements from the list." last: Int
    // "Returns the elements in the list that come before the specified cursor."
    // before: String): BakerTransactionRelationConnection
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

#[derive(SimpleObject)]
struct NodeStatus {
    // TODO: add below fields
    // peersList: [PeerReference!]!
    // nodeName: String
    // nodeId: String!
    // peerType: String!
    // uptime: UnsignedLong!
    // clientVersion: String!
    // averagePing: Float
    // peersCount: UnsignedLong!
    // bestBlock: String!
    // bestBlockHeight: UnsignedLong!
    // bestBlockBakerId: UnsignedLong
    // bestArrivedTime: DateTime
    // blockArrivePeriodEma: Float
    // blockArrivePeriodEmsd: Float
    // blockArriveLatencyEma: Float
    // blockArriveLatencyEmsd: Float
    // blockReceivePeriodEma: Float
    // blockReceivePeriodEmsd: Float
    // blockReceiveLatencyEma: Float
    // blockReceiveLatencyEmsd: Float
    // finalizedBlock: String!
    // finalizedBlockHeight: UnsignedLong!
    // finalizedTime: DateTime
    // finalizationPeriodEma: Float
    // finalizationPeriodEmsd: Float
    // packetsSent: UnsignedLong!
    // packetsReceived: UnsignedLong!
    // consensusRunning: Boolean!
    // bakingCommitteeMember: String!
    // consensusBakerId: UnsignedLong
    // finalizationCommitteeMember: Boolean!
    // transactionsPerBlockEma: Float
    // transactionsPerBlockEmsd: Float
    // bestBlockTransactionsSize: UnsignedLong
    // bestBlockTotalEncryptedAmount: UnsignedLong
    // bestBlockTotalAmount: UnsignedLong
    // bestBlockTransactionCount: UnsignedLong
    // bestBlockTransactionEnergyCost: UnsignedLong
    // bestBlockExecutionCost: UnsignedLong
    // bestBlockCentralBankAmount: UnsignedLong
    // blocksReceivedCount: UnsignedLong
    // blocksVerifiedCount: UnsignedLong
    // genesisBlock: String!
    // finalizationCount: UnsignedLong
    // finalizedBlockParent: String!
    // averageBytesPerSecondIn: Float!
    // averageBytesPerSecondOut: Float!
    id: types::ID,
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

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize, sqlx::Type)]
#[sqlx(type_name = "pool_open_status")] // only for PostgreSQL to match a type definition
pub enum BakerPoolOpenStatus {
    OpenForAll,
    ClosedForNew,
    ClosedForAll,
}

impl From<concordium_rust_sdk::types::OpenStatus> for BakerPoolOpenStatus {
    fn from(status: concordium_rust_sdk::types::OpenStatus) -> Self {
        use concordium_rust_sdk::types::OpenStatus;
        match status {
            OpenStatus::OpenForAll => Self::OpenForAll,
            OpenStatus::ClosedForNew => Self::ClosedForNew,
            OpenStatus::ClosedForAll => Self::ClosedForAll,
        }
    }
}

#[derive(SimpleObject)]
struct Ranking {
    rank:  i32,
    total: i32,
}

#[derive(SimpleObject)]
struct Delegation {
    delegator_id:      i64,
    staked_amount:     Amount,
    restake_earnings:  bool,
    delegation_target: DelegationTarget,
    pending_change:    PendingDelegationChange,
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
enum DelegationTarget {
    PassiveDelegationTarget(PassiveDelegationTarget),
    BakerDelegationTarget(BakerDelegationTarget),
}

impl TryFrom<concordium_rust_sdk::types::DelegationTarget> for DelegationTarget {
    type Error = anyhow::Error;

    fn try_from(target: concordium_rust_sdk::types::DelegationTarget) -> Result<Self, Self::Error> {
        use concordium_rust_sdk::types::DelegationTarget as Target;
        match target {
            Target::Passive => {
                Ok(DelegationTarget::PassiveDelegationTarget(PassiveDelegationTarget {
                    dummy: true,
                }))
            }
            Target::Baker {
                baker_id,
            } => Ok(DelegationTarget::BakerDelegationTarget(BakerDelegationTarget {
                baker_id: baker_id.id.index.try_into()?,
            })),
        }
    }
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

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum AccountStatementEntryType {
    TransferIn,
    TransferOut,
    AmountDecrypted,
    AmountEncrypted,
    TransactionFee,
    FinalizationReward,
    FoundationReward,
    BakerReward,
    TransactionFeeReward,
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

impl AccountOrder {
    /// Returns a string that represents a GraphQL cursor, when ordering
    /// accounts by the given field.
    fn cursor(&self, account: &Account) -> String {
        match self.field {
            // Index and age correspond monotonically.
            AccountOrderField::Age => account.index,
            AccountOrderField::Amount => account.amount,
            AccountOrderField::TransactionCount => account.num_txs,
            AccountOrderField::DelegatedStake => account.delegated_stake,
        }
        .to_string()
    }
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

/// A sort direction, either ascending or descending.
#[derive(Debug, Clone, Copy)]
enum OrderDir {
    Asc,
    Desc,
}

#[derive(InputObject)]
struct AccountFilterInput {
    is_delegator: bool,
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

struct SearchResult {
    _query: String,
}

#[Object]
impl SearchResult {
    async fn contracts<'a>(
        &self,
        _ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Contract>> {
        todo_api!()
    }

    // async fn modules(
    //     &self,
    //     #[graphql(desc = "Returns the first _n_ elements from the list.")]
    // _first: Option<i32>,     #[graphql(desc = "Returns the elements in the
    // list that come after the specified cursor.")]     _after: Option<String>,
    //     #[graphql(desc = "Returns the last _n_ elements from the list.")] _last:
    // Option<i32>,     #[graphql(
    //         desc = "Returns the elements in the list that come before the
    // specified cursor."     )]
    //     _before: Option<String>,
    // ) -> ApiResult<connection::Connection<String, Module>> {
    //     todo_api!()
    // }

    async fn blocks(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Block>> {
        todo_api!()
    }

    async fn transactions(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        todo_api!()
    }

    async fn tokens(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Token>> {
        todo_api!()
    }

    async fn accounts(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Account>> {
        todo_api!()
    }

    async fn bakers(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        todo_api!()
    }

    async fn node_statuses(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, NodeStatus>> {
        todo_api!()
    }
}

fn decode_value_with_schema(
    opt_schema: Option<&Type>,
    value: &[u8],
    schema_name: SmartContractSchemaNames,
) -> String {
    let Some(schema) = opt_schema else {
        // Note: There could be something better displayed than this string if no schema is
        // available for decoding at the frontend long-term.
        return format!(
            "No embedded {} schema in smart contract available for decoding",
            schema_name.kind()
        );
    };

    let mut cursor = Cursor::new(&value);
    match schema.to_json(&mut cursor) {
        Ok(v) => {
            serde_json::to_string(&v).unwrap_or_else(|e| {
                // We don't return an error here since the query is correctly formed and
                // the CCDScan backend is working as expected.
                // A wrong/missing schema is a mistake by the smart contract
                // developer which in general cannot be fixed after the deployment of
                // the contract. We display the error message (instead of the decoded
                // value) in the block explorer to make the info visible to the smart
                // contract developer for debugging purposes here.
                format!(
                    "Failed to deserialize {} with {} schema into string: {:?}",
                    schema_name.value(),
                    schema_name.kind(),
                    e
                )
            })
        }
        Err(e) => {
            // We don't return an error here since the query is correctly formed and
            // the CCDScan backend is working as expected.
            // A wrong/missing schema is a mistake by the smart contract
            // developer which in general cannot be fixed after the deployment of
            // the contract. We display the error message (instead of the decoded
            // value) in the block explorer to make the info visible to the smart
            // contract developer for debugging purposes here.
            format!(
                "Failed to deserialize {} with {} schema: {:?}",
                schema_name.value(),
                schema_name.kind(),
                e.display(true)
            )
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractAddress {
    index:     ContractIndex,
    sub_index: ContractIndex,
    as_string: String,
}

impl From<concordium_rust_sdk::types::ContractAddress> for ContractAddress {
    fn from(value: concordium_rust_sdk::types::ContractAddress) -> Self {
        Self {
            index:     value.index.into(),
            sub_index: value.subindex.into(),
            as_string: value.to_string(),
        }
    }
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
enum Address {
    ContractAddress(ContractAddress),
    AccountAddress(AccountAddress),
}

impl From<concordium_rust_sdk::types::Address> for Address {
    fn from(value: concordium_rust_sdk::types::Address) -> Self {
        use concordium_rust_sdk::types::Address as Addr;
        match value {
            Addr::Account(a) => Address::AccountAddress(a.into()),
            Addr::Contract(c) => Address::ContractAddress(c.into()),
        }
    }
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum Event {
    /// A transfer of CCD. Can be either from an account or a smart contract
    /// instance, but the receiver in this event is always an account.
    Transferred(Transferred),
    AccountCreated(AccountCreated),
    AmountAddedByDecryption(AmountAddedByDecryption),
    BakerAdded(BakerAdded),
    BakerKeysUpdated(BakerKeysUpdated),
    BakerRemoved(BakerRemoved),
    BakerSetRestakeEarnings(BakerSetRestakeEarnings),
    BakerStakeDecreased(BakerStakeDecreased),
    BakerStakeIncreased(BakerStakeIncreased),
    ContractInitialized(ContractInitialized),
    ContractModuleDeployed(ContractModuleDeployed),
    ContractUpdated(ContractUpdated),
    ContractCall(ContractCall),
    CredentialDeployed(CredentialDeployed),
    CredentialKeysUpdated(CredentialKeysUpdated),
    CredentialsUpdated(CredentialsUpdated),
    DataRegistered(DataRegistered),
    EncryptedAmountsRemoved(EncryptedAmountsRemoved),
    EncryptedSelfAmountAdded(EncryptedSelfAmountAdded),
    NewEncryptedAmount(NewEncryptedAmount),
    TransferMemo(TransferMemo),
    TransferredWithSchedule(TransferredWithSchedule),
    ChainUpdateEnqueued(ChainUpdateEnqueued),
    ContractInterrupted(ContractInterrupted),
    ContractResumed(ContractResumed),
    ContractUpgraded(ContractUpgraded),
    BakerSetOpenStatus(BakerSetOpenStatus),
    BakerSetMetadataURL(BakerSetMetadataURL),
    BakerSetTransactionFeeCommission(BakerSetTransactionFeeCommission),
    BakerSetBakingRewardCommission(BakerSetBakingRewardCommission),
    BakerSetFinalizationRewardCommission(BakerSetFinalizationRewardCommission),
    DelegationAdded(DelegationAdded),
    DelegationRemoved(DelegationRemoved),
    DelegationStakeIncreased(DelegationStakeIncreased),
    DelegationStakeDecreased(DelegationStakeDecreased),
    DelegationSetRestakeEarnings(DelegationSetRestakeEarnings),
    DelegationSetDelegationTarget(DelegationSetDelegationTarget),
}

pub fn events_from_summary(
    value: concordium_rust_sdk::types::BlockItemSummaryDetails,
) -> anyhow::Result<Vec<Event>> {
    use concordium_rust_sdk::types::{AccountTransactionEffects, BlockItemSummaryDetails};
    let events = match value {
        BlockItemSummaryDetails::AccountTransaction(details) => match details.effects {
            AccountTransactionEffects::None {
                ..
            } => {
                anyhow::bail!("Transaction was rejected")
            }
            AccountTransactionEffects::ModuleDeployed {
                module_ref,
            } => {
                vec![Event::ContractModuleDeployed(ContractModuleDeployed {
                    module_ref: module_ref.to_string(),
                })]
            }
            AccountTransactionEffects::ContractInitialized {
                data,
            } => {
                vec![Event::ContractInitialized(ContractInitialized {
                    module_ref:        data.origin_ref.to_string(),
                    contract_address:  data.address.into(),
                    amount:            i64::try_from(data.amount.micro_ccd)?,
                    init_name:         data.init_name.to_string(),
                    version:           data.contract_version.into(),
                    contract_logs_raw: data.events.iter().map(|e| e.as_ref().to_vec()).collect(),
                })]
            }
            AccountTransactionEffects::ContractUpdateIssued {
                effects,
            } => {
                use concordium_rust_sdk::types::ContractTraceElement;
                effects
                    .into_iter()
                    .map(|effect| match effect {
                        ContractTraceElement::Updated {
                            data,
                        } => Ok(Event::ContractUpdated(ContractUpdated {
                            contract_address:  data.address.into(),
                            instigator:        data.instigator.into(),
                            amount:            data.amount.micro_ccd().try_into()?,
                            receive_name:      data.receive_name.to_string(),
                            version:           data.contract_version.into(),
                            input_parameter:   data.message.as_ref().to_vec(),
                            contract_logs_raw: data
                                .events
                                .iter()
                                .map(|e| e.as_ref().to_vec())
                                .collect(),
                        })),
                        ContractTraceElement::Transferred {
                            from,
                            amount,
                            to,
                        } => Ok(Event::Transferred(Transferred {
                            amount: amount.micro_ccd().try_into()?,
                            from:   Address::ContractAddress(from.into()),
                            to:     to.into(),
                        })),
                        ContractTraceElement::Interrupted {
                            address,
                            events,
                        } => Ok(Event::ContractInterrupted(ContractInterrupted {
                            contract_address:  address.into(),
                            contract_logs_raw: events.iter().map(|e| e.as_ref().to_vec()).collect(),
                        })),
                        ContractTraceElement::Resumed {
                            address,
                            success,
                        } => Ok(Event::ContractResumed(ContractResumed {
                            contract_address: address.into(),
                            success,
                        })),
                        ContractTraceElement::Upgraded {
                            address,
                            from,
                            to,
                        } => Ok(Event::ContractUpgraded(ContractUpgraded {
                            contract_address: address.into(),
                            from:             from.to_string(),
                            to:               to.to_string(),
                        })),
                    })
                    .collect::<anyhow::Result<Vec<_>>>()?
            }
            AccountTransactionEffects::AccountTransfer {
                amount,
                to,
            } => {
                vec![Event::Transferred(Transferred {
                    amount: i64::try_from(amount.micro_ccd)?,
                    from:   Address::AccountAddress(details.sender.into()),
                    to:     to.into(),
                })]
            }
            AccountTransactionEffects::AccountTransferWithMemo {
                amount,
                to,
                memo,
            } => {
                vec![
                    Event::Transferred(Transferred {
                        amount: i64::try_from(amount.micro_ccd)?,
                        from:   Address::AccountAddress(details.sender.into()),
                        to:     to.into(),
                    }),
                    Event::TransferMemo(memo.into()),
                ]
            }
            AccountTransactionEffects::BakerAdded {
                data,
            } => {
                vec![Event::BakerAdded(BakerAdded {
                    staked_amount:    data.stake.micro_ccd.try_into()?,
                    restake_earnings: data.restake_earnings,
                    baker_id:         data.keys_event.baker_id.id.index.try_into()?,
                    sign_key:         serde_json::to_string(&data.keys_event.sign_key)?,
                    election_key:     serde_json::to_string(&data.keys_event.election_key)?,
                    aggregation_key:  serde_json::to_string(&data.keys_event.aggregation_key)?,
                })]
            }
            AccountTransactionEffects::BakerRemoved {
                baker_id,
            } => {
                vec![Event::BakerRemoved(BakerRemoved {
                    baker_id: baker_id.id.index.try_into()?,
                })]
            }
            AccountTransactionEffects::BakerStakeUpdated {
                data,
            } => {
                if let Some(data) = data {
                    if data.increased {
                        vec![Event::BakerStakeIncreased(BakerStakeIncreased {
                            baker_id:          data.baker_id.id.index.try_into()?,
                            new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                        })]
                    } else {
                        vec![Event::BakerStakeDecreased(BakerStakeDecreased {
                            baker_id:          data.baker_id.id.index.try_into()?,
                            new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                        })]
                    }
                } else {
                    Vec::new()
                }
            }
            AccountTransactionEffects::BakerRestakeEarningsUpdated {
                baker_id,
                restake_earnings,
            } => {
                vec![Event::BakerSetRestakeEarnings(BakerSetRestakeEarnings {
                    baker_id: baker_id.id.index.try_into()?,
                    restake_earnings,
                })]
            }
            AccountTransactionEffects::BakerKeysUpdated {
                data,
            } => {
                vec![Event::BakerKeysUpdated(BakerKeysUpdated {
                    baker_id:        data.baker_id.id.index.try_into()?,
                    sign_key:        serde_json::to_string(&data.sign_key)?,
                    election_key:    serde_json::to_string(&data.election_key)?,
                    aggregation_key: serde_json::to_string(&data.aggregation_key)?,
                })]
            }
            AccountTransactionEffects::EncryptedAmountTransferred {
                removed,
                added,
            } => {
                vec![
                    Event::EncryptedAmountsRemoved((*removed).try_into()?),
                    Event::NewEncryptedAmount((*added).try_into()?),
                ]
            }
            AccountTransactionEffects::EncryptedAmountTransferredWithMemo {
                removed,
                added,
                memo,
            } => {
                vec![
                    Event::EncryptedAmountsRemoved((*removed).try_into()?),
                    Event::NewEncryptedAmount((*added).try_into()?),
                    Event::TransferMemo(memo.into()),
                ]
            }
            AccountTransactionEffects::TransferredToEncrypted {
                data,
            } => {
                vec![Event::EncryptedSelfAmountAdded(EncryptedSelfAmountAdded {
                    account_address:      data.account.into(),
                    new_encrypted_amount: serde_json::to_string(&data.new_amount)?,
                    amount:               data.amount.micro_ccd.try_into()?,
                })]
            }
            AccountTransactionEffects::TransferredToPublic {
                removed,
                amount,
            } => {
                vec![
                    Event::EncryptedAmountsRemoved((*removed).try_into()?),
                    Event::AmountAddedByDecryption(AmountAddedByDecryption {
                        amount:          amount.micro_ccd().try_into()?,
                        account_address: details.sender.into(),
                    }),
                ]
            }
            AccountTransactionEffects::TransferredWithSchedule {
                to,
                amount,
            } => {
                vec![Event::TransferredWithSchedule(TransferredWithSchedule {
                    from_account_address: details.sender.into(),
                    to_account_address:   to.into(),
                    total_amount:         amount
                        .into_iter()
                        .map(|(_, amount)| amount.micro_ccd())
                        .sum::<u64>()
                        .try_into()?,
                })]
            }
            AccountTransactionEffects::TransferredWithScheduleAndMemo {
                to,
                amount,
                memo,
            } => {
                vec![
                    Event::TransferredWithSchedule(TransferredWithSchedule {
                        from_account_address: details.sender.into(),
                        to_account_address:   to.into(),
                        total_amount:         amount
                            .into_iter()
                            .map(|(_, amount)| amount.micro_ccd())
                            .sum::<u64>()
                            .try_into()?,
                    }),
                    Event::TransferMemo(memo.into()),
                ]
            }
            AccountTransactionEffects::CredentialKeysUpdated {
                cred_id,
            } => {
                vec![Event::CredentialKeysUpdated(CredentialKeysUpdated {
                    cred_id: cred_id.to_string(),
                })]
            }
            AccountTransactionEffects::CredentialsUpdated {
                new_cred_ids,
                removed_cred_ids,
                new_threshold,
            } => {
                vec![Event::CredentialsUpdated(CredentialsUpdated {
                    account_address:  details.sender.into(),
                    new_cred_ids:     new_cred_ids
                        .into_iter()
                        .map(|cred| cred.to_string())
                        .collect(),
                    removed_cred_ids: removed_cred_ids
                        .into_iter()
                        .map(|cred| cred.to_string())
                        .collect(),
                    new_threshold:    Byte(u8::from(new_threshold)),
                })]
            }
            AccountTransactionEffects::DataRegistered {
                data,
            } => {
                vec![Event::DataRegistered(DataRegistered {
                    data_as_hex: hex::encode(data.as_ref()),
                })]
            }
            AccountTransactionEffects::BakerConfigured {
                data,
            } => data
                .into_iter()
                .map(|baker_event| {
                    use concordium_rust_sdk::types::BakerEvent;
                    match baker_event {
                        BakerEvent::BakerAdded {
                            data,
                        } => Ok(Event::BakerAdded(BakerAdded {
                            staked_amount:    data.stake.micro_ccd.try_into()?,
                            restake_earnings: data.restake_earnings,
                            baker_id:         data.keys_event.baker_id.id.index.try_into()?,
                            sign_key:         serde_json::to_string(&data.keys_event.sign_key)?,
                            election_key:     serde_json::to_string(&data.keys_event.election_key)?,
                            aggregation_key:  serde_json::to_string(
                                &data.keys_event.aggregation_key,
                            )?,
                        })),
                        BakerEvent::BakerRemoved {
                            baker_id,
                        } => Ok(Event::BakerRemoved(BakerRemoved {
                            baker_id: baker_id.id.index.try_into()?,
                        })),
                        BakerEvent::BakerStakeIncreased {
                            baker_id,
                            new_stake,
                        } => Ok(Event::BakerStakeIncreased(BakerStakeIncreased {
                            baker_id:          baker_id.id.index.try_into()?,
                            new_staked_amount: new_stake.micro_ccd.try_into()?,
                        })),
                        BakerEvent::BakerStakeDecreased {
                            baker_id,
                            new_stake,
                        } => Ok(Event::BakerStakeDecreased(BakerStakeDecreased {
                            baker_id:          baker_id.id.index.try_into()?,
                            new_staked_amount: new_stake.micro_ccd.try_into()?,
                        })),
                        BakerEvent::BakerRestakeEarningsUpdated {
                            baker_id,
                            restake_earnings,
                        } => Ok(Event::BakerSetRestakeEarnings(BakerSetRestakeEarnings {
                            baker_id: baker_id.id.index.try_into()?,
                            restake_earnings,
                        })),
                        BakerEvent::BakerKeysUpdated {
                            data,
                        } => Ok(Event::BakerKeysUpdated(BakerKeysUpdated {
                            baker_id:        data.baker_id.id.index.try_into()?,
                            sign_key:        serde_json::to_string(&data.sign_key)?,
                            election_key:    serde_json::to_string(&data.election_key)?,
                            aggregation_key: serde_json::to_string(&data.aggregation_key)?,
                        })),
                        BakerEvent::BakerSetOpenStatus {
                            baker_id,
                            open_status,
                        } => Ok(Event::BakerSetOpenStatus(BakerSetOpenStatus {
                            baker_id:        baker_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                            open_status:     open_status.into(),
                        })),
                        BakerEvent::BakerSetMetadataURL {
                            baker_id,
                            metadata_url,
                        } => Ok(Event::BakerSetMetadataURL(BakerSetMetadataURL {
                            baker_id:        baker_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                            metadata_url:    metadata_url.into(),
                        })),
                        BakerEvent::BakerSetTransactionFeeCommission {
                            baker_id,
                            transaction_fee_commission,
                        } => Ok(Event::BakerSetTransactionFeeCommission(
                            BakerSetTransactionFeeCommission {
                                baker_id:                   baker_id.id.index.try_into()?,
                                account_address:            details.sender.into(),
                                transaction_fee_commission: transaction_fee_commission.into(),
                            },
                        )),
                        BakerEvent::BakerSetBakingRewardCommission {
                            baker_id,
                            baking_reward_commission,
                        } => Ok(Event::BakerSetBakingRewardCommission(
                            BakerSetBakingRewardCommission {
                                baker_id:                 baker_id.id.index.try_into()?,
                                account_address:          details.sender.into(),
                                baking_reward_commission: baking_reward_commission.into(),
                            },
                        )),
                        BakerEvent::BakerSetFinalizationRewardCommission {
                            baker_id,
                            finalization_reward_commission,
                        } => Ok(Event::BakerSetFinalizationRewardCommission(
                            BakerSetFinalizationRewardCommission {
                                baker_id: baker_id.id.index.try_into()?,
                                account_address: details.sender.into(),
                                finalization_reward_commission: finalization_reward_commission
                                    .into(),
                            },
                        )),
                        BakerEvent::DelegationRemoved {
                            delegator_id,
                        } => {
                            unimplemented!()
                        }
                    }
                })
                .collect::<anyhow::Result<Vec<Event>>>()?,
            AccountTransactionEffects::DelegationConfigured {
                data,
            } => {
                use concordium_rust_sdk::types::DelegationEvent;
                data.into_iter()
                    .map(|event| match event {
                        DelegationEvent::DelegationStakeIncreased {
                            delegator_id,
                            new_stake,
                        } => Ok(Event::DelegationStakeIncreased(DelegationStakeIncreased {
                            delegator_id:      delegator_id.id.index.try_into()?,
                            account_address:   details.sender.into(),
                            new_staked_amount: new_stake.micro_ccd().try_into()?,
                        })),
                        DelegationEvent::DelegationStakeDecreased {
                            delegator_id,
                            new_stake,
                        } => Ok(Event::DelegationStakeDecreased(DelegationStakeDecreased {
                            delegator_id:      delegator_id.id.index.try_into()?,
                            account_address:   details.sender.into(),
                            new_staked_amount: new_stake.micro_ccd().try_into()?,
                        })),
                        DelegationEvent::DelegationSetRestakeEarnings {
                            delegator_id,
                            restake_earnings,
                        } => {
                            Ok(Event::DelegationSetRestakeEarnings(DelegationSetRestakeEarnings {
                                delegator_id: delegator_id.id.index.try_into()?,
                                account_address: details.sender.into(),
                                restake_earnings,
                            }))
                        }
                        DelegationEvent::DelegationSetDelegationTarget {
                            delegator_id,
                            delegation_target,
                        } => Ok(Event::DelegationSetDelegationTarget(
                            DelegationSetDelegationTarget {
                                delegator_id:      delegator_id.id.index.try_into()?,
                                account_address:   details.sender.into(),
                                delegation_target: delegation_target.try_into()?,
                            },
                        )),
                        DelegationEvent::DelegationAdded {
                            delegator_id,
                        } => Ok(Event::DelegationAdded(DelegationAdded {
                            delegator_id:    delegator_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                        })),
                        DelegationEvent::DelegationRemoved {
                            delegator_id,
                        } => Ok(Event::DelegationRemoved(DelegationRemoved {
                            delegator_id:    delegator_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                        })),
                        DelegationEvent::BakerRemoved {
                            baker_id,
                        } => {
                            unimplemented!();
                        }
                    })
                    .collect::<anyhow::Result<Vec<_>>>()?
            }
        },
        BlockItemSummaryDetails::AccountCreation(details) => {
            vec![Event::AccountCreated(AccountCreated {
                account_address: details.address.into(),
            })]
        }
        BlockItemSummaryDetails::Update(details) => {
            vec![Event::ChainUpdateEnqueued(ChainUpdateEnqueued {
                effective_time: DateTime::from_timestamp(
                    details.effective_time.seconds.try_into()?,
                    0,
                )
                .context("Failed to parse effective time")?,
                payload:        true, // placeholder
            })]
        }
    };
    Ok(events)
}

impl TryFrom<concordium_rust_sdk::types::RejectReason> for TransactionRejectReason {
    type Error = anyhow::Error;

    fn try_from(reason: concordium_rust_sdk::types::RejectReason) -> Result<Self, Self::Error> {
        use concordium_rust_sdk::types::RejectReason;
        match reason {
            RejectReason::ModuleNotWF => Ok(TransactionRejectReason::ModuleNotWf(ModuleNotWf {
                dummy: true,
            })),
            RejectReason::ModuleHashAlreadyExists {
                contents,
            } => Ok(TransactionRejectReason::ModuleHashAlreadyExists(ModuleHashAlreadyExists {
                module_ref: contents.to_string(),
            })),
            RejectReason::InvalidAccountReference {
                contents,
            } => Ok(TransactionRejectReason::InvalidAccountReference(InvalidAccountReference {
                account_address: contents.into(),
            })),
            RejectReason::InvalidInitMethod {
                contents,
            } => Ok(TransactionRejectReason::InvalidInitMethod(InvalidInitMethod {
                module_ref: contents.0.to_string(),
                init_name:  contents.1.to_string(),
            })),
            RejectReason::InvalidReceiveMethod {
                contents,
            } => Ok(TransactionRejectReason::InvalidReceiveMethod(InvalidReceiveMethod {
                module_ref:   contents.0.to_string(),
                receive_name: contents.1.to_string(),
            })),
            RejectReason::InvalidModuleReference {
                contents,
            } => Ok(TransactionRejectReason::InvalidModuleReference(InvalidModuleReference {
                module_ref: contents.to_string(),
            })),
            RejectReason::InvalidContractAddress {
                contents,
            } => Ok(TransactionRejectReason::InvalidContractAddress(InvalidContractAddress {
                contract_address: contents.into(),
            })),
            RejectReason::RuntimeFailure => {
                Ok(TransactionRejectReason::RuntimeFailure(RuntimeFailure {
                    dummy: true,
                }))
            }
            RejectReason::AmountTooLarge {
                contents,
            } => Ok(TransactionRejectReason::AmountTooLarge(AmountTooLarge {
                address: contents.0.into(),
                amount:  contents.1.micro_ccd().try_into()?,
            })),
            RejectReason::SerializationFailure => {
                Ok(TransactionRejectReason::SerializationFailure(SerializationFailure {
                    dummy: true,
                }))
            }
            RejectReason::OutOfEnergy => Ok(TransactionRejectReason::OutOfEnergy(OutOfEnergy {
                dummy: true,
            })),
            RejectReason::RejectedInit {
                reject_reason,
            } => Ok(TransactionRejectReason::RejectedInit(RejectedInit {
                reject_reason,
            })),
            RejectReason::RejectedReceive {
                reject_reason,
                contract_address,
                receive_name,
                parameter,
            } => {
                Ok(TransactionRejectReason::RejectedReceive(RejectedReceive {
                    reject_reason,
                    contract_address: contract_address.into(),
                    receive_name: receive_name.to_string(),
                    message_as_hex: hex::encode(parameter.as_ref()),
                    // message: todo!(),
                }))
            }
            RejectReason::InvalidProof => Ok(TransactionRejectReason::InvalidProof(InvalidProof {
                dummy: true,
            })),
            RejectReason::AlreadyABaker {
                contents,
            } => Ok(TransactionRejectReason::AlreadyABaker(AlreadyABaker {
                baker_id: contents.id.index.try_into()?,
            })),
            RejectReason::NotABaker {
                contents,
            } => Ok(TransactionRejectReason::NotABaker(NotABaker {
                account_address: contents.into(),
            })),
            RejectReason::InsufficientBalanceForBakerStake => {
                Ok(TransactionRejectReason::InsufficientBalanceForBakerStake(
                    InsufficientBalanceForBakerStake {
                        dummy: true,
                    },
                ))
            }
            RejectReason::StakeUnderMinimumThresholdForBaking => {
                Ok(TransactionRejectReason::StakeUnderMinimumThresholdForBaking(
                    StakeUnderMinimumThresholdForBaking {
                        dummy: true,
                    },
                ))
            }
            RejectReason::BakerInCooldown => {
                Ok(TransactionRejectReason::BakerInCooldown(BakerInCooldown {
                    dummy: true,
                }))
            }
            RejectReason::DuplicateAggregationKey {
                contents,
            } => Ok(TransactionRejectReason::DuplicateAggregationKey(DuplicateAggregationKey {
                aggregation_key: serde_json::to_string(&contents)?,
            })),
            RejectReason::NonExistentCredentialID => {
                Ok(TransactionRejectReason::NonExistentCredentialId(NonExistentCredentialId {
                    dummy: true,
                }))
            }
            RejectReason::KeyIndexAlreadyInUse => {
                Ok(TransactionRejectReason::KeyIndexAlreadyInUse(KeyIndexAlreadyInUse {
                    dummy: true,
                }))
            }
            RejectReason::InvalidAccountThreshold => {
                Ok(TransactionRejectReason::InvalidAccountThreshold(InvalidAccountThreshold {
                    dummy: true,
                }))
            }
            RejectReason::InvalidCredentialKeySignThreshold => {
                Ok(TransactionRejectReason::InvalidCredentialKeySignThreshold(
                    InvalidCredentialKeySignThreshold {
                        dummy: true,
                    },
                ))
            }
            RejectReason::InvalidEncryptedAmountTransferProof => {
                Ok(TransactionRejectReason::InvalidEncryptedAmountTransferProof(
                    InvalidEncryptedAmountTransferProof {
                        dummy: true,
                    },
                ))
            }
            RejectReason::InvalidTransferToPublicProof => {
                Ok(TransactionRejectReason::InvalidTransferToPublicProof(
                    InvalidTransferToPublicProof {
                        dummy: true,
                    },
                ))
            }
            RejectReason::EncryptedAmountSelfTransfer {
                contents,
            } => Ok(TransactionRejectReason::EncryptedAmountSelfTransfer(
                EncryptedAmountSelfTransfer {
                    account_address: contents.into(),
                },
            )),
            RejectReason::InvalidIndexOnEncryptedTransfer => {
                Ok(TransactionRejectReason::InvalidIndexOnEncryptedTransfer(
                    InvalidIndexOnEncryptedTransfer {
                        dummy: true,
                    },
                ))
            }
            RejectReason::ZeroScheduledAmount => {
                Ok(TransactionRejectReason::ZeroScheduledAmount(ZeroScheduledAmount {
                    dummy: true,
                }))
            }
            RejectReason::NonIncreasingSchedule => {
                Ok(TransactionRejectReason::NonIncreasingSchedule(NonIncreasingSchedule {
                    dummy: true,
                }))
            }
            RejectReason::FirstScheduledReleaseExpired => {
                Ok(TransactionRejectReason::FirstScheduledReleaseExpired(
                    FirstScheduledReleaseExpired {
                        dummy: true,
                    },
                ))
            }
            RejectReason::ScheduledSelfTransfer {
                contents,
            } => Ok(TransactionRejectReason::ScheduledSelfTransfer(ScheduledSelfTransfer {
                account_address: contents.into(),
            })),
            RejectReason::InvalidCredentials => {
                Ok(TransactionRejectReason::InvalidCredentials(InvalidCredentials {
                    dummy: true,
                }))
            }
            RejectReason::DuplicateCredIDs {
                contents,
            } => Ok(TransactionRejectReason::DuplicateCredIds(DuplicateCredIds {
                cred_ids: contents.into_iter().map(|cred_id| cred_id.to_string()).collect(),
            })),
            RejectReason::NonExistentCredIDs {
                contents,
            } => Ok(TransactionRejectReason::NonExistentCredIds(NonExistentCredIds {
                cred_ids: contents.into_iter().map(|cred_id| cred_id.to_string()).collect(),
            })),
            RejectReason::RemoveFirstCredential => {
                Ok(TransactionRejectReason::RemoveFirstCredential(RemoveFirstCredential {
                    dummy: true,
                }))
            }
            RejectReason::CredentialHolderDidNotSign => Ok(
                TransactionRejectReason::CredentialHolderDidNotSign(CredentialHolderDidNotSign {
                    dummy: true,
                }),
            ),
            RejectReason::NotAllowedMultipleCredentials => {
                Ok(TransactionRejectReason::NotAllowedMultipleCredentials(
                    NotAllowedMultipleCredentials {
                        dummy: true,
                    },
                ))
            }
            RejectReason::NotAllowedToReceiveEncrypted => {
                Ok(TransactionRejectReason::NotAllowedToReceiveEncrypted(
                    NotAllowedToReceiveEncrypted {
                        dummy: true,
                    },
                ))
            }
            RejectReason::NotAllowedToHandleEncrypted => Ok(
                TransactionRejectReason::NotAllowedToHandleEncrypted(NotAllowedToHandleEncrypted {
                    dummy: true,
                }),
            ),
            RejectReason::MissingBakerAddParameters => {
                Ok(TransactionRejectReason::MissingBakerAddParameters(MissingBakerAddParameters {
                    dummy: true,
                }))
            }
            RejectReason::FinalizationRewardCommissionNotInRange => {
                Ok(TransactionRejectReason::FinalizationRewardCommissionNotInRange(
                    FinalizationRewardCommissionNotInRange {
                        dummy: true,
                    },
                ))
            }
            RejectReason::BakingRewardCommissionNotInRange => {
                Ok(TransactionRejectReason::BakingRewardCommissionNotInRange(
                    BakingRewardCommissionNotInRange {
                        dummy: true,
                    },
                ))
            }
            RejectReason::TransactionFeeCommissionNotInRange => {
                Ok(TransactionRejectReason::TransactionFeeCommissionNotInRange(
                    TransactionFeeCommissionNotInRange {
                        dummy: true,
                    },
                ))
            }
            RejectReason::AlreadyADelegator => {
                Ok(TransactionRejectReason::AlreadyADelegator(AlreadyADelegator {
                    dummy: true,
                }))
            }
            RejectReason::InsufficientBalanceForDelegationStake => {
                Ok(TransactionRejectReason::InsufficientBalanceForDelegationStake(
                    InsufficientBalanceForDelegationStake {
                        dummy: true,
                    },
                ))
            }
            RejectReason::MissingDelegationAddParameters => {
                Ok(TransactionRejectReason::MissingDelegationAddParameters(
                    MissingDelegationAddParameters {
                        dummy: true,
                    },
                ))
            }
            RejectReason::InsufficientDelegationStake => Ok(
                TransactionRejectReason::InsufficientDelegationStake(InsufficientDelegationStake {
                    dummy: true,
                }),
            ),
            RejectReason::DelegatorInCooldown => {
                Ok(TransactionRejectReason::DelegatorInCooldown(DelegatorInCooldown {
                    dummy: true,
                }))
            }
            RejectReason::NotADelegator {
                address,
            } => Ok(TransactionRejectReason::NotADelegator(NotADelegator {
                account_address: address.into(),
            })),
            RejectReason::DelegationTargetNotABaker {
                target,
            } => {
                Ok(TransactionRejectReason::DelegationTargetNotABaker(DelegationTargetNotABaker {
                    baker_id: target.id.index.try_into()?,
                }))
            }
            RejectReason::StakeOverMaximumThresholdForPool => {
                Ok(TransactionRejectReason::StakeOverMaximumThresholdForPool(
                    StakeOverMaximumThresholdForPool {
                        dummy: true,
                    },
                ))
            }
            RejectReason::PoolWouldBecomeOverDelegated => {
                Ok(TransactionRejectReason::PoolWouldBecomeOverDelegated(
                    PoolWouldBecomeOverDelegated {
                        dummy: true,
                    },
                ))
            }
            RejectReason::PoolClosed => Ok(TransactionRejectReason::PoolClosed(PoolClosed {
                dummy: true,
            })),
        }
    }
}

impl From<concordium_rust_sdk::types::Memo> for TransferMemo {
    fn from(value: concordium_rust_sdk::types::Memo) -> Self {
        TransferMemo {
            raw_hex: hex::encode(value.as_ref()),
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct Transferred {
    amount: Amount,
    from:   Address,
    to:     AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AccountCreated {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AmountAddedByDecryption {
    amount:          Amount,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerAdded {
    staked_amount:    Amount,
    restake_earnings: bool,
    baker_id:         BakerId,
    sign_key:         String,
    election_key:     String,
    aggregation_key:  String,
}
#[ComplexObject]
impl BakerAdded {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerKeysUpdated {
    baker_id:        BakerId,
    sign_key:        String,
    election_key:    String,
    aggregation_key: String,
}
#[ComplexObject]
impl BakerKeysUpdated {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerRemoved {
    baker_id: BakerId,
}
#[ComplexObject]
impl BakerRemoved {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerSetRestakeEarnings {
    baker_id:         BakerId,
    restake_earnings: bool,
}
#[ComplexObject]
impl BakerSetRestakeEarnings {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeDecreased {
    baker_id:          BakerId,
    new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeDecreased {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeIncreased {
    baker_id:          BakerId,
    new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeIncreased {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct ContractInitialized {
    module_ref:        String,
    contract_address:  ContractAddress,
    amount:            Amount,
    init_name:         String,
    version:           ContractVersion,
    // All logged events by the smart contract during the transaction execution.
    contract_logs_raw: Vec<Vec<u8>>,
}

#[ComplexObject]
impl ContractInitialized {
    async fn events_as_hex(&self) -> ApiResult<connection::Connection<String, String>> {
        let mut connection = connection::Connection::new(true, true);

        self.contract_logs_raw.iter().enumerate().for_each(|(index, log)| {
            connection.edges.push(connection::Edge::new(index.to_string(), hex::encode(log)));
        });

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }

    async fn events<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<connection::Connection<String, String>> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                contracts.module_reference as module_reference,
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the event schema if it exists.
        let opt_event_schema = if let Some(event_schema) = row.display_schema.as_ref() {
            let versioned_schema =
                VersionedModuleSchema::new(event_schema, &None).map_err(|_| {
                    ApiError::InternalError(
                        "Database bytes should be a valid VersionedModuleSchema".to_string(),
                    )
                })?;

            versioned_schema.get_event_schema(&row.contract_name).ok()
        } else {
            None
        };

        let mut connection = connection::Connection::new(true, true);

        for (index, log) in self.contract_logs_raw.iter().enumerate() {
            let decoded_log = decode_value_with_schema(
                opt_event_schema.as_ref(),
                log,
                SmartContractSchemaNames::Event,
            );

            connection.edges.push(connection::Edge::new(index.to_string(), decoded_log));
        }

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
pub enum ContractVersion {
    V0,
    V1,
}

impl From<concordium_rust_sdk::types::smart_contracts::WasmVersion> for ContractVersion {
    fn from(value: concordium_rust_sdk::types::smart_contracts::WasmVersion) -> Self {
        use concordium_rust_sdk::types::smart_contracts::WasmVersion;
        match value {
            WasmVersion::V0 => ContractVersion::V0,
            WasmVersion::V1 => ContractVersion::V1,
        }
    }
}

#[derive(Debug, thiserror::Error, Clone)]
#[error("Invalid contract version: {0}")]
pub struct InvalidContractVersionError(i32);

impl TryFrom<i32> for ContractVersion {
    type Error = InvalidContractVersionError;

    fn try_from(value: i32) -> Result<Self, Self::Error> {
        match value {
            0 => Ok(ContractVersion::V0),
            1 => Ok(ContractVersion::V1),
            _ => Err(InvalidContractVersionError(value)),
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractModuleDeployed {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractCall {
    contract_updated: ContractUpdated,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialDeployed {
    reg_id:          String,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialKeysUpdated {
    cred_id: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialsUpdated {
    account_address:  AccountAddress,
    new_cred_ids:     Vec<String>,
    removed_cred_ids: Vec<String>,
    new_threshold:    Byte,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct DataRegistered {
    data_as_hex: String,
}

#[ComplexObject]
impl DataRegistered {
    async fn decoded(&self) -> ApiResult<DecodedText> {
        let decoded_data = hex::decode(&self.data_as_hex).map_err(|e| {
            error!("Invalid hex encoding {:?} in a controlled environment", e);
            ApiError::InternalError("Failed to decode hex data".to_string())
        })?;

        Ok(DecodedText::from_bytes(decoded_data.as_slice()))
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DecodedText {
    text:        String,
    decode_type: TextDecodeType,
}

impl DecodedText {
    /// Attempt to parse the bytes as a CBOR string otherwise use HEX to present
    /// the bytes.
    fn from_bytes(bytes: &[u8]) -> Self {
        if let Ok(text) = ciborium::from_reader::<String, _>(bytes) {
            Self {
                text,
                decode_type: TextDecodeType::Cbor,
            }
        } else {
            Self {
                text:        hex::encode(bytes),
                decode_type: TextDecodeType::Hex,
            }
        }
    }
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
pub enum TextDecodeType {
    Cbor,
    Hex,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct EncryptedAmountsRemoved {
    account_address:      AccountAddress,
    new_encrypted_amount: String,
    input_amount:         String,
    up_to_index:          u64,
}

impl TryFrom<concordium_rust_sdk::types::EncryptedAmountRemovedEvent> for EncryptedAmountsRemoved {
    type Error = anyhow::Error;

    fn try_from(
        removed: concordium_rust_sdk::types::EncryptedAmountRemovedEvent,
    ) -> Result<Self, Self::Error> {
        Ok(EncryptedAmountsRemoved {
            account_address:      removed.account.into(),
            new_encrypted_amount: serde_json::to_string(&removed.new_amount)?,
            input_amount:         serde_json::to_string(&removed.input_amount)?,
            up_to_index:          removed.up_to_index.index,
        })
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct EncryptedSelfAmountAdded {
    account_address:      AccountAddress,
    new_encrypted_amount: String,
    amount:               Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NewEncryptedAmount {
    account_address:  AccountAddress,
    new_index:        u64,
    encrypted_amount: String,
}

impl TryFrom<concordium_rust_sdk::types::NewEncryptedAmountEvent> for NewEncryptedAmount {
    type Error = anyhow::Error;

    fn try_from(
        added: concordium_rust_sdk::types::NewEncryptedAmountEvent,
    ) -> Result<Self, Self::Error> {
        Ok(NewEncryptedAmount {
            account_address:  added.receiver.into(),
            new_index:        added.new_index.index,
            encrypted_amount: serde_json::to_string(&added.encrypted_amount)?,
        })
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct TransferMemo {
    raw_hex: String,
}

#[ComplexObject]
impl TransferMemo {
    async fn decoded(&self) -> ApiResult<DecodedText> {
        let decoded_data = hex::decode(&self.raw_hex).map_err(|e| {
            error!("Invalid hex encoding {:?} in a controlled environment", e);
            ApiError::InternalError("Failed to decode hex data".to_string())
        })?;

        Ok(DecodedText::from_bytes(decoded_data.as_slice()))
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct TransferredWithSchedule {
    from_account_address: AccountAddress,
    to_account_address:   AccountAddress,
    total_amount:         Amount,
    // TODO: amountsSchedule("Returns the first _n_ elements from the list." first: Int "Returns
    // the elements in the list that come after the specified cursor." after: String "Returns the
    // last _n_ elements from the list." last: Int "Returns the elements in the list that come
    // before the specified cursor." before: String): AmountsScheduleConnection
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ModuleReferenceEvent {
    module_reference: String,
    sender:           AccountAddress,
    block_height:     BlockHeight,
    transaction_hash: String,
    block_slot_time:  DateTime,
    display_schema:   Option<String>,
    // TODO:
    // moduleReferenceRejectEvents(skip: Int take: Int):
    // ModuleReferenceRejectEventsCollectionSegment moduleReferenceContractLinkEvents(skip: Int
    // take: Int): ModuleReferenceContractLinkEventsCollectionSegment linkedContracts(skip: Int
    // take: Int): LinkedContractsCollectionSegment
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdateEnqueued {
    effective_time: DateTime,
    // effective_immediately: bool, // Not sure this makes sense.
    payload:        bool, // ChainUpdatePayload,
}

// union ChainUpdatePayload = MinBlockTimeUpdate | TimeoutParametersUpdate |
// FinalizationCommitteeParametersUpdate | BlockEnergyLimitUpdate |
// GasRewardsCpv2Update | ProtocolChainUpdatePayload |
// ElectionDifficultyChainUpdatePayload | EuroPerEnergyChainUpdatePayload |
// MicroCcdPerEuroChainUpdatePayload | FoundationAccountChainUpdatePayload |
// MintDistributionChainUpdatePayload |
// TransactionFeeDistributionChainUpdatePayload | GasRewardsChainUpdatePayload |
// BakerStakeThresholdChainUpdatePayload | RootKeysChainUpdatePayload |
// Level1KeysChainUpdatePayload | AddAnonymityRevokerChainUpdatePayload |
// AddIdentityProviderChainUpdatePayload | CooldownParametersChainUpdatePayload
// | PoolParametersChainUpdatePayload | TimeParametersChainUpdatePayload |
// MintDistributionV1ChainUpdatePayload
#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ChainUpdatePayload {
    todo: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractInterrupted {
    contract_address:  ContractAddress,
    // All logged events by the smart contract during this section of the transaction execution.
    contract_logs_raw: Vec<Vec<u8>>,
}

#[ComplexObject]
impl ContractInterrupted {
    async fn events_as_hex(&self) -> ApiResult<connection::Connection<String, String>> {
        let mut connection = connection::Connection::new(true, true);

        self.contract_logs_raw.iter().enumerate().for_each(|(index, log)| {
            connection.edges.push(connection::Edge::new(index.to_string(), hex::encode(log)));
        });

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }

    async fn events<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<connection::Connection<String, String>> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                contracts.module_reference as module_reference,
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the event schema if it exists.
        let opt_event_schema = if let Some(event_schema) = row.display_schema.as_ref() {
            let versioned_schema =
                VersionedModuleSchema::new(event_schema, &None).map_err(|_| {
                    ApiError::InternalError(
                        "Database bytes should be a valid VersionedModuleSchema".to_string(),
                    )
                })?;

            versioned_schema.get_event_schema(&row.contract_name).ok()
        } else {
            None
        };

        let mut connection = connection::Connection::new(true, true);

        for (index, log) in self.contract_logs_raw.iter().enumerate() {
            let decoded_log = decode_value_with_schema(
                opt_event_schema.as_ref(),
                log,
                SmartContractSchemaNames::Event,
            );

            connection.edges.push(connection::Edge::new(index.to_string(), decoded_log));
        }

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractResumed {
    contract_address: ContractAddress,
    success:          bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct ContractUpdated {
    contract_address:  ContractAddress,
    instigator:        Address,
    amount:            Amount,
    receive_name:      String,
    version:           ContractVersion,
    // All logged events by the smart contract during this section of the transaction execution.
    contract_logs_raw: Vec<Vec<u8>>,
    input_parameter:   Vec<u8>,
}

#[ComplexObject]
impl ContractUpdated {
    async fn message_as_hex(&self) -> ApiResult<String> { Ok(hex::encode(&self.input_parameter)) }

    async fn message<'a>(&self, ctx: &Context<'a>) -> ApiResult<String> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                contracts.module_reference as module_reference,
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the receive param schema if it exists.
        let opt_receive_param_schema = if let Some(event_schema) = row.display_schema.as_ref() {
            let versioned_schema =
                VersionedModuleSchema::new(event_schema, &None).map_err(|_| {
                    ApiError::InternalError(
                        "Database bytes should be a valid VersionedModuleSchema".to_string(),
                    )
                })?;

            versioned_schema
                .get_receive_param_schema(
                    &row.contract_name,
                    ReceiveName::new_unchecked(&self.receive_name).entrypoint_name().into(),
                )
                .ok()
        } else {
            None
        };

        let decoded_input_parameter = decode_value_with_schema(
            opt_receive_param_schema.as_ref(),
            &self.input_parameter,
            SmartContractSchemaNames::InputParameterReceiveFunction,
        );

        Ok(decoded_input_parameter)
    }

    async fn events_as_hex(&self) -> ApiResult<connection::Connection<String, String>> {
        let mut connection = connection::Connection::new(true, true);

        self.contract_logs_raw.iter().enumerate().for_each(|(index, log)| {
            connection.edges.push(connection::Edge::new(index.to_string(), hex::encode(log)));
        });

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }

    async fn events<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<connection::Connection<String, String>> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            "
            SELECT
                contracts.module_reference as module_reference,
                name as contract_name,
                schema as display_schema
            FROM contracts
            JOIN smart_contract_modules ON smart_contract_modules.module_reference = \
             contracts.module_reference
            WHERE index = $1 AND sub_index = $2
            ",
            self.contract_address.index.0 as i64,
            self.contract_address.sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        // Get the event schema if it exists.
        let opt_event_schema = if let Some(event_schema) = row.display_schema.as_ref() {
            let versioned_schema =
                VersionedModuleSchema::new(event_schema, &None).map_err(|_| {
                    ApiError::InternalError(
                        "Database bytes should be a valid VersionedModuleSchema".to_string(),
                    )
                })?;

            versioned_schema.get_event_schema(&row.contract_name).ok()
        } else {
            None
        };

        let mut connection = connection::Connection::new(true, true);

        for (index, log) in self.contract_logs_raw.iter().enumerate() {
            let decoded_log = decode_value_with_schema(
                opt_event_schema.as_ref(),
                log,
                SmartContractSchemaNames::Event,
            );

            connection.edges.push(connection::Edge::new(index.to_string(), decoded_log));
        }

        // TODO: pagination info but not used at front-end currently (issue#318).

        Ok(connection)
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ContractUpgraded {
    contract_address: ContractAddress,
    from:             String,
    to:               String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetBakingRewardCommission {
    baker_id:                 BakerId,
    account_address:          AccountAddress,
    baking_reward_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetFinalizationRewardCommission {
    baker_id: BakerId,
    account_address: AccountAddress,
    finalization_reward_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetTransactionFeeCommission {
    baker_id:                   BakerId,
    account_address:            AccountAddress,
    transaction_fee_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetMetadataURL {
    baker_id:        BakerId,
    account_address: AccountAddress,
    metadata_url:    String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerSetOpenStatus {
    baker_id:        BakerId,
    account_address: AccountAddress,
    open_status:     BakerPoolOpenStatus,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationAdded {
    delegator_id:    AccountIndex,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationRemoved {
    delegator_id:    AccountIndex,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationSetDelegationTarget {
    delegator_id:      AccountIndex,
    account_address:   AccountAddress,
    delegation_target: DelegationTarget,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationSetRestakeEarnings {
    delegator_id:     AccountIndex,
    account_address:  AccountAddress,
    restake_earnings: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationStakeDecreased {
    delegator_id:      AccountIndex,
    account_address:   AccountAddress,
    new_staked_amount: Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationStakeIncreased {
    delegator_id:      AccountIndex,
    account_address:   AccountAddress,
    new_staked_amount: Amount,
}

#[derive(SimpleObject)]
struct BlockMetrics {
    /// The most recent block height. Equals the total length of the chain minus
    /// one (genesis block is at height zero).
    last_block_height: BlockHeight,
    /// Total number of blocks added in requested period.
    blocks_added: i64,
    /// The average block time in seconds (slot-time difference between two
    /// adjacent blocks) in the requested period. Will be null if no blocks
    /// have been added in the requested period.
    avg_block_time: Option<f64>,
    /// The average finalization time in seconds (slot-time difference between a
    /// given block and the block that holds its finalization proof) in the
    /// requested period. Will be null if no blocks have been finalized in
    /// the requested period.
    avg_finalization_time: Option<f64>,
    /// The current total amount of CCD in existence.
    last_total_micro_ccd: Amount,
    /// The total CCD Released. This is total CCD supply not counting the
    /// balances of non circulating accounts.
    last_total_micro_ccd_released: Amount,
    /// The current total CCD released according to the Concordium promise
    /// published on deck.concordium.com. Will be null for blocks with slot
    /// time before the published release schedule.
    last_total_micro_ccd_unlocked: Option<Amount>,
    /// The current total amount of CCD staked.
    last_total_micro_ccd_staked: Amount,
    buckets: BlockMetricsBuckets,
}

#[derive(SimpleObject)]
struct BlockMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,
    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,
    /// Number of blocks added within the bucket time period. Intended y-axis
    /// value.
    #[graphql(name = "y_BlocksAdded")]
    y_blocks_added: Vec<i64>,
    /// The average block time (slot-time difference between two adjacent
    /// blocks) in the bucket period. Intended y-axis value. Will be null if
    /// no blocks have been added in the bucket period.
    #[graphql(name = "y_BlockTimeAvg")]
    y_block_time_avg: Vec<f64>,
    /// The average finalization time (slot-time difference between a given
    /// block and the block that holds its finalization proof) in the bucket
    /// period. Intended y-axis value. Will be null if no blocks have been
    /// finalized in the bucket period.
    #[graphql(name = "y_FinalizationTimeAvg")]
    y_finalization_time_avg: Vec<f64>,
    /// The total amount of CCD staked at the end of the bucket period. Intended
    /// y-axis value.
    #[graphql(name = "y_LastTotalMicroCcdStaked")]
    y_last_total_micro_ccd_staked: Vec<Amount>,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum MetricsPeriod {
    LastHour,
    #[graphql(name = "LAST24_HOURS")]
    Last24Hours,
    #[graphql(name = "LAST7_DAYS")]
    Last7Days,
    #[graphql(name = "LAST30_DAYS")]
    Last30Days,
    #[graphql(name = "LAST_YEAR")]
    LastYear,
}

impl MetricsPeriod {
    /// The metrics period as a duration.
    fn as_duration(&self) -> Duration {
        match self {
            MetricsPeriod::LastHour => Duration::hours(1),
            MetricsPeriod::Last24Hours => Duration::hours(24),
            MetricsPeriod::Last7Days => Duration::days(7),
            MetricsPeriod::Last30Days => Duration::days(30),
            MetricsPeriod::LastYear => Duration::days(365),
        }
    }

    /// Duration used per bucket for a given metrics period.
    fn bucket_width(&self) -> Duration {
        match self {
            MetricsPeriod::LastHour => Duration::minutes(2),
            MetricsPeriod::Last24Hours => Duration::hours(1),
            MetricsPeriod::Last7Days => Duration::hours(6),
            MetricsPeriod::Last30Days => Duration::days(1),
            MetricsPeriod::LastYear => Duration::days(15),
        }
    }
}

#[derive(sqlx::Type, Copy, Clone)]
#[sqlx(type_name = "transaction_type")] // only for PostgreSQL to match a type definition
pub enum DbTransactionType {
    Account,
    CredentialDeployment,
    Update,
}
