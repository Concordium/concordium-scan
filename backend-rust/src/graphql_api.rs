//! TODO
//! - Enable GraphiQL through flag instead of always.

#![allow(unused_variables)]

mod account_metrics;
mod baker;
mod block;
mod block_metrics;
mod contract;
mod module_reference_event;
mod token;
mod transaction;
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
    address::AccountAddress,
    scalar_types::{AccountIndex, Amount, BlockHeight, DateTime, TimeSpan, TransactionIndex},
    transaction_event::{
        delegation::{BakerDelegationTarget, DelegationTarget, PassiveDelegationTarget},
        smart_contracts::InvalidContractVersionError,
        Event,
    },
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use anyhow::{anyhow, Context as _};
use async_graphql::{
    http::GraphiQLSource,
    types::{self, connection},
    ComplexObject, Context, EmptyMutation, Enum, InputObject, MergedObject, Object, Schema,
    SimpleObject, Subscription, Union,
};
use async_graphql_axum::GraphQLSubscription;
use block::Block;
use chrono::Duration;
use concordium_rust_sdk::{
    base::contracts_common::schema::VersionedSchemaError, id::types as sdk_types,
};
use derive_more::Display;
use futures::prelude::*;
use prometheus_client::registry::Registry;
use sqlx::PgPool;
use std::{
    cmp::{max, min},
    error::Error,
    str::FromStr,
    sync::Arc,
};
use token::{AccountToken, Token};
use tokio::{net::TcpListener, sync::broadcast};
use tokio_stream::wrappers::errors::BroadcastStreamRecvError;
use tokio_util::sync::CancellationToken;
use tower_http::cors::{Any, CorsLayer};
use tracing::{error, info};
use transaction::Transaction;

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
        env = "CCDSCAN_API_CONFIG_ACCOUNT_STATEMENTS_CONNECTION_LIMIT",
        default_value = "100"
    )]
    account_statements_connection_limit: u64,
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
        env = "CCDSCAN_API_CONFIG_TRANSACTION_EVENT_CONNECTION_LIMIT",
        default_value = "100"
    )]
    transaction_event_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_TOKENS_CONNECTION_LIMIT", default_value = "100")]
    tokens_connection_limit: u64,
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
        env = "CCDSCAN_API_CONFIG_CONTRACT_REJECT_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    contract_reject_events_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_MODULE_REFERENCE_REJECT_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    module_reference_reject_events_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_MODULE_REFERENCE_LINKED_CONTRACTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    module_reference_linked_contracts_collection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_MODULE_REFERENCE_CONTRACT_LINK_EVENTS_COLLECTION_LIMIT",
        default_value = "100"
    )]
    module_reference_contract_link_events_collection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_REWARD_CONNECTION_LIMIT", default_value = "100")]
    reward_connection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_TOKEN_HOLDER_ADDRESSES_COLLECTION_LIMIT",
        default_value = "100"
    )]
    token_holder_addresses_collection_limit: u64,
}

#[derive(MergedObject, Default)]
pub struct Query(
    BaseQuery,
    baker::QueryBaker,
    block::QueryBlocks,
    transaction::QueryTransactions,
    account_metrics::QueryAccountMetrics,
    transaction_metrics::QueryTransactionMetrics,
    block_metrics::QueryBlockMetrics,
    module_reference_event::QueryModuleReferenceEvent,
    contract::QueryContract,
    token::QueryToken,
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

    async fn tokens(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Token>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;

        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.tokens_connection_limit,
        )?;

        let mut row_stream = sqlx::query_as!(
            Token,
            "SELECT * FROM (
                SELECT
                    index,
                    init_transaction_index,
                    total_supply as raw_total_supply,
                    token_id,
                    contract_index,
                    contract_sub_index,
                    token_address,
                    metadata_url
                FROM tokens
                WHERE tokens.index > $1 AND tokens.index < $2
                ORDER BY
                    (CASE WHEN $4 THEN tokens.index END) DESC,
                    (CASE WHEN NOT $4 THEN tokens.index END) ASC
                LIMIT $3
            ) AS token_data
            ORDER BY token_data.index DESC",
            query.from,
            query.to,
            query.limit,
            query.desc
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(true, true);
        while let Some(token) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(token.index.to_string(), token));
        }
        if last.is_some() {
            if let Some(edge) = connection.edges.last() {
                connection.has_previous_page = edge.node.index != 0;
            }
        } else if let Some(edge) = connection.edges.first() {
            connection.has_previous_page = edge.node.index != 0;
        }

        Ok(connection)
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
#[graphql(complex)]
pub struct AccountReward {
    #[graphql(skip)]
    id:           i64,
    #[graphql(skip)]
    block_height: BlockHeight,
    timestamp:    DateTime,
    #[graphql(skip)]
    entry_type:   AccountStatementEntryType,
    amount:       Amount,
}
#[ComplexObject]
impl AccountReward {
    async fn id(&self) -> types::ID { types::ID::from(self.id) }

    async fn block(&self, ctx: &Context<'_>) -> ApiResult<Block> {
        Block::query_by_height(get_pool(ctx)?, self.block_height).await
    }

    async fn reward_type(&self, ctx: &Context<'_>) -> ApiResult<RewardType> {
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
    amount:          Amount,
    account_balance: Amount,
    #[graphql(skip)]
    transaction_id:  Option<TransactionIndex>,
    #[graphql(skip)]
    block_height:    BlockHeight,
}

#[ComplexObject]
impl AccountStatementEntry {
    async fn id(&self) -> types::ID { types::ID::from(self.id) }

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

    delegated_restake_earnings: Option<bool>,
    delegated_target_baker_id:  Option<i64>, /* Get baker information if this account is baking.
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
            query.desc,
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
            query.desc,
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

#[derive(Enum, Clone, Copy, Display, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "account_statement_entry_type")]
pub enum AccountStatementEntryType {
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
    ) -> ApiResult<connection::Connection<String, contract::Contract>> {
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
    ) -> ApiResult<connection::Connection<String, token::Token>> {
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
    ) -> ApiResult<connection::Connection<String, baker::Baker>> {
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
