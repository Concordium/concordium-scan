//! TODO
//! - Enable GraphiQL through flag instead of always.

mod account;
mod account_metrics;
mod baker;
mod baker_and_delegator_types;
mod baker_metrics;
mod block;
mod block_metrics;
mod contract;
mod module_reference_event;
pub mod node_status;
mod passive_delegation;
mod reward_metrics;
mod search_result;
mod stable_coin;
mod suspended_validators;
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
    connection::ConnectionQuery,
    graphql_api::search_result::SearchResult,
    migrations::{current_schema_version, SchemaVersion},
    scalar_types::{BlockHeight, DateTime, TimeSpan, UnsignedLong},
    transaction_event::smart_contracts::InvalidContractVersionError,
};
use anyhow::Context as _;
use async_graphql::{
    http::GraphiQLSource, types::connection, ComplexObject, Context, EmptyMutation, Enum,
    MergedObject, Object, SDLExportOptions, Schema, SimpleObject, Subscription, Union,
};
use async_graphql_axum::GraphQLSubscription;
use block::Block;

use chrono::{Duration, TimeDelta, Utc};
use concordium_rust_sdk::{
    base::contracts_common::schema::VersionedSchemaError, id::types as sdk_types,
};
use derive_more::Display;
use futures::prelude::*;
use node_status::NodeStatus;
use prometheus_client::registry::Registry;
use sqlx::PgPool;
use std::{error::Error, str::FromStr, sync::Arc};
use tokio::sync::{broadcast, watch::Receiver};
use tokio_stream::wrappers::errors::BroadcastStreamRecvError;
use tokio_util::sync::CancellationToken;
use tower_http::cors::{Any, CorsLayer};
use tracing::{error, info};
use transaction::Transaction;

const VERSION: &str = clap::crate_version!();

#[derive(Debug, clap::Args)]
pub struct ApiServiceConfig {
    /// Account(s) that should not be considered in circulation.
    #[arg(long, env = "CCDSCAN_API_CONFIG_NON_CIRCULATING_ACCOUNTS", value_delimiter = ',')]
    pub non_circulating_account: Vec<sdk_types::AccountAddress>,
    /// The most transactions which can be queried at once.
    #[arg(long, env = "CCDSCAN_API_CONFIG_TRANSACTION_CONNECTION_LIMIT", default_value = "100")]
    transaction_connection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_TRANSACTIONS_PER_BLOCK_CONNECTION_LIMIT",
        default_value = "100"
    )]
    transactions_per_block_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_BLOCK_CONNECTION_LIMIT", default_value = "100")]
    block_connection_limit: u64,
    #[arg(
        long,
        env = "CCDSCAN_API_CONFIG_SPECIAL_EVENTS_PER_BLOCK_CONNECTION_LIMIT",
        default_value = "100"
    )]
    special_events_per_block_connection_limit: u64,
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
    #[arg(long, env = "CCDSCAN_API_CONFIG_BAKER_CONNECTION_LIMIT", default_value = "100")]
    baker_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_CONTRACT_CONNECTION_LIMIT", default_value = "100")]
    contract_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_DELEGATORS_CONNECTION_LIMIT", default_value = "100")]
    delegators_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_POOL_REWARDS_CONNECTION_LIMIT", default_value = "100")]
    pool_rewards_connection_limit: u64,
    #[arg(long, env = "CCDSCAN_API_CONFIG_VALIDATORS_CONNECTION_LIMIT", default_value = "100")]
    validators_connection_limit: u64,
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
    #[arg(long, env = "CCDSCAN_API_CONFIG_TOKEN_EVENTS_COLLECTION_LIMIT", default_value = "100")]
    token_events_collection_limit: u64,
}

#[derive(MergedObject, Default)]
pub struct Query(
    BaseQuery,
    passive_delegation::QueryPassiveDelegation,
    suspended_validators::QuerySuspendedValidators,
    baker::QueryBaker,
    block::QueryBlocks,
    stable_coin::QueryStableCoins,
    transaction::QueryTransactions,
    account::QueryAccounts,
    module_reference_event::QueryModuleReferenceEvent,
    contract::QueryContract,
    node_status::QueryNodeStatus,
    token::QueryToken,
    account_metrics::QueryAccountMetrics,
    baker_metrics::QueryBakerMetrics,
    reward_metrics::QueryRewardMetrics,
    block_metrics::QueryBlockMetrics,
    transaction_metrics::QueryTransactionMetrics,
);

pub struct Service {
    schema: Schema<Query, EmptyMutation, Subscription>,
}
impl Service {
    pub fn new(
        subscription: Subscription,
        registry: &mut Registry,
        pool: PgPool,
        config: Arc<ApiServiceConfig>,
        receiver: Receiver<Option<Vec<NodeStatus>>>,
    ) -> Self {
        let schema = Schema::build(Query::default(), EmptyMutation, subscription)
            .extension(async_graphql::extensions::Tracing)
            .extension(monitor::MonitorExtension::new(registry))
            .data(receiver)
            .data(pool)
            .data(config)
            .finish();
        Self {
            schema,
        }
    }

    /// Construct the GraphQL Schema Definition Language used by the service.
    pub fn sdl() -> String {
        let (subscription, _) = Subscription::new(0);
        let schema = Schema::build(Query::default(), EmptyMutation, subscription).finish();
        schema.sdl_with_options(SDLExportOptions::new().prefer_single_line_descriptions())
    }

    /// Convert service into an axum router.
    pub fn as_router(self) -> axum::Router {
        let cors_layer = CorsLayer::new()
            .allow_origin(Any)  // Open access to selected route
            .allow_methods(Any)
            .allow_headers(Any);
        axum::Router::new()
            .route("/", axum::routing::get(Self::graphiql))
            .route(
                "/api/graphql",
                axum::routing::post_service(async_graphql_axum::GraphQL::new(self.schema.clone())),
            )
            .route_service("/ws/graphql", GraphQLSubscription::new(self.schema))
            .layer(cors_layer)
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
    #[error("Internal error (NoDatabasePool): {}", .0.message)]
    NoDatabasePool(async_graphql::Error),
    #[error("Internal error (NoServiceConfig): {}", .0.message)]
    NoServiceConfig(async_graphql::Error),
    #[error("Internal error: {}", .0.message)]
    NoReceiver(async_graphql::Error),
    #[error("Internal error (FailedDatabaseQuery): {0}")]
    FailedDatabaseQuery(Arc<sqlx::Error>),
    #[error("Invalid ID format: {0}")]
    InvalidIdInt(std::num::ParseIntError),
    #[error("Invalid cursor format: {0}")]
    InvalidCursorFormat(String),
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
    let config = ctx.data::<Arc<ApiServiceConfig>>().map_err(ApiError::NoServiceConfig)?;
    Ok(config.as_ref())
}

#[derive(Default)]
struct BaseQuery;

#[Object]
#[allow(clippy::too_many_arguments)]
impl BaseQuery {
    async fn versions(&self, ctx: &Context<'_>) -> ApiResult<Versions> {
        Ok(Versions {
            backend_version: VERSION.to_string(),
            database_schema_version: current_schema_version(get_pool(ctx)?)
                .await
                .map_err(|e| ApiError::InternalError(e.to_string()))?
                .to_string(),
            api_supported_database_schema_version: SchemaVersion::API_SUPPORTED_SCHEMA_VERSION
                .to_string(),
        })
    }

    async fn import_state<'a>(&self, ctx: &Context<'a>) -> ApiResult<ImportState> {
        let epoch_duration =
            sqlx::query_scalar!("SELECT epoch_duration FROM current_chain_parameters")
                .fetch_optional(get_pool(ctx)?)
                .await?
                .ok_or(ApiError::NotFound)?;

        Ok(ImportState {
            epoch_duration: TimeSpan(
                Duration::try_milliseconds(epoch_duration).ok_or(ApiError::InternalError(
                    "Epoch duration (epoch_duration) in the database should be a valid duration \
                     in milliseconds."
                        .to_string(),
                ))?,
            ),
        })
    }

    async fn latest_chain_parameters<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<LatestChainParameters> {
        let reward_period_length =
            sqlx::query_scalar!("SELECT reward_period_length FROM current_chain_parameters")
                .fetch_optional(get_pool(ctx)?)
                .await?
                .ok_or(ApiError::NotFound)?;

        // Future improvement (breaking changes): remove `ChainParametersV1` and just
        // use the `reward_period_length` from the current consensus algorithm
        // directly.
        Ok(LatestChainParameters::ChainParametersV1(ChainParametersV1 {
            reward_period_length: reward_period_length.try_into()?,
        }))
    }

    async fn payday_status<'a>(&self, ctx: &Context<'a>) -> ApiResult<PaydayStatus> {
        let row = sqlx::query_as!(
            CurrentChainParameters,
            "SELECT 
                reward_period_length, 
                epoch_duration, 
                last_payday_block_height as opt_last_payday_block_height,   
                slot_time as last_payday_block_slot_time
            FROM current_chain_parameters
                JOIN blocks 
                ON blocks.height = last_payday_block_height
            "
        )
        .fetch_optional(get_pool(ctx)?)
        .await?
        .ok_or(ApiError::NotFound)?;

        let payday_duration_milli_seconds = row.reward_period_length * row.epoch_duration;
        let next_payday_time = row
            .last_payday_block_slot_time
            .checked_add_signed(TimeDelta::milliseconds(payday_duration_milli_seconds))
            .ok_or(ApiError::InternalError("`Next_payday_time` should not overflow".to_string()))?;

        Ok(PaydayStatus {
            next_payday_time,
            opt_last_payday_block_height: row.opt_last_payday_block_height,
        })
    }

    async fn search(&self, query: String) -> SearchResult {
        SearchResult {
            query,
        }
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
struct ImportState {
    epoch_duration: TimeSpan,
}

// Future improvement (breaking changes): remove `ChainParametersV1` and just
// use the `reward_period_length` from the current consensus algorithm directly.
#[derive(Union)]
pub enum LatestChainParameters {
    ChainParametersV1(ChainParametersV1),
}

pub struct CurrentChainParameters {
    reward_period_length:         i64,
    epoch_duration:               i64,
    opt_last_payday_block_height: Option<i64>,
    last_payday_block_slot_time:  chrono::DateTime<Utc>,
}

#[derive(SimpleObject)]
#[graphql(complex)]
pub struct PaydayStatus {
    next_payday_time:             DateTime,
    #[graphql(skip)]
    opt_last_payday_block_height: Option<i64>,
}

#[ComplexObject]
impl PaydayStatus {
    // Future improvement (breaking changes): The front end only uses the
    // `lastPaydayBlock` which is returned here.
    // Return the `PaydaySummary` of the `lastPaydayBlock` directly without the
    // `payday_summaries` list.
    async fn payday_summaries(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, PaydaySummary>> {
        let mut connection = connection::Connection::new(false, false);

        let last_payday_block_height =
            self.opt_last_payday_block_height.ok_or(ApiError::InternalError(
                "Indexer should have recorded a payday block in database if it was running for at \
                 least the duration of a payday."
                    .to_string(),
            ))?;

        let payday_summary = PaydaySummary {
            block_height: last_payday_block_height,
        };

        connection
            .edges
            .push(connection::Edge::new(last_payday_block_height.to_string(), payday_summary));
        Ok(connection)
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
pub struct PaydaySummary {
    block_height: BlockHeight,
}

#[ComplexObject]
impl PaydaySummary {
    async fn block(&self, ctx: &Context<'_>) -> ApiResult<Block> {
        Ok(Block::query_by_height(get_pool(ctx)?, self.block_height).await?)
    }
}

#[derive(SimpleObject)]
pub struct ChainParametersV1 {
    pub reward_period_length: UnsignedLong,
}

#[derive(SimpleObject)]
struct Versions {
    backend_version: String,
    database_schema_version: String,
    api_supported_database_schema_version: String,
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
struct Ranking {
    rank:  i32,
    total: i32,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum ApyPeriod {
    #[graphql(name = "LAST7_DAYS")]
    Last7Days,
    #[graphql(name = "LAST30_DAYS")]
    Last30Days,
}
impl ApyPeriod {
    /// The metrics period as a duration.
    fn as_duration(&self) -> Duration {
        match self {
            Self::Last7Days => Duration::days(7),
            Self::Last30Days => Duration::days(30),
        }
    }
}
impl TryFrom<ApyPeriod> for sqlx::postgres::types::PgInterval {
    type Error = ApiError;

    fn try_from(value: ApyPeriod) -> Result<Self, Self::Error> {
        value.as_duration().try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))
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

#[derive(Debug, Enum, Clone, Copy, Display, PartialEq, Eq, sqlx::Type)]
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

/// A sort direction, either ascending or descending.
#[derive(Debug, Clone, Copy)]
enum OrderDir {
    Asc,
    Desc,
}
