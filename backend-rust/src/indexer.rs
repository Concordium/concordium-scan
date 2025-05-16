//! The main indexer service and configurations, spawning task for traversing
//! the blocks preprocessing each concurrently  and one task for processing each
//! block sequentially.

use anyhow::Context;
use concordium_rust_sdk::{indexer::TraverseConfig, types as sdk_types, v2};
use futures::StreamExt;
use prometheus_client::registry::Registry;
use sqlx::{postgres::PgConnectOptions, PgConnection};
use std::time::Duration;
use tokio_util::sync::CancellationToken;
use tracing::info;

mod block;
mod block_preprocessor;
mod block_processor;
mod db;
mod ensure_affected_rows;
mod genesis_data;
mod statistics;

pub use db::lock::acquire_indexer_lock;

#[derive(clap::Args)]
pub struct IndexerServiceConfig {
    /// Request timeout in seconds when querying a Concordium Node.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_NODE_REQUEST_TIMEOUT", default_value = "60")]
    pub node_request_timeout:             u64,
    /// Connection timeout in seconds when connecting a Concordium Node.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_NODE_CONNECT_TIMEOUT", default_value = "10")]
    pub node_connect_timeout:             u64,
    /// Acquire the indexer advisory lock timeout in seconds when connecting to
    /// the database.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_DATABASE_INDEXER_LOCK_TIMEOUT", default_value = "5")]
    pub database_indexer_lock_timeout:    u64,
    /// Maximum number of blocks being preprocessed in parallel.
    #[arg(
        long,
        env = "CCDSCAN_INDEXER_CONFIG_MAX_PARALLEL_BLOCK_PREPROCESSORS",
        default_value = "8"
    )]
    pub max_parallel_block_preprocessors: usize,
    /// Maximum number of blocks allowed to be batched into the same database
    /// transaction.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_MAX_PROCESSING_BATCH", default_value = "4")]
    pub max_processing_batch:             usize,
    /// Set the maximum amount of seconds the last finalized block of the node
    /// can be behind before it is deemed too far behind, and another node
    /// is tried.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_NODE_MAX_BEHIND", default_value = "60")]
    pub node_max_behind:                  u64,
    /// Enables rate limit on the number of requests send through
    /// each connection to the node.
    /// Provided as the number of requests per second.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_NODE_REQUEST_RATE_LIMIT")]
    pub node_request_rate_limit:          Option<u64>,
    /// Enables limit on the number of concurrent requests send through each
    /// connection to the node.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_NODE_REQUEST_CONCURRENCY_LIMIT")]
    pub node_request_concurrency_limit:   Option<usize>,
    /// Set the max number of acceptable successive failures before shutting
    /// down the service.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_MAX_SUCCESSIVE_FAILURES", default_value = "10")]
    pub max_successive_failures:          u32,
}

/// Service traversing each block of the chain, indexing it into a database.
///
/// The indexer purposefully performs insertions in a sequential manner, such
/// that table indices can be strictly increasing without skipping any values.
/// Since no rows are ever deleted, this allows using the table indices to
/// quickly calculate the number of rows in a table, without having to actually
/// count all rows via a table scan.
pub struct IndexerService {
    /// List of Concordium nodes to cycle through when traversing.
    endpoints:           Vec<v2::Endpoint>,
    /// The block height to traversing from.
    start_height:        u64,
    /// State tracked by the block preprocessor during traversing.
    block_pre_processor: block_preprocessor::BlockPreProcessor,
    /// State tracked by the block processor, which is submitting to the
    /// database.
    block_processor:     block_processor::BlockProcessor,
    config:              IndexerServiceConfig,
}

impl IndexerService {
    /// Construct the service. This reads the current state from the database.
    pub async fn new(
        endpoints: Vec<v2::Endpoint>,
        db_connect_options: PgConnectOptions,
        mut db_connection: PgConnection,
        registry: &mut Registry,
        config: IndexerServiceConfig,
    ) -> anyhow::Result<Self> {
        let database_indexer_lock_timeout =
            Duration::from_secs(config.database_indexer_lock_timeout);
        let last_height_stored = sqlx::query!(
            "
                SELECT height
                FROM blocks
                ORDER BY height
                DESC LIMIT 1
            "
        )
        .fetch_optional(db_connection.as_mut())
        .await?
        .map(|r| r.height);

        let start_height = if let Some(height) = last_height_stored {
            u64::try_from(height)? + 1
        } else {
            genesis_data::save_genesis_data(endpoints[0].clone(), db_connection.as_mut())
                .await
                .context("Failed initializing the database with the genesis block")?;
            1
        };
        let genesis_block_hash: sdk_types::hashes::BlockHash =
            sqlx::query!("SELECT hash FROM blocks WHERE height=0")
                .fetch_one(db_connection.as_mut())
                .await?
                .hash
                .parse()?;

        let block_pre_processor = block_preprocessor::BlockPreProcessor::new(
            genesis_block_hash,
            config.max_successive_failures.into(),
            registry.sub_registry_with_prefix("preprocessor"),
        );
        let block_processor = block_processor::BlockProcessor::new(
            db_connect_options,
            db_connection,
            database_indexer_lock_timeout,
            config.max_successive_failures,
            registry.sub_registry_with_prefix("processor"),
        )
        .await?;

        Ok(Self {
            endpoints,
            start_height,
            block_pre_processor,
            block_processor,
            config,
        })
    }

    /// Run the service. This future will only stop when signaled by the
    /// `cancel_token`.
    pub async fn run(self, cancel_token: CancellationToken) -> anyhow::Result<()> {
        let traverse_config = TraverseConfig::new(self.endpoints, self.start_height.into())
            .context("Failed setting up TraverseConfig")?
            .set_max_parallel(self.config.max_parallel_block_preprocessors)
            .set_max_behind(std::time::Duration::from_secs(self.config.node_max_behind));
        let processor_config = concordium_rust_sdk::indexer::ProcessorConfig::new()
            .set_stop_signal(cancel_token.cancelled_owned());

        let (sender, receiver) = tokio::sync::mpsc::channel(self.config.max_processing_batch);
        let receiver = tokio_stream::wrappers::ReceiverStream::from(receiver)
            .ready_chunks(self.config.max_processing_batch);
        let traverse_future =
            tokio::spawn(traverse_config.traverse(self.block_pre_processor, sender));
        let process_future =
            tokio::spawn(processor_config.process_event_stream(self.block_processor, receiver));
        info!("Indexing from block height {}", self.start_height);
        // Wait for both processes to exit, in case one of them results in an error,
        // wait for the other which then eventually will stop gracefully as either end
        // of their channel will get dropped.
        let (traverse_result, process_result) = futures::join!(traverse_future, process_future);
        process_result?;
        Ok(traverse_result??)
    }
}
