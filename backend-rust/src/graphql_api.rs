//! TODO
//! - Enable GraphiQL through flag instead of always.

#![allow(unused_variables)]

mod account_metrics;
mod block;
mod block_metrics;
mod transaction_metrics;

// TODO remove this macro, when done with first iteration
/// Short hand for returning API error with the message not implemented.
macro_rules! todo_api {
    () => {
        Err(crate::graphql_api::ApiError::InternalError(String::from("Not implemented")))
    };
}
pub(crate) use todo_api;

use crate::{
    address::{AccountAddress, ContractAddress, ContractIndex},
    scalar_types::{
        AccountIndex, Amount, BakerId, BigInteger, BlockHeight, DateTime, Decimal, Energy,
        MetadataUrl, ModuleReference, TimeSpan, TransactionHash, TransactionIndex,
    },
    transaction_event::{
        baker::BakerPoolOpenStatus,
        delegation::{BakerDelegationTarget, DelegationTarget, PassiveDelegationTarget},
        smart_contracts::InvalidContractVersionError,
        Event,
    },
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransaction, AccountTransactionType, CredentialDeploymentTransaction,
        CredentialDeploymentTransactionType, DbTransactionType, TransactionType, UpdateTransaction,
        UpdateTransactionType,
    },
};
use anyhow::Context as _;
use async_graphql::{
    http::GraphiQLSource,
    types::{self, connection},
    ComplexObject, Context, EmptyMutation, Enum, InputObject, MergedObject, Object, Schema,
    SimpleObject, Subscription, Union,
};
use async_graphql_axum::GraphQLSubscription;
use bigdecimal::BigDecimal;
use block::Block;
use chrono::Duration;
use concordium_rust_sdk::{
    base::contracts_common::schema::{VersionedModuleSchema, VersionedSchemaError},
    id::types as sdk_types,
    types::AmountFraction,
};
use futures::prelude::*;
use prometheus_client::registry::Registry;
use sqlx::PgPool;
use std::{error::Error, mem, str::FromStr, sync::Arc};
use tokio::{net::TcpListener, sync::broadcast};
use tokio_stream::wrappers::errors::BroadcastStreamRecvError;
use tokio_util::sync::CancellationToken;
use tower_http::cors::{Any, CorsLayer};
use tracing::{error, info};

const VERSION: &str = clap::crate_version!();

#[derive(clap::Args)]
pub struct ApiServiceConfig {
    /// Account(s) that should not be considered in circulation.
    #[arg(long, env = "CCDSCAN_API_CONFIG_NON_CIRCULATING_ACCOUNTS", value_delimiter = ',')]
    non_circulating_account: Vec<sdk_types::AccountAddress>,
    /// The most transactions which can be queried at once.
    #[arg(long, env = "CCDSCAN_API_CONFIG_TRANSACTION_CONNECTION_LIMIT", default_value = "100")]
    transaction_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_BLOCK_CONNECTION_LIMIT", default_value = "100")]
    block_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_ACCOUNT_CONNECTION_LIMIT", default_value = "100")]
    account_connection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_ACCOUNT_SCHEDULE_CONNECTION_LIMIT",
        default_value = "100"
    )]
    account_schedule_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_CONTRACT_CONNECTION_LIMIT", default_value = "100")]
    contract_connection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_CONTRACT_TOKENS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    contract_tokens_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_CONTRACT_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    contract_events_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_MODULE_REFERENCE_REJECT_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    module_reference_reject_events_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_MODULE_REFERENCE_CONTRACT_LINK_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    module_reference_contract_link_events_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_TRANSACTION_EVENT_CONNECTION_LIMIT",
        default_value = "100"
    )]
    transaction_event_connection_limit: u64,
}

#[derive(MergedObject, Default)]
pub struct Query(
    BaseQuery,
    block::Query,
    account_metrics::Query,
    transaction_metrics::Query,
    block_metrics::Query,
);

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
    impl futures::stream::Stream for WrappedStream<'_> {
        type Item = async_graphql::Response;

        fn poll_next(
            mut self: std::pin::Pin<&mut Self>,
            cx: &mut std::task::Context<'_>,
        ) -> std::task::Poll<Option<Self::Item>> {
            self.inner.poll_next_unpin(cx)
        }
    }
    impl std::ops::Drop for WrappedStream<'_> {
        fn drop(&mut self) { self.active_subscriptions.dec(); }
    }
}

/// All the errors that may be produced by the GraphQL API.
///
/// Note that `async_graphql` requires this to be `Clone`, as it is used as a
/// return type in queries. However, some of the underlying error types are not
/// `Clone`, so we wrap those in `Arc`s to make them `Clone`.
#[derive(Debug, thiserror::Error, Clone)]
pub enum ApiError {
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

pub type ApiResult<A> = Result<A, ApiError>;

/// Get the database pool from the context.
pub fn get_pool<'a>(ctx: &Context<'a>) -> ApiResult<&'a PgPool> {
    ctx.data::<PgPool>().map_err(ApiError::NoDatabasePool)
}

/// Get service configuration from the context.
pub fn get_config<'a>(ctx: &Context<'a>) -> ApiResult<&'a ApiServiceConfig> {
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
struct BaseQuery;

#[Object]
#[allow(clippy::too_many_arguments)]
impl BaseQuery {
    async fn versions(&self) -> Versions {
        Versions {
            backend_versions: VERSION.to_string(),
        }
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
                    (NOT $3 OR index           > $1 AND index           < $2) AND
                    (NOT $4 OR amount          > $1 AND amount          < $2) AND
                    (NOT $5 OR num_txs         > $1 AND num_txs         < $2) AND
                    (NOT $6 OR delegated_stake > $1 AND delegated_stake < $2) AND
                    -- Need to filter for only delegators if the user requests this.
                    (NOT $7 OR delegated_stake > 0)
                ORDER BY
                    -- Order by the field requested. Depending on the order of the collection
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
            -- We need to order each page still, as we only use the DESC/ASC ordering above
            -- to select page items from the start/end of the range.
            -- Each page must still independently be ordered.
            -- See also https://relay.dev/graphql/connections.htm#sec-Edge-order
            ORDER BY
                (CASE WHEN $3 AND $10     THEN index           END) DESC,
                (CASE WHEN $3 AND NOT $10 THEN index           END) ASC,
                (CASE WHEN $4 AND $10     THEN amount          END) DESC,
                (CASE WHEN $4 AND NOT $10 THEN amount          END) ASC,
                (CASE WHEN $5 AND $10     THEN num_txs         END) DESC,
                (CASE WHEN $5 AND NOT $10 THEN num_txs         END) ASC,
                (CASE WHEN $6 AND $10     THEN delegated_stake END) DESC,
                (CASE WHEN $6 AND NOT $10 THEN delegated_stake END) ASC",
            query.from,
            query.to,
            matches!(order.field, AccountOrderField::Age),
            matches!(order.field, AccountOrderField::Amount),
            matches!(order.field, AccountOrderField::TransactionCount),
            matches!(order.field, AccountOrderField::DelegatedStake),
            filter.map(|f| f.is_delegator).unwrap_or_default(),
            query.desc != matches!(order.dir, OrderDir::Desc),
            query.limit,
            matches!(order.dir, OrderDir::Desc),
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
    // String): TokensConnection

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
    block_added:      broadcast::Receiver<Block>,
    accounts_updated: broadcast::Receiver<AccountsUpdatedSubscriptionItem>,
}

impl Subscription {
    pub fn new(retry_delay_sec: u64) -> (Self, SubscriptionContext) {
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
                retry_delay_sec,
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
    retry_delay_sec:         u64,
}

impl SubscriptionContext {
    const ACCOUNTS_UPDATED_CHANNEL: &'static str = "account_updated";
    const BLOCK_ADDED_CHANNEL: &'static str = "block_added";

    pub async fn listen(self, pool: PgPool, stop_signal: CancellationToken) -> anyhow::Result<()> {
        loop {
            match self.run_listener(&pool, &stop_signal).await {
                Ok(_) => {
                    info!("PgListener stopped gracefully.");
                    break; // Graceful exit, stop the loop
                }
                Err(err) => {
                    error!("PgListener encountered an error: {}. Retrying...", err);

                    // Check if the stop signal has been triggered before retrying
                    if stop_signal.is_cancelled() {
                        info!("Stop signal received. Exiting PgListener loop.");
                        break;
                    }

                    tokio::time::sleep(std::time::Duration::from_secs(self.retry_delay_sec)).await;
                }
            }
        }

        Ok(())
    }

    async fn run_listener(
        &self,
        pool: &PgPool,
        stop_signal: &CancellationToken,
    ) -> anyhow::Result<()> {
        let mut listener = sqlx::postgres::PgListener::connect_with(pool)
            .await
            .context("Failed to create a PostgreSQL listener")?;

        listener
            .listen_all([Self::BLOCK_ADDED_CHANNEL, Self::ACCOUNTS_UPDATED_CHANNEL])
            .await
            .context("Failed to listen to PostgreSQL notifications")?;

        let exit = stop_signal
            .run_until_cancelled(async move {
                loop {
                    let notification = listener.recv().await?;
                    match notification.channel() {
                        Self::BLOCK_ADDED_CHANNEL => {
                            let block_height = BlockHeight::from_str(notification.payload())
                                .context("Failed to parse payload of block added")?;
                            let block = Block::query_by_height(pool, block_height).await?;
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

        // Handle early exit due to stop signal or errors
        if let Some(result) = exit {
            result.context("Failed while listening on database changes")?;
        }

        Ok(())
    }
}

#[derive(Clone, Debug, SimpleObject)]
pub struct AccountsUpdatedSubscriptionItem {
    address: String,
}

#[derive(SimpleObject)]
struct Versions {
    backend_versions: String,
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

    // This function fetches CIS2 tokens associated to a given contract, ordered by
    // their creation index in descending order. It retrieves the most recent
    // tokens first, with support for pagination through `skip` and `take`
    // parameters.
    // - `skip` determines how many of the most recent tokens to skip.
    // - `take` controls the number of tokens to return, respecting the collection
    //   limit.
    async fn tokens(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<TokensCollectionSegment> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;

        let max_token_index = sqlx::query_scalar!(
            "SELECT MAX(token_index_per_contract)
            FROM tokens
            WHERE tokens.contract_index = $1 AND tokens.contract_sub_index = $2",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0) as u64;

        let max_index = max_token_index.saturating_sub(skip.unwrap_or(0));
        let limit = i64::try_from(take.map_or(config.contract_tokens_collection_limit, |t| {
            config.contract_tokens_collection_limit.min(t)
        }))?;

        let mut items = sqlx::query_as!(
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
            WHERE tokens.contract_index = $1 AND tokens.contract_sub_index = $2
                AND tokens.token_index_per_contract <= $3
            ORDER BY tokens.token_index_per_contract DESC
            LIMIT $4
            ",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
            max_index as i64,
            limit as i64 + 1,
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = max_token_index > max_index;

        Ok(TokensCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count: items.len().try_into()?,
            items,
        })
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

type AccountReleaseScheduleItemIndex = i64;

struct AccountReleaseScheduleItem {
    /// Table index
    /// Used for the cursor in the connection
    index:             AccountReleaseScheduleItemIndex,
    transaction_index: TransactionIndex,
    timestamp:         DateTime,
    amount:            Amount,
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

    async fn amount(&self) -> Amount { self.amount }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct AccountToken {
    contract_index:     ContractIndex,
    contract_sub_index: ContractIndex,
    token_id:           String,
    #[graphql(skip)]
    raw_balance:        BigDecimal,
    token:              Token,
    account_id:         i64,
    account:            Account,
}

#[ComplexObject]
impl AccountToken {
    async fn balance(&self, ctx: &Context<'_>) -> ApiResult<BigInteger> {
        Ok(BigInteger::from(self.raw_balance.clone()))
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct Token {
    #[graphql(skip)]
    init_transaction_index: TransactionIndex,
    contract_index:         i64,
    contract_sub_index:     i64,
    token_id:               String,
    metadata_url:           Option<String>,
    #[graphql(skip)]
    raw_total_supply:       BigDecimal,
    token_address:          String,
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

struct Transaction {
    index: TransactionIndex,
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
    async fn query_by_index(pool: &PgPool, index: TransactionIndex) -> ApiResult<Option<Self>> {
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

    async fn transaction_index(&self) -> TransactionIndex { self.index }

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
    index: i64,
    /// Index of the transaction creating this account.
    /// Only `None` for genesis accounts.
    transaction_index: Option<i64>,
    /// The address of the account in Base58Check.
    #[sqlx(try_from = "String")]
    address: AccountAddress,
    /// The total amount of CCD hold by the account.
    amount: Amount,
    /// The total delegated stake of this account.
    delegated_stake: Amount,
    /// The total number of transactions this account has been involved in or
    /// affected by.
    num_txs: i64,
    delegated_restake_earnings: Option<bool>,
    delegated_target_baker_id: Option<i64>, /* Get baker information if this account is baking.
                                             * baker: Option<Baker>, */
}
impl Account {
    async fn query_by_index(pool: &PgPool, index: AccountIndex) -> ApiResult<Option<Self>> {
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

    async fn query_by_address(pool: &PgPool, address: String) -> ApiResult<Option<Self>> {
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
    async fn id(&self) -> types::ID { types::ID::from(self.index) }

    /// The address of the account in Base58Check.
    async fn address(&self) -> &AccountAddress { &self.address }

    /// The total amount of CCD hold by the account.
    async fn amount(&self) -> Amount { self.amount }

    async fn delegation(&self) -> Option<Delegation> {
        self.delegated_restake_earnings.map(|restake_earnings| Delegation {
            delegator_id: self.index,
            restake_earnings,
            staked_amount: self.delegated_stake,
            delegation_target: if let Some(target) = self.delegated_target_baker_id {
                DelegationTarget::BakerDelegationTarget(BakerDelegationTarget {
                    baker_id: target,
                })
            } else {
                DelegationTarget::PassiveDelegationTarget(PassiveDelegationTarget {
                    dummy: false,
                })
            },
        })
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
        Ok(total_amount.unwrap_or(0))
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
            query.desc,
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

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct ModuleReferenceEvent {
    module_reference: ModuleReference,
    sender:           AccountAddress,
    block_height:     BlockHeight,
    transaction_hash: TransactionHash,
    block_slot_time:  DateTime,
    display_schema:   Option<String>,
    // TODO: linkedContracts(skip: Int take: Int): LinkedContractsCollectionSegment
}
#[ComplexObject]
impl ModuleReferenceEvent {
    async fn module_reference_reject_events(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<ModuleReferenceRejectEventsCollectionSegment> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let min_index = i64::try_from(skip.unwrap_or(0))?;
        let limit = i64::try_from(
            take.map_or(config.module_reference_reject_events_collection_limit, |t| {
                config.module_reference_reject_events_collection_limit.min(t)
            }),
        )?;

        let mut items = sqlx::query_as!(
            ModuleReferenceRejectEvent,
            r#"SELECT
                module_reference,
                transactions.reject as "reject: sqlx::types::Json<TransactionRejectReason>",
                transactions.block_height,
                transactions.hash as transaction_hash,
                blocks.slot_time as block_slot_time
            FROM rejected_smart_contract_module_transactions
                JOIN transactions ON transaction_index = transactions.index
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE module_reference = $1
                AND rejected_smart_contract_module_transactions.index >= $2
            LIMIT $3
        "#,
            self.module_reference,
            min_index,
            limit + 1
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = min_index > 0;
        Ok(ModuleReferenceRejectEventsCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count: items.len().try_into()?,
            items,
        })
    }

    async fn module_reference_contract_link_events(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<ModuleReferenceContractLinkEventsCollectionSegment> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let min_index = i64::try_from(skip.unwrap_or(0))?;
        let limit = i64::try_from(
            take.map_or(config.module_reference_contract_link_events_collection_limit, |t| {
                config.module_reference_contract_link_events_collection_limit.min(t)
            }),
        )?;

        let mut items = sqlx::query_as!(
            ModuleReferenceContractLinkEvent,
            r#"SELECT
                link_action as "link_action: ModuleReferenceContractLinkAction",
                contract_index,
                contract_sub_index,
                transactions.hash as transaction_hash,
                blocks.slot_time as block_slot_time
            FROM link_smart_contract_module_transactions
                JOIN transactions ON transaction_index = transactions.index
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE module_reference = $1
                AND link_smart_contract_module_transactions.index >= $2
            LIMIT $3
        "#,
            self.module_reference,
            min_index,
            limit + 1
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = min_index > 0;

        Ok(ModuleReferenceContractLinkEventsCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count: items.len().try_into()?,
            items,
        })
    }
}

#[derive(SimpleObject)]
struct ModuleReferenceRejectEventsCollectionSegment {
    page_info:   CollectionSegmentInfo,
    items:       Vec<ModuleReferenceRejectEvent>,
    total_count: u64,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct ModuleReferenceRejectEvent {
    module_reference: ModuleReference,
    #[graphql(skip)]
    reject:           Option<sqlx::types::Json<TransactionRejectReason>>,
    block_height:     BlockHeight,
    transaction_hash: TransactionHash,
    block_slot_time:  DateTime,
}
#[ComplexObject]
impl ModuleReferenceRejectEvent {
    async fn rejected_event(&self) -> ApiResult<&TransactionRejectReason> {
        if let Some(sqlx::types::Json(reason)) = self.reject.as_ref() {
            Ok(reason)
        } else {
            Err(ApiError::InternalError(
                "ModuleReferenceRejectEvent: No reject reason found".to_string(),
            ))
        }
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct ModuleReferenceContractLinkEvent {
    block_slot_time:    DateTime,
    transaction_hash:   TransactionHash,
    link_action:        ModuleReferenceContractLinkAction,
    #[graphql(skip)]
    contract_index:     i64,
    #[graphql(skip)]
    contract_sub_index: i64,
}
#[ComplexObject]
impl ModuleReferenceContractLinkEvent {
    async fn contract_address(&self) -> ApiResult<ContractAddress> {
        ContractAddress::new(self.contract_index, self.contract_sub_index)
    }
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "module_reference_contract_link_action")]
pub enum ModuleReferenceContractLinkAction {
    Added,
    Removed,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ModuleReferenceContractLinkEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<ModuleReferenceContractLinkEvent>,
    total_count: u64,
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
