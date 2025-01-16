use crate::{
    graphql_api::AccountStatementEntryType,
    transaction_event::{
        baker::BakerPoolOpenStatus, events_from_summary,
        smart_contracts::ModuleReferenceContractLinkAction, Cis2BurnEvent, Cis2MintEvent,
        Cis2TokenMetadataEvent, Cis2TransferEvent, ScalarCis2TokenEvent,
    },
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use anyhow::Context;
use bigdecimal::BigDecimal;
use chrono::{DateTime, Utc};
use concordium_rust_sdk::{
    base::{
        contracts_common::to_bytes,
        smart_contracts::WasmVersion,
        transactions::{BlockItem, EncodedPayload, Payload},
    },
    cis0,
    cis2::{self, TokenAddress},
    common::types::{Address, Amount, Timestamp},
    indexer::{async_trait, Indexer, ProcessEvent, TraverseConfig, TraverseError},
    smart_contracts::engine::utils::{get_embedded_schema_v0, get_embedded_schema_v1},
    types::{
        self as sdk_types, queries::BlockInfo, AbsoluteBlockHeight, AccountStakingInfo,
        AccountTransactionDetails, AccountTransactionEffects, BlockItemSummary,
        BlockItemSummaryDetails, ContractAddress, ContractInitializedEvent, ContractTraceElement,
        DelegationTarget, PartsPerHundredThousands, RejectReason, RewardsOverview,
        SpecialTransactionOutcome, TransactionType,
    },
    v2::{
        self, BlockIdentifier, ChainParameters, FinalizedBlockInfo, QueryError, QueryResult,
        RPCError,
    },
};
use futures::{future::join_all, StreamExt, TryStreamExt};
use prometheus_client::{
    metrics::{
        counter::Counter,
        family::Family,
        gauge::Gauge,
        histogram::{self, Histogram},
    },
    registry::Registry,
};
use sqlx::PgPool;
use std::{convert::TryInto, sync::Arc};
use tokio::{time::Instant, try_join};
use tokio_util::sync::CancellationToken;
use tracing::{error, info, warn};

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
    block_pre_processor: BlockPreProcessor,
    /// State tracked by the block processor, which is submitting to the
    /// database.
    block_processor:     BlockProcessor,
    config:              IndexerServiceConfig,
}

#[derive(clap::Args)]
pub struct IndexerServiceConfig {
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
    /// Set the max number of acceptable successive failures before shutting
    /// down the service.
    #[arg(long, env = "CCDSCAN_INDEXER_CONFIG_MAX_SUCCESSIVE_FAILURES", default_value = "10")]
    pub max_successive_failures:          u32,
}

impl IndexerService {
    /// Construct the service. This reads the current state from the database.
    pub async fn new(
        endpoints: Vec<v2::Endpoint>,
        pool: PgPool,
        registry: &mut Registry,
        config: IndexerServiceConfig,
    ) -> anyhow::Result<Self> {
        let last_height_stored = sqlx::query!(
            "
SELECT height FROM blocks ORDER BY height DESC LIMIT 1
"
        )
        .fetch_optional(&pool)
        .await?
        .map(|r| r.height);

        let start_height = if let Some(height) = last_height_stored {
            u64::try_from(height)? + 1
        } else {
            save_genesis_data(endpoints[0].clone(), &pool).await?;
            1
        };
        let genesis_block_hash: sdk_types::hashes::BlockHash =
            sqlx::query!("SELECT hash FROM blocks WHERE height=0")
                .fetch_one(&pool)
                .await?
                .hash
                .parse()?;

        let block_pre_processor = BlockPreProcessor::new(
            genesis_block_hash,
            config.max_successive_failures.into(),
            registry.sub_registry_with_prefix("preprocessor"),
        );
        let block_processor = BlockProcessor::new(
            pool,
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
        // Set up endpoints to the node.
        let mut endpoints_with_schema = Vec::new();
        for endpoint in self.endpoints {
            if endpoint
                .uri()
                .scheme()
                .map_or(false, |x| x == &concordium_rust_sdk::v2::Scheme::HTTPS)
            {
                let new_endpoint = endpoint
                    .tls_config(tonic::transport::ClientTlsConfig::new())
                    .context("Unable to construct TLS configuration for the Concordium node.")?;
                endpoints_with_schema.push(new_endpoint);
            } else {
                endpoints_with_schema.push(endpoint);
            }
        }

        let traverse_config = TraverseConfig::new(endpoints_with_schema, self.start_height.into())
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

/// Represents the labels used for metrics related to Concordium Node.
#[derive(Clone, Debug, Hash, PartialEq, Eq, prometheus_client::encoding::EncodeLabelSet)]
struct NodeMetricLabels {
    /// URI of the node
    node: String,
}
impl NodeMetricLabels {
    fn new(endpoint: &v2::Endpoint) -> Self {
        Self {
            node: endpoint.uri().to_string(),
        }
    }
}

/// State tracked during block preprocessing, this also holds the implementation
/// of [`Indexer`](concordium_rust_sdk::indexer::Indexer). Since several
/// preprocessors can run in parallel, this must be `Sync`.
struct BlockPreProcessor {
    /// Genesis hash, used to ensure the nodes are on the expected network.
    genesis_hash:                 sdk_types::hashes::BlockHash,
    /// Metric counting the total number of connections ever established to a
    /// node.
    established_node_connections: Family<NodeMetricLabels, Counter>,
    /// Metric counting the total number of failed attempts to preprocess
    /// blocks.
    preprocessing_failures:       Family<NodeMetricLabels, Counter>,
    /// Metric tracking the number of blocks currently being preprocessed.
    blocks_being_preprocessed:    Family<NodeMetricLabels, Gauge>,
    /// Histogram collecting the time it takes for fetching all the block data
    /// from the node.
    node_response_time:           Family<NodeMetricLabels, Histogram>,
    /// Max number of acceptable successive failures before shutting down the
    /// service.
    max_successive_failures:      u64,
}
impl BlockPreProcessor {
    fn new(
        genesis_hash: sdk_types::hashes::BlockHash,
        max_successive_failures: u64,
        registry: &mut Registry,
    ) -> Self {
        let established_node_connections = Family::default();
        registry.register(
            "established_node_connections",
            "Total number of established Concordium Node connections",
            established_node_connections.clone(),
        );
        let preprocessing_failures = Family::default();
        registry.register(
            "preprocessing_failures",
            "Total number of failed attempts to preprocess blocks",
            preprocessing_failures.clone(),
        );
        let blocks_being_preprocessed = Family::default();
        registry.register(
            "blocks_being_preprocessed",
            "Current number of blocks being preprocessed",
            blocks_being_preprocessed.clone(),
        );
        let node_response_time: Family<NodeMetricLabels, Histogram> =
            Family::new_with_constructor(|| {
                Histogram::new(histogram::exponential_buckets(0.010, 2.0, 10))
            });
        registry.register(
            "node_response_time_seconds",
            "Duration of seconds used to fetch all of the block information",
            node_response_time.clone(),
        );

        Self {
            genesis_hash,
            established_node_connections,
            preprocessing_failures,
            blocks_being_preprocessed,
            node_response_time,
            max_successive_failures,
        }
    }
}
#[async_trait]
impl Indexer for BlockPreProcessor {
    type Context = NodeMetricLabels;
    type Data = PreparedBlock;

    /// Called when a new connection is established to the given endpoint.
    /// The return value from this method is passed to each call of
    /// on_finalized.
    async fn on_connect<'a>(
        &mut self,
        endpoint: v2::Endpoint,
        client: &'a mut v2::Client,
    ) -> QueryResult<Self::Context> {
        let info = client.get_consensus_info().await?;
        if info.genesis_block != self.genesis_hash {
            error!(
                "Invalid client: {} is on network with genesis hash {} expected {}",
                endpoint.uri(),
                info.genesis_block,
                self.genesis_hash
            );
            return Err(QueryError::RPCError(RPCError::CallError(
                tonic::Status::failed_precondition(format!(
                    "Invalid client: {} is on network with genesis hash {} expected {}",
                    endpoint.uri(),
                    info.genesis_block,
                    self.genesis_hash
                )),
            )));
        }
        info!("Connection established to node at uri: {}", endpoint.uri());
        let label = NodeMetricLabels::new(&endpoint);
        self.established_node_connections.get_or_create(&label).inc();
        Ok(label)
    }

    /// The main method of this trait. It is called for each finalized block
    /// that the indexer discovers. Note that the indexer might call this
    /// concurrently for multiple blocks at the same time to speed up indexing.
    ///
    /// This method is meant to return errors that are unexpected, and if it
    /// does return an error the indexer will attempt to reconnect to the
    /// next endpoint.
    async fn on_finalized<'a>(
        &self,
        mut client: v2::Client,
        label: &'a Self::Context,
        fbi: FinalizedBlockInfo,
    ) -> QueryResult<Self::Data> {
        self.blocks_being_preprocessed.get_or_create(label).inc();
        // We block together the computation, so we can update the metric in the error
        // case, before returning early.
        let result =
            async move {
                let mut client1 = client.clone();
                let mut client2 = client.clone();
                let mut client3 = client.clone();
                let mut client4 = client.clone();
                let mut client5 = client.clone();
                let get_events = async move {
                    let events = client3
                        .get_block_transaction_events(fbi.height)
                        .await?
                        .response
                        .try_collect::<Vec<_>>()
                        .await?;
                    Ok(events)
                };

                let get_items = async move {
                    let items = client4
                        .get_block_items(fbi.height)
                        .await?
                        .response
                        .try_collect::<Vec<_>>()
                        .await?;
                    Ok(items)
                };

                let get_special_items = async move {
                    let items = client5
                        .get_block_special_events(fbi.height)
                        .await?
                        .response
                        .try_collect::<Vec<_>>()
                        .await?;
                    Ok(items)
                };

                let start_fetching = Instant::now();
                let (block_info, chain_parameters, events, items, tokenomics_info, special_events) =
                    try_join!(
                        client1.get_block_info(fbi.height),
                        client2.get_block_chain_parameters(fbi.height),
                        get_events,
                        get_items,
                        client.get_tokenomics_info(fbi.height),
                        get_special_items
                    )?;
                let total_staked_capital = match tokenomics_info.response {
                    RewardsOverview::V0 {
                        ..
                    } => {
                        compute_total_stake_capital(
                            &mut client,
                            BlockIdentifier::AbsoluteHeight(fbi.height),
                        )
                        .await?
                    }
                    RewardsOverview::V1 {
                        total_staked_capital,
                        ..
                    } => total_staked_capital,
                };
                let node_response_time = start_fetching.elapsed();
                self.node_response_time
                    .get_or_create(label)
                    .observe(node_response_time.as_secs_f64());

                let data = BlockData {
                    finalized_block_info: fbi,
                    block_info: block_info.response,
                    events,
                    items,
                    chain_parameters: chain_parameters.response,
                    tokenomics_info: tokenomics_info.response,
                    total_staked_capital,
                    special_events,
                };

                let prepared_block = PreparedBlock::prepare(&mut client, &data)
                    .await
                    .map_err(RPCError::ParseError)?;
                Ok(prepared_block)
            }
            .await;
        self.blocks_being_preprocessed.get_or_create(label).dec();
        result
    }

    /// Called when either connecting to the node or querying the node fails.
    /// The number of successive failures without progress is passed to the
    /// method which should return whether to stop indexing `true` or not
    /// `false`
    async fn on_failure(
        &mut self,
        endpoint: v2::Endpoint,
        successive_failures: u64,
        err: TraverseError,
    ) -> bool {
        info!("Failed preprocessing {} times in row: {}", successive_failures, err);
        self.preprocessing_failures.get_or_create(&NodeMetricLabels::new(&endpoint)).inc();
        successive_failures > self.max_successive_failures
    }
}

/// Compute the total stake capital by summing all the stake of the bakers.
/// This is only needed for older blocks, which does not provide this
/// information as part of the tokenomics info query.
async fn compute_total_stake_capital(
    client: &mut v2::Client,
    block_height: v2::BlockIdentifier,
) -> QueryResult<Amount> {
    let mut total_staked_capital = Amount::zero();
    let mut bakers = client.get_baker_list(block_height).await?.response;
    while let Some(baker_id) = bakers.try_next().await? {
        let account_info = client
            .get_account_info(&v2::AccountIdentifier::Index(baker_id.id), block_height)
            .await?
            .response;
        total_staked_capital += account_info
            .account_stake
            .context("Expected baker to have account stake information")
            .map_err(RPCError::ParseError)?
            .staked_amount();
    }
    Ok(total_staked_capital)
}

/// Type implementing the `ProcessEvent` handling the insertion of prepared
/// blocks.
struct BlockProcessor {
    /// Database connection pool
    pool: PgPool,
    /// Metric counting how many blocks was saved to the database successfully.
    blocks_processed: Counter,
    /// Metric counting the total number of failed attempts to process
    /// blocks.
    processing_failures: Counter,
    /// Histogram collecting the time it took to process a block.
    processing_duration_seconds: Histogram,
    /// Max number of acceptable successive failures before shutting down the
    /// service.
    max_successive_failures: u32,
    /// Starting context which is tracked across processing blocks.
    current_context: BlockProcessingContext,
}
impl BlockProcessor {
    /// Construct the block processor by loading the initial state from the
    /// database. This assumes at least the genesis block is in the
    /// database.
    async fn new(
        pool: PgPool,
        max_successive_failures: u32,
        registry: &mut Registry,
    ) -> anyhow::Result<Self> {
        let last_finalized_block = sqlx::query!(
            "
SELECT
  hash
FROM blocks
WHERE finalization_time IS NOT NULL
ORDER BY height DESC
LIMIT 1
"
        )
        .fetch_one(&pool)
        .await
        .context("Failed to query data for save context")?;

        let last_block = sqlx::query!(
            "
SELECT
  slot_time,
  cumulative_num_txs
FROM blocks
ORDER BY height DESC
LIMIT 1
"
        )
        .fetch_one(&pool)
        .await
        .context("Failed to query data for save context")?;

        let starting_context = BlockProcessingContext {
            last_finalized_hash:     last_finalized_block.hash,
            last_block_slot_time:    last_block.slot_time,
            last_cumulative_num_txs: last_block.cumulative_num_txs,
        };

        let blocks_processed = Counter::default();
        registry.register(
            "blocks_processed",
            "Number of blocks save to the database",
            blocks_processed.clone(),
        );
        let processing_failures = Counter::default();
        registry.register(
            "processing_failures",
            "Number of blocks save to the database",
            processing_failures.clone(),
        );
        let processing_duration_seconds =
            Histogram::new(histogram::exponential_buckets(0.01, 2.0, 10));
        registry.register(
            "processing_duration_seconds",
            "Time taken for processing a block",
            processing_duration_seconds.clone(),
        );

        Ok(Self {
            pool,
            current_context: starting_context,
            blocks_processed,
            processing_failures,
            processing_duration_seconds,
            max_successive_failures,
        })
    }
}

#[async_trait]
impl ProcessEvent for BlockProcessor {
    /// The type of events that are to be processed. Typically this will be all
    /// of the transactions of interest for a single block."]
    type Data = Vec<PreparedBlock>;
    /// A description returned by the [`process`](ProcessEvent::process) method.
    /// This message is logged by the [`ProcessorConfig`] and is intended to
    /// describe the data that was just processed.
    type Description = String;
    /// An error that can be signalled.
    type Error = anyhow::Error;

    /// Process a single item. This should work atomically in the sense that
    /// either the entire `data` is processed or none of it is in case of an
    /// error. This property is relied upon by the [`ProcessorConfig`] to retry
    /// failed attempts.
    async fn process(&mut self, batch: &Self::Data) -> Result<Self::Description, Self::Error> {
        let start_time = Instant::now();
        let mut out = format!("Processed {} blocks:", batch.len());
        let mut tx = self.pool.begin().await.context("Failed to create SQL transaction")?;
        // Clone the context, to avoid mutating the current context until we are certain
        // nothing fails.
        let mut new_context = self.current_context.clone();
        PreparedBlock::batch_save(batch, &mut new_context, &mut tx).await?;
        for block in batch {
            for item in block.prepared_block_items.iter() {
                item.save(&mut tx).await?;
            }
            for item in block.special_items.iter() {
                item.save(&mut tx, None).await?;
            }
            out.push_str(format!("\n- {}:{}", block.height, block.hash).as_str())
        }
        process_release_schedules(new_context.last_block_slot_time, &mut tx)
            .await
            .context("Processing scheduled releases")?;
        tx.commit().await.context("Failed to commit SQL transaction")?;
        // Update metrics.
        let duration = start_time.elapsed();
        self.processing_duration_seconds.observe(duration.as_secs_f64() / batch.len() as f64);
        self.blocks_processed.inc_by(u64::try_from(batch.len())?);
        // Update the current context when we are certain that nothing failed during
        // processing.
        self.current_context = new_context;
        Ok(out)
    }

    /// The `on_failure` method is invoked by the [`ProcessorConfig`] when it
    /// fails to process an event. It is meant to retry to recreate the
    /// resources, such as a database connection, that might have been
    /// dropped. The return value should signal if the handler process
    /// should continue (`true`) or not.
    ///
    /// The function takes the `error` that occurred at the latest
    /// [`process`](Self::process) call that just failed, and the number of
    /// attempts of calling `process` that failed.
    async fn on_failure(
        &mut self,
        error: Self::Error,
        successive_failures: u32,
    ) -> Result<bool, Self::Error> {
        info!("Failed processing {} times in row: \n{:?}", successive_failures, error);
        self.processing_failures.inc();
        Ok(self.max_successive_failures >= successive_failures)
    }
}

#[derive(Clone)]
struct BlockProcessingContext {
    /// The last finalized block hash according to the latest indexed block.
    /// This is used when computing the finalization time.
    last_finalized_hash:     String,
    /// The slot time of the last processed block.
    /// This is used when computing the block time.
    last_block_slot_time:    DateTime<Utc>,
    /// The value of cumulative_num_txs from the last block.
    /// This, along with the number of transactions in the current block,
    /// is used to calculate the next cumulative_num_txs.
    last_cumulative_num_txs: i64,
}

/// Process schedule releases based on the slot time of the last processed
/// block.
async fn process_release_schedules(
    last_block_slot_time: DateTime<Utc>,
    tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
) -> anyhow::Result<()> {
    sqlx::query!(
        "DELETE FROM scheduled_releases
         WHERE release_time <= $1",
        last_block_slot_time
    )
    .execute(tx.as_mut())
    .await?;
    Ok(())
}

/// Raw block information fetched from a Concordium Node.
struct BlockData {
    finalized_block_info: FinalizedBlockInfo,
    block_info:           BlockInfo,
    events:               Vec<BlockItemSummary>,
    items:                Vec<BlockItem<EncodedPayload>>,
    chain_parameters:     ChainParameters,
    tokenomics_info:      RewardsOverview,
    total_staked_capital: Amount,
    special_events:       Vec<SpecialTransactionOutcome>,
}

/// Function for initializing the database with the genesis block.
/// This should only be called if the database is empty.
async fn save_genesis_data(endpoint: v2::Endpoint, pool: &PgPool) -> anyhow::Result<()> {
    let mut client = v2::Client::new(endpoint).await?;
    let mut tx = pool.begin().await.context("Failed to create SQL transaction")?;
    let genesis_height = v2::BlockIdentifier::AbsoluteHeight(0.into());
    {
        let genesis_block_info = client.get_block_info(genesis_height).await?.response;
        let block_hash = genesis_block_info.block_hash.to_string();
        let slot_time = genesis_block_info.block_slot_time;
        let genesis_tokenomics = client.get_tokenomics_info(genesis_height).await?.response;
        let total_staked = match genesis_tokenomics {
            RewardsOverview::V0 {
                ..
            } => {
                let total_staked_capital =
                    compute_total_stake_capital(&mut client, genesis_height).await?;
                i64::try_from(total_staked_capital.micro_ccd())?
            }
            RewardsOverview::V1 {
                total_staked_capital,
                ..
            } => i64::try_from(total_staked_capital.micro_ccd())?,
        };
        let total_amount =
            i64::try_from(genesis_tokenomics.common_reward_data().total_amount.micro_ccd())?;
        sqlx::query!(
            "INSERT INTO blocks (
                height,
                hash,
                slot_time,
                block_time,
                finalization_time,
                total_amount,
                total_staked,
                cumulative_num_txs
            ) VALUES (0, $1, $2, 0, 0, $3, $4, 0);",
            block_hash,
            slot_time,
            total_amount,
            total_staked,
        )
        .execute(&mut *tx)
        .await?;
    }

    let mut genesis_accounts = client.get_account_list(genesis_height).await?.response;
    while let Some(account) = genesis_accounts.try_next().await? {
        let info = client.get_account_info(&account.into(), genesis_height).await?.response;
        let index = i64::try_from(info.account_index.index)?;
        let account_address = account.to_string();
        let amount = i64::try_from(info.account_amount.micro_ccd)?;

        // Note that we override the usual default num_txs = 1 here
        // because the genesis accounts do not have a creation transaction.
        sqlx::query!(
            "INSERT INTO accounts (index, address, amount, num_txs)
            VALUES ($1, $2, $3, 0)",
            index,
            account_address,
            amount,
        )
        .execute(&mut *tx)
        .await?;

        if let Some(AccountStakingInfo::Baker {
            staked_amount,
            restake_earnings,
            baker_info: _,
            pending_change: _,
            pool_info,
        }) = info.account_stake
        {
            let stake = i64::try_from(staked_amount.micro_ccd())?;
            let open_status = pool_info.as_ref().map(|i| BakerPoolOpenStatus::from(i.open_status));
            let metadata_url = pool_info.as_ref().map(|i| i.metadata_url.to_string());
            let transaction_commission = pool_info.as_ref().map(|i| {
                i64::from(u32::from(PartsPerHundredThousands::from(i.commission_rates.transaction)))
            });
            let baking_commission = pool_info.as_ref().map(|i| {
                i64::from(u32::from(PartsPerHundredThousands::from(i.commission_rates.baking)))
            });
            let finalization_commission = pool_info.as_ref().map(|i| {
                i64::from(u32::from(PartsPerHundredThousands::from(
                    i.commission_rates.finalization,
                )))
            });
            sqlx::query!(
                "INSERT INTO bakers (id, staked, restake_earnings, open_status, metadata_url, \
                 transaction_commission, baking_commission, finalization_commission)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8)",
                index,
                stake,
                restake_earnings,
                open_status as Option<BakerPoolOpenStatus>,
                metadata_url,
                transaction_commission,
                baking_commission,
                finalization_commission
            )
            .execute(&mut *tx)
            .await?;
        }
    }

    tx.commit().await.context("Failed to commit SQL transaction")?;
    Ok(())
}

/// Preprocessed block which is ready to be saved in the database.
struct PreparedBlock {
    /// Hash of the block.
    hash:                 String,
    /// Absolute height of the block.
    height:               i64,
    /// Block slot time (UTC).
    slot_time:            DateTime<Utc>,
    /// Id of the validator which constructed the block. Is only None for the
    /// genesis block.
    baker_id:             Option<i64>,
    /// Total amount of CCD in existence at the time of this block.
    total_amount:         i64,
    /// Total staked CCD at the time of this block.
    total_staked:         i64,
    /// Block hash of the last finalized block.
    block_last_finalized: String,
    /// Preprocessed block items, ready to be saved in the database.
    prepared_block_items: Vec<PreparedBlockItem>,
    /// Preprocessed block special items, ready to be saved in the database.
    special_items:        Vec<PreparedAccountStatement>,
}

impl PreparedBlock {
    async fn prepare(node_client: &mut v2::Client, data: &BlockData) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let hash = data.finalized_block_info.block_hash.to_string();
        let block_last_finalized = data.block_info.block_last_finalized.to_string();
        let slot_time = data.block_info.block_slot_time;
        let baker_id = if let Some(index) = data.block_info.block_baker {
            Some(i64::try_from(index.id.index)?)
        } else {
            None
        };
        let total_amount =
            i64::try_from(data.tokenomics_info.common_reward_data().total_amount.micro_ccd())?;
        let total_staked = i64::try_from(data.total_staked_capital.micro_ccd())?;
        let mut prepared_block_items = Vec::new();
        for (item_summary, item) in data.events.iter().zip(data.items.iter()) {
            prepared_block_items
                .push(PreparedBlockItem::prepare(node_client, data, item_summary, item).await?)
        }

        let special_items: Vec<_> = data
            .special_events
            .iter()
            .flat_map(|event| {
                PreparedAccountStatement::prepare_from_special_transaction(
                    event,
                    data.block_info.block_height,
                )
            })
            .flatten()
            .filter(|event| event.amount > 0)
            .collect();

        Ok(Self {
            hash,
            height,
            slot_time,
            baker_id,
            total_amount,
            total_staked,
            block_last_finalized,
            prepared_block_items,
            special_items,
        })
    }

    async fn batch_save(
        batch: &[Self],
        context: &mut BlockProcessingContext,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        let mut heights = Vec::with_capacity(batch.len());
        let mut hashes = Vec::with_capacity(batch.len());
        let mut slot_times = Vec::with_capacity(batch.len());
        let mut baker_ids = Vec::with_capacity(batch.len());
        let mut total_amounts = Vec::with_capacity(batch.len());
        let mut total_staked = Vec::with_capacity(batch.len());
        let mut block_times = Vec::with_capacity(batch.len());
        let mut cumulative_num_txss = Vec::with_capacity(batch.len());

        let mut finalizers = Vec::with_capacity(batch.len());
        let mut last_finalizeds = Vec::with_capacity(batch.len());
        let mut finalizers_slot_time = Vec::with_capacity(batch.len());

        for block in batch {
            heights.push(block.height);
            hashes.push(block.hash.clone());
            slot_times.push(block.slot_time);
            baker_ids.push(block.baker_id);
            total_amounts.push(block.total_amount);
            total_staked.push(block.total_staked);
            block_times.push(
                block
                    .slot_time
                    .signed_duration_since(context.last_block_slot_time)
                    .num_milliseconds(),
            );
            context.last_cumulative_num_txs += block.prepared_block_items.len() as i64;
            cumulative_num_txss.push(context.last_cumulative_num_txs);
            context.last_block_slot_time = block.slot_time;

            // Check if this block knows of a new finalized block.
            // If so, note it down so we can mark the blocks since last time as finalized by
            // this block.
            if block.block_last_finalized != context.last_finalized_hash {
                finalizers.push(block.height);
                finalizers_slot_time.push(block.slot_time);
                last_finalizeds.push(block.block_last_finalized.clone());

                context.last_finalized_hash = block.block_last_finalized.clone();
            }
        }

        sqlx::query!(
            "INSERT INTO blocks
  (height, hash, slot_time, block_time, baker_id, total_amount, total_staked, cumulative_num_txs)
SELECT * FROM UNNEST(
  $1::BIGINT[],
  $2::TEXT[],
  $3::TIMESTAMPTZ[],
  $4::BIGINT[],
  $5::BIGINT[],
  $6::BIGINT[],
  $7::BIGINT[],
  $8::BIGINT[]
);",
            &heights,
            &hashes,
            &slot_times,
            &block_times,
            &baker_ids as &[Option<i64>],
            &total_amounts,
            &total_staked,
            &cumulative_num_txss
        )
        .execute(tx.as_mut())
        .await?;

        // With all blocks in the batch inserted we update blocks which we now can
        // compute the finalization time for. Using the list of finalizer blocks
        // (those containing a last finalized block different from its predecessor)
        // we update the blocks below which does not contain finalization time and
        // compute it to be the difference between the slot_time of the block and the
        // finalizer block.
        sqlx::query!(
            r#"
UPDATE blocks
   SET finalization_time = EXTRACT("MILLISECONDS" FROM finalizer.slot_time - blocks.slot_time),
       finalized_by = finalizer.height
FROM UNNEST($1::BIGINT[], $2::TEXT[], $3::TIMESTAMPTZ[]) AS finalizer(height, finalized, slot_time)
JOIN blocks last ON finalizer.finalized = last.hash
WHERE blocks.finalization_time IS NULL AND blocks.height <= last.height
"#,
            &finalizers,
            &last_finalizeds,
            &finalizers_slot_time
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedAccountStatement {
    account_address:  String,
    amount:           i64,
    block_height:     i64,
    transaction_type: AccountStatementEntryType,
}

impl PreparedAccountStatement {
    fn prepare_from_special_transaction(
        event: &SpecialTransactionOutcome,
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Vec<Self>> {
        let results = match &event {
            SpecialTransactionOutcome::BakingRewards {
                baker_rewards,
                ..
            } => baker_rewards
                .iter()
                .map(|(account_address, amount)| {
                    Ok::<PreparedAccountStatement, anyhow::Error>(PreparedAccountStatement {
                        account_address:  account_address.to_string(),
                        amount:           amount.micro_ccd.try_into()?,
                        block_height:     block_height.height.try_into()?,
                        transaction_type: AccountStatementEntryType::BakerReward,
                    })
                })
                .collect::<Result<Vec<_>, _>>()?,
            SpecialTransactionOutcome::Mint {
                foundation_account,
                mint_platform_development_charge,
                ..
            } => vec![PreparedAccountStatement {
                account_address:  foundation_account.to_string(),
                amount:           mint_platform_development_charge.micro_ccd.try_into()?,
                block_height:     block_height.height.try_into()?,
                transaction_type: AccountStatementEntryType::FoundationReward,
            }],
            SpecialTransactionOutcome::FinalizationRewards {
                finalization_rewards,
                ..
            } => finalization_rewards
                .iter()
                .map(|(account_address, amount)| {
                    Ok::<PreparedAccountStatement, anyhow::Error>(PreparedAccountStatement {
                        account_address:  account_address.to_string(),
                        amount:           amount.micro_ccd.try_into()?,
                        block_height:     block_height.height.try_into()?,
                        transaction_type: AccountStatementEntryType::FinalizationReward,
                    })
                })
                .collect::<Result<Vec<_>, _>>()?,
            SpecialTransactionOutcome::BlockReward {
                baker,
                foundation_account,
                baker_reward,
                foundation_charge,
                ..
            } => vec![
                PreparedAccountStatement {
                    account_address:  foundation_account.to_string(),
                    amount:           foundation_charge.micro_ccd.try_into()?,
                    block_height:     block_height.height.try_into()?,
                    transaction_type: AccountStatementEntryType::FoundationReward,
                },
                PreparedAccountStatement {
                    account_address:  baker.to_string(),
                    amount:           baker_reward.micro_ccd.try_into()?,
                    block_height:     block_height.height.try_into()?,
                    transaction_type: AccountStatementEntryType::BakerReward,
                },
            ],
            SpecialTransactionOutcome::PaydayFoundationReward {
                foundation_account,
                development_charge,
            } => vec![PreparedAccountStatement {
                account_address:  foundation_account.to_string(),
                amount:           development_charge.micro_ccd.try_into()?,
                block_height:     block_height.height.try_into()?,
                transaction_type: AccountStatementEntryType::FoundationReward,
            }],
            SpecialTransactionOutcome::PaydayAccountReward {
                account,
                transaction_fees,
                baker_reward,
                finalization_reward,
            } => vec![
                PreparedAccountStatement {
                    account_address:  account.to_string(),
                    amount:           transaction_fees.micro_ccd.try_into()?,
                    block_height:     block_height.height.try_into()?,
                    transaction_type: AccountStatementEntryType::TransactionFeeReward,
                },
                PreparedAccountStatement {
                    account_address:  account.to_string(),
                    amount:           baker_reward.micro_ccd.try_into()?,
                    block_height:     block_height.height.try_into()?,
                    transaction_type: AccountStatementEntryType::BakerReward,
                },
                PreparedAccountStatement {
                    account_address:  account.to_string(),
                    amount:           finalization_reward.micro_ccd.try_into()?,
                    block_height:     block_height.height.try_into()?,
                    transaction_type: AccountStatementEntryType::FinalizationReward,
                },
            ],
            // TODO: Support these two types. (Deviates from Old CCDScan)
            SpecialTransactionOutcome::BlockAccrueReward {
                ..
            }
            | SpecialTransactionOutcome::PaydayPoolReward {
                ..
            } => Vec::new(),
        };
        Ok(results)
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: Option<i64>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "WITH account_info AS (
            SELECT index AS account_index, amount + $3 AS current_balance
            FROM accounts
            WHERE address = $1
        )
        INSERT INTO account_statements (
            account_index,
            entry_type,
            amount,
            block_height,
            transaction_id,
            account_balance
        )
        SELECT
            account_index,
            $2,
            $3,
            $4,
            $5,
            current_balance
        FROM account_info",
            self.account_address,
            self.transaction_type as AccountStatementEntryType,
            self.amount,
            self.block_height,
            transaction_index
        )
        .execute(tx.as_mut())
        .await?;

        sqlx::query!(
            "UPDATE accounts
            SET amount = amount + $1
            WHERE address = $2",
            self.amount,
            self.account_address
        )
        .execute(tx.as_mut())
        .await?;

        Ok(())
    }
}

/// Prepared block item (transaction), ready to be inserted in the database
struct PreparedBlockItem {
    /// Hash of the transaction
    block_item_hash:   String,
    /// Cost for the account signing the block item (in microCCD), always 0 for
    /// update and credential deployments.
    ccd_cost:          i64,
    /// Energy cost of the execution of the block item.
    energy_cost:       i64,
    /// Absolute height of the block.
    block_height:      i64,
    /// Base58check representation of the account address which signed the
    /// block, none for update and credential deployments.
    sender:            Option<String>,
    /// Whether the block item is an account transaction, update or credential
    /// deployment.
    transaction_type:  DbTransactionType,
    /// The type of account transaction, is none if not an account transaction
    /// or if the account transaction got rejected due to deserialization
    /// failing.
    account_type:      Option<AccountTransactionType>,
    /// The type of credential deployment transaction, is none if not a
    /// credential deployment transaction.
    credential_type:   Option<CredentialDeploymentTransactionType>,
    /// The type of update transaction, is none if not an update transaction.
    update_type:       Option<UpdateTransactionType>,
    /// Whether the block item was successful i.e. not rejected.
    success:           bool,
    /// Events of the block item. Is none for rejected block items.
    events:            Option<serde_json::Value>,
    /// Reject reason the block item. Is none for successful block items.
    reject:            Option<serde_json::Value>,
    /// All affected accounts for this transaction. Each entry is the `String`
    /// representation of an account address.
    affected_accounts: Vec<String>,
    /// Block item events prepared for inserting into the database.
    prepared_event:    PreparedEvent,
}

impl PreparedBlockItem {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        item_summary: &BlockItemSummary,
        item: &BlockItem<EncodedPayload>,
    ) -> anyhow::Result<Self> {
        let block_height = i64::try_from(data.finalized_block_info.height.height)?;
        let block_item_hash = item_summary.hash.to_string();
        let ccd_cost =
            i64::try_from(data.chain_parameters.ccd_cost(item_summary.energy_cost).micro_ccd)?;
        let energy_cost = i64::try_from(item_summary.energy_cost.energy)?;
        let sender = item_summary.sender_account().map(|a| a.to_string());
        let (transaction_type, account_type, credential_type, update_type) =
            match &item_summary.details {
                BlockItemSummaryDetails::AccountTransaction(details) => {
                    let account_transaction_type =
                        details.transaction_type().map(AccountTransactionType::from);
                    (DbTransactionType::Account, account_transaction_type, None, None)
                }
                BlockItemSummaryDetails::AccountCreation(details) => {
                    let credential_type =
                        CredentialDeploymentTransactionType::from(details.credential_type);
                    (DbTransactionType::CredentialDeployment, None, Some(credential_type), None)
                }
                BlockItemSummaryDetails::Update(details) => {
                    let update_type = UpdateTransactionType::from(details.update_type());
                    (DbTransactionType::Update, None, None, Some(update_type))
                }
            };
        let success = item_summary.is_success();
        let (events, reject) = if success {
            let events = serde_json::to_value(events_from_summary(item_summary.details.clone())?)?;
            (Some(events), None)
        } else {
            let reject =
                if let BlockItemSummaryDetails::AccountTransaction(AccountTransactionDetails {
                    effects:
                        AccountTransactionEffects::None {
                            reject_reason,
                            ..
                        },
                    ..
                }) = &item_summary.details
                {
                    serde_json::to_value(
                        crate::transaction_reject::TransactionRejectReason::try_from(
                            reject_reason.clone(),
                        )?,
                    )?
                } else {
                    anyhow::bail!("Invariant violation: Failed transaction without a reject reason")
                };
            (None, Some(reject))
        };
        let affected_accounts =
            item_summary.affected_addresses().into_iter().map(|a| a.to_string()).collect();
        let prepared_event = PreparedEvent::prepare(node_client, data, item_summary, item).await?;

        Ok(Self {
            block_item_hash,
            ccd_cost,
            energy_cost,
            block_height,
            sender,
            transaction_type,
            account_type,
            credential_type,
            update_type,
            success,
            events,
            reject,
            affected_accounts,
            prepared_event,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        let tx_idx = sqlx::query_scalar!(
            "INSERT INTO transactions (
                index,
                hash,
                ccd_cost,
                energy_cost,
                block_height,
                sender,
                type,
                type_account,
                type_credential_deployment,
                type_update,
                success,
                events,
                reject
            ) VALUES (
                (SELECT COALESCE(MAX(index) + 1, 0) FROM transactions),
                $1,
                $2,
                $3,
                $4,
                (SELECT index FROM accounts WHERE address = $5),
                $6,
                $7,
                $8,
                $9,
                $10,
                $11,
                $12
            ) RETURNING index",
            self.block_item_hash,
            self.ccd_cost,
            self.energy_cost,
            self.block_height,
            self.sender,
            self.transaction_type as DbTransactionType,
            self.account_type as Option<AccountTransactionType>,
            self.credential_type as Option<CredentialDeploymentTransactionType>,
            self.update_type as Option<UpdateTransactionType>,
            self.success,
            self.events,
            self.reject
        )
        .fetch_one(tx.as_mut())
        .await?;

        // Note that this does not include account creation. We handle that when saving
        // the account creation event.
        sqlx::query!(
            "INSERT INTO affected_accounts (transaction_index, account_index)
            SELECT $1, index FROM accounts WHERE address = ANY($2)",
            tx_idx,
            &self.affected_accounts,
        )
        .execute(tx.as_mut())
        .await?;

        // We also need to keep track of the number of transactions on the accounts
        // table.
        sqlx::query!(
            "UPDATE accounts
            SET num_txs = num_txs + 1
            WHERE address = ANY($1)",
            &self.affected_accounts,
        )
        .execute(tx.as_mut())
        .await?;

        self.prepared_event.save(tx, tx_idx).await?;
        Ok(())
    }
}

/// Different types of block item events that can be prepared.
enum PreparedEvent {
    /// A new account got created.
    AccountCreation(PreparedAccountCreation),
    /// A transfer of CCD from one account to another account.
    CcdTransfer(PreparedCcdTransferEvent),
    /// Event of moving funds either from or to the encrypted balance.
    EncryptedBalance(PreparedUpdateAccountBalance),
    /// Changes related to validators (previously referred to as bakers).
    BakerEvents(Vec<PreparedBakerEvent>),
    /// Account delegation events
    AccountDelegationEvents(Vec<PreparedAccountDelegationEvent>),
    /// Smart contract module got deployed.
    ModuleDeployed(PreparedModuleDeployed),
    /// Contract got initialized.
    ContractInitialized(PreparedContractInitialized),
    /// Rejected transaction attempting to initialize a smart contract
    /// instance or redeploying a module reference.
    RejectModuleTransaction(PreparedRejectModuleTransaction),
    /// Rejected transaction attempting to update a smart contract instance.
    RejectContractUpdateTransaction(PreparedRejectContractUpdateTransaction),
    /// Contract got updated.
    ContractUpdate(Vec<PreparedContractUpdate>),
    /// A scheduled transfer got executed.
    ScheduledTransfer(PreparedScheduledReleases),
    /// No changes in the database was caused by this event.
    NoOperation,
}
impl PreparedEvent {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        item_summary: &BlockItemSummary,
        item: &BlockItem<EncodedPayload>,
    ) -> anyhow::Result<Self> {
        let height = data.block_info.block_height;
        let prepared_event = match &item_summary.details {
            BlockItemSummaryDetails::AccountCreation(details) => {
                PreparedEvent::AccountCreation(PreparedAccountCreation::prepare(details)?)
            }
            BlockItemSummaryDetails::AccountTransaction(details) => match &details.effects {
                AccountTransactionEffects::None {
                    transaction_type,
                    reject_reason,
                } => match transaction_type.as_ref() {
                    Some(&TransactionType::InitContract) | Some(&TransactionType::DeployModule) => {
                        if let RejectReason::ModuleNotWF
                        | RejectReason::InvalidModuleReference {
                            ..
                        } = reject_reason
                        {
                            // Trying to initialize a smart contract from invalid module
                            // reference or deploying invalid smart contract modules are not indexed
                            // further.
                            return Ok(PreparedEvent::NoOperation);
                        }
                        let BlockItem::AccountTransaction(account_transaction) = item else {
                            anyhow::bail!("Block item was expected to be an account transaction")
                        };
                        let decoded = account_transaction
                            .payload
                            .decode()
                            .context("Failed decoding account transaction payload")?;
                        let module_reference = match decoded {
                            Payload::InitContract {
                                payload,
                            } => payload.mod_ref,
                            Payload::DeployModule {
                                module,
                            } => module.get_module_ref(),
                            _ => anyhow::bail!(
                                "Payload did not match InitContract or DeployModule as expected"
                            ),
                        };
                        PreparedEvent::RejectModuleTransaction(
                            PreparedRejectModuleTransaction::prepare(module_reference)?,
                        )
                    }
                    Some(&TransactionType::Update) => {
                        if let RejectReason::InvalidContractAddress {
                            ..
                        } = reject_reason
                        {
                            // Updating a smart contract instances using invalid contract addresses,
                            // i.e. non existing instance, are not indexed further.
                            return Ok(PreparedEvent::NoOperation);
                        }
                        anyhow::ensure!(
                            matches!(
                                reject_reason,
                                RejectReason::InvalidReceiveMethod { .. }
                                    | RejectReason::RuntimeFailure
                                    | RejectReason::AmountTooLarge { .. }
                                    | RejectReason::OutOfEnergy
                                    | RejectReason::RejectedReceive { .. }
                                    | RejectReason::InvalidAccountReference { .. }
                            ),
                            "Unexpected reject reason for Contract Update transaction: {:?}",
                            reject_reason
                        );

                        let BlockItem::AccountTransaction(account_transaction) = item else {
                            anyhow::bail!("Block item was expected to be an account transaction")
                        };
                        let payload = account_transaction
                            .payload
                            .decode()
                            .context("Failed decoding account transaction payload")?;
                        let Payload::Update {
                            payload,
                        } = payload
                        else {
                            anyhow::bail!(
                                "Unexpected payload for transaction of type Update: {:?}",
                                payload
                            )
                        };
                        PreparedEvent::RejectContractUpdateTransaction(
                            PreparedRejectContractUpdateTransaction::prepare(payload.address)?,
                        )
                    }
                    _ => PreparedEvent::NoOperation,
                },
                AccountTransactionEffects::ModuleDeployed {
                    module_ref,
                } => PreparedEvent::ModuleDeployed(
                    PreparedModuleDeployed::prepare(node_client, data, *module_ref).await?,
                ),
                AccountTransactionEffects::ContractInitialized {
                    data: event_data,
                } => PreparedEvent::ContractInitialized(
                    PreparedContractInitialized::prepare(
                        node_client,
                        data,
                        event_data,
                        details.sender.to_string(),
                    )
                    .await?,
                ),
                AccountTransactionEffects::ContractUpdateIssued {
                    effects,
                } => PreparedEvent::ContractUpdate(
                    join_all(effects.iter().enumerate().map(|(trace_element_index, effect)| {
                        let mut node_client = node_client.clone();

                        async move {
                            PreparedContractUpdate::prepare(
                                &mut node_client,
                                data,
                                effect,
                                trace_element_index,
                            )
                            .await
                        }
                    }))
                    .await
                    .into_iter()
                    .collect::<Result<Vec<_>, anyhow::Error>>()?,
                ),
                AccountTransactionEffects::AccountTransfer {
                    amount,
                    to,
                }
                | AccountTransactionEffects::AccountTransferWithMemo {
                    amount,
                    to,
                    ..
                } => PreparedEvent::CcdTransfer(PreparedCcdTransferEvent::prepare(
                    Arc::new(details.sender.to_string()),
                    Arc::new(to.to_string()),
                    amount.micro_ccd().try_into()?,
                    height,
                )?),

                AccountTransactionEffects::BakerAdded {
                    data: event_data,
                } => {
                    let event = concordium_rust_sdk::types::BakerEvent::BakerAdded {
                        data: event_data.clone(),
                    };
                    let prepared = PreparedBakerEvent::prepare(&event)?;
                    PreparedEvent::BakerEvents(vec![prepared])
                }
                AccountTransactionEffects::BakerRemoved {
                    baker_id,
                } => {
                    let event = concordium_rust_sdk::types::BakerEvent::BakerRemoved {
                        baker_id: *baker_id,
                    };
                    let prepared = PreparedBakerEvent::prepare(&event)?;
                    PreparedEvent::BakerEvents(vec![prepared])
                }
                AccountTransactionEffects::BakerStakeUpdated {
                    data: update,
                } => {
                    let Some(update) = update else {
                        return Ok(PreparedEvent::NoOperation);
                    };
                    let event = if update.increased {
                        concordium_rust_sdk::types::BakerEvent::BakerStakeIncreased {
                            baker_id:  update.baker_id,
                            new_stake: update.new_stake,
                        }
                    } else {
                        concordium_rust_sdk::types::BakerEvent::BakerStakeDecreased {
                            baker_id:  update.baker_id,
                            new_stake: update.new_stake,
                        }
                    };
                    let prepared = PreparedBakerEvent::prepare(&event)?;
                    PreparedEvent::BakerEvents(vec![prepared])
                }
                AccountTransactionEffects::BakerRestakeEarningsUpdated {
                    baker_id,
                    restake_earnings,
                } => PreparedEvent::BakerEvents(vec![PreparedBakerEvent::prepare(
                    &concordium_rust_sdk::types::BakerEvent::BakerRestakeEarningsUpdated {
                        baker_id:         *baker_id,
                        restake_earnings: *restake_earnings,
                    },
                )?]),
                AccountTransactionEffects::BakerKeysUpdated {
                    ..
                } => PreparedEvent::NoOperation,
                AccountTransactionEffects::BakerConfigured {
                    data: events,
                } => PreparedEvent::BakerEvents(
                    events
                        .iter()
                        .map(PreparedBakerEvent::prepare)
                        .collect::<anyhow::Result<Vec<_>>>()?,
                ),

                AccountTransactionEffects::EncryptedAmountTransferred {
                    ..
                }
                | AccountTransactionEffects::EncryptedAmountTransferredWithMemo {
                    ..
                } => PreparedEvent::NoOperation,
                AccountTransactionEffects::TransferredToEncrypted {
                    data,
                } => PreparedEvent::EncryptedBalance(PreparedUpdateAccountBalance::prepare(
                    details.sender.to_string(),
                    -i64::try_from(data.amount.micro_ccd)?,
                    height,
                    AccountStatementEntryType::AmountEncrypted,
                )?),
                AccountTransactionEffects::TransferredToPublic {
                    amount,
                    ..
                } => PreparedEvent::EncryptedBalance(PreparedUpdateAccountBalance::prepare(
                    details.sender.to_string(),
                    i64::try_from(amount.micro_ccd)?,
                    height,
                    AccountStatementEntryType::AmountDecrypted,
                )?),
                AccountTransactionEffects::TransferredWithSchedule {
                    to,
                    amount: scheduled_releases,
                }
                | AccountTransactionEffects::TransferredWithScheduleAndMemo {
                    to,
                    amount: scheduled_releases,
                    ..
                } => PreparedEvent::ScheduledTransfer(PreparedScheduledReleases::prepare(
                    Arc::new(to.to_string()),
                    details.sender.to_string(),
                    scheduled_releases,
                    height,
                )?),
                AccountTransactionEffects::CredentialKeysUpdated {
                    ..
                }
                | AccountTransactionEffects::CredentialsUpdated {
                    ..
                }
                | AccountTransactionEffects::DataRegistered {
                    ..
                } => PreparedEvent::NoOperation,

                AccountTransactionEffects::DelegationConfigured {
                    data: events,
                } => PreparedEvent::AccountDelegationEvents(
                    events
                        .iter()
                        .map(PreparedAccountDelegationEvent::prepare)
                        .collect::<anyhow::Result<Vec<_>>>()?,
                ),
            },
            details => {
                warn!("details = \n {:#?}", details);
                PreparedEvent::NoOperation
            }
        };
        Ok(prepared_event)
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        tx_idx: i64,
    ) -> anyhow::Result<()> {
        match self {
            PreparedEvent::AccountCreation(event) => event.save(tx, tx_idx).await,
            PreparedEvent::CcdTransfer(event) => event.save(tx, tx_idx).await,
            PreparedEvent::EncryptedBalance(event) => event.save(tx, tx_idx).await,
            PreparedEvent::BakerEvents(events) => {
                for event in events {
                    event.save(tx).await?;
                }
                Ok(())
            }
            PreparedEvent::ModuleDeployed(event) => event.save(tx, tx_idx).await,
            PreparedEvent::ContractInitialized(event) => event.save(tx, tx_idx).await,
            PreparedEvent::RejectModuleTransaction(event) => event.save(tx, tx_idx).await,
            PreparedEvent::ContractUpdate(events) => {
                for event in events {
                    event.save(tx, tx_idx).await?;
                }
                Ok(())
            }
            PreparedEvent::RejectContractUpdateTransaction(event) => event.save(tx, tx_idx).await,
            PreparedEvent::AccountDelegationEvents(events) => {
                for event in events {
                    event.save(tx).await?;
                }
                Ok(())
            }
            PreparedEvent::ScheduledTransfer(event) => event.save(tx, tx_idx).await,
            PreparedEvent::NoOperation => Ok(()),
        }
    }
}

/// Prepared database insertion of a new account.
struct PreparedAccountCreation {
    /// The base58check representation of the canonical account address.
    account_address: String,
}

impl PreparedAccountCreation {
    fn prepare(
        details: &concordium_rust_sdk::types::AccountCreationDetails,
    ) -> anyhow::Result<Self> {
        Ok(Self {
            account_address: details.address.to_string(),
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        let account_index = sqlx::query_scalar!(
            "INSERT INTO
                accounts (index, address, transaction_index)
            VALUES
                ((SELECT COALESCE(MAX(index) + 1, 0) FROM accounts), $1, $2)
            RETURNING index",
            self.account_address,
            transaction_index,
        )
        .fetch_one(tx.as_mut())
        .await?;

        sqlx::query!(
            "INSERT INTO affected_accounts (transaction_index, account_index)
            VALUES ($1, $2)",
            transaction_index,
            account_index
        )
        .execute(tx.as_mut())
        .await?;

        Ok(())
    }
}

enum PreparedAccountDelegationEvent {
    StakeIncrease {
        account_id: i64,
        staked:     i64,
    },
    StakeDecrease {
        account_id: i64,
        staked:     i64,
    },
    SetRestakeEarnings {
        account_id:       i64,
        restake_earnings: bool,
    },
    Added {
        account_id: i64,
    },
    Removed {
        account_id: i64,
    },
    SetDelegationTarget {
        account_id: i64,
        target_id:  Option<i64>,
    },
    NoOperation,
}

impl PreparedAccountDelegationEvent {
    fn prepare(event: &concordium_rust_sdk::types::DelegationEvent) -> anyhow::Result<Self> {
        use concordium_rust_sdk::types::DelegationEvent;
        let prepared = match event {
            DelegationEvent::DelegationStakeIncreased {
                delegator_id,
                new_stake,
            } => PreparedAccountDelegationEvent::StakeIncrease {
                account_id: delegator_id.id.index.try_into()?,
                staked:     new_stake.micro_ccd.try_into()?,
            },
            DelegationEvent::DelegationStakeDecreased {
                delegator_id,
                new_stake,
            } => PreparedAccountDelegationEvent::StakeDecrease {
                account_id: delegator_id.id.index.try_into()?,
                staked:     new_stake.micro_ccd.try_into()?,
            },
            DelegationEvent::DelegationSetRestakeEarnings {
                delegator_id,
                restake_earnings,
            } => PreparedAccountDelegationEvent::SetRestakeEarnings {
                account_id:       delegator_id.id.index.try_into()?,
                restake_earnings: *restake_earnings,
            },
            DelegationEvent::DelegationSetDelegationTarget {
                delegator_id,
                delegation_target,
            } => PreparedAccountDelegationEvent::SetDelegationTarget {
                account_id: delegator_id.id.index.try_into()?,
                target_id:  if let DelegationTarget::Baker {
                    baker_id,
                } = delegation_target
                {
                    Some(baker_id.id.index.try_into()?)
                } else {
                    None
                },
            },
            DelegationEvent::DelegationAdded {
                delegator_id,
            } => PreparedAccountDelegationEvent::Added {
                account_id: delegator_id.id.index.try_into()?,
            },
            DelegationEvent::DelegationRemoved {
                delegator_id,
            } => PreparedAccountDelegationEvent::Removed {
                account_id: delegator_id.id.index.try_into()?,
            },
            DelegationEvent::BakerRemoved {
                ..
            } => PreparedAccountDelegationEvent::NoOperation,
        };
        Ok(prepared)
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        match self {
            PreparedAccountDelegationEvent::StakeIncrease {
                account_id,
                staked,
            }
            | PreparedAccountDelegationEvent::StakeDecrease {
                account_id,
                staked,
            } => {
                sqlx::query!(
                    r#"UPDATE accounts SET delegated_stake = $1 WHERE index = $2"#,
                    staked,
                    account_id
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedAccountDelegationEvent::Added {
                account_id,
            }
            | PreparedAccountDelegationEvent::Removed {
                account_id,
            } => {
                sqlx::query!(
                    r#"UPDATE accounts SET delegated_stake = 0, delegated_restake_earnings = false, delegated_target_baker_id = NULL WHERE index = $1"#,
                    account_id
                )
                .execute(tx.as_mut())
                .await?;
            }

            PreparedAccountDelegationEvent::SetRestakeEarnings {
                account_id,
                restake_earnings,
            } => {
                sqlx::query!(
                    r#"UPDATE accounts SET delegated_restake_earnings = $1 WHERE index = $2"#,
                    *restake_earnings,
                    account_id
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedAccountDelegationEvent::SetDelegationTarget {
                account_id,
                target_id,
            } => {
                sqlx::query!(
                    r#"UPDATE accounts SET delegated_target_baker_id = $1 WHERE index = $2"#,
                    *target_id,
                    account_id
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedAccountDelegationEvent::NoOperation {} => (),
        }
        Ok(())
    }
}

/// Event changing state related to validators (bakers).
enum PreparedBakerEvent {
    Add {
        baker_id:         i64,
        staked:           i64,
        restake_earnings: bool,
    },
    Remove {
        baker_id: i64,
    },
    StakeIncrease {
        baker_id: i64,
        staked:   i64,
    },
    StakeDecrease {
        baker_id: i64,
        staked:   i64,
    },
    SetRestakeEarnings {
        baker_id:         i64,
        restake_earnings: bool,
    },
    SetOpenStatus {
        baker_id:    i64,
        open_status: BakerPoolOpenStatus,
    },
    SetMetadataUrl {
        baker_id:     i64,
        metadata_url: String,
    },
    SetTransactionFeeCommission {
        baker_id:   i64,
        commission: i64,
    },
    SetBakingRewardCommission {
        baker_id:   i64,
        commission: i64,
    },
    SetFinalizationRewardCommission {
        baker_id:   i64,
        commission: i64,
    },
    RemoveDelegation {
        delegator_id: i64,
    },
    NoOperation,
}
impl PreparedBakerEvent {
    fn prepare(event: &concordium_rust_sdk::types::BakerEvent) -> anyhow::Result<Self> {
        use concordium_rust_sdk::types::BakerEvent;
        let prepared = match event {
            BakerEvent::BakerAdded {
                data: details,
            } => PreparedBakerEvent::Add {
                baker_id:         details.keys_event.baker_id.id.index.try_into()?,
                staked:           details.stake.micro_ccd().try_into()?,
                restake_earnings: details.restake_earnings,
            },
            BakerEvent::BakerRemoved {
                baker_id,
            } => PreparedBakerEvent::Remove {
                baker_id: baker_id.id.index.try_into()?,
            },
            BakerEvent::BakerStakeIncreased {
                baker_id,
                new_stake,
            } => PreparedBakerEvent::StakeIncrease {
                baker_id: baker_id.id.index.try_into()?,
                staked:   new_stake.micro_ccd().try_into()?,
            },
            BakerEvent::BakerStakeDecreased {
                baker_id,
                new_stake,
            } => PreparedBakerEvent::StakeDecrease {
                baker_id: baker_id.id.index.try_into()?,
                staked:   new_stake.micro_ccd().try_into()?,
            },
            BakerEvent::BakerRestakeEarningsUpdated {
                baker_id,
                restake_earnings,
            } => PreparedBakerEvent::SetRestakeEarnings {
                baker_id:         baker_id.id.index.try_into()?,
                restake_earnings: *restake_earnings,
            },
            BakerEvent::BakerKeysUpdated {
                ..
            } => PreparedBakerEvent::NoOperation,
            BakerEvent::BakerSetOpenStatus {
                baker_id,
                open_status,
            } => PreparedBakerEvent::SetOpenStatus {
                baker_id:    baker_id.id.index.try_into()?,
                open_status: open_status.to_owned().into(),
            },
            BakerEvent::BakerSetMetadataURL {
                baker_id,
                metadata_url,
            } => PreparedBakerEvent::SetMetadataUrl {
                baker_id:     baker_id.id.index.try_into()?,
                metadata_url: metadata_url.to_string(),
            },
            BakerEvent::BakerSetTransactionFeeCommission {
                baker_id,
                transaction_fee_commission,
            } => PreparedBakerEvent::SetTransactionFeeCommission {
                baker_id:   baker_id.id.index.try_into()?,
                commission: u32::from(PartsPerHundredThousands::from(*transaction_fee_commission))
                    .into(),
            },
            BakerEvent::BakerSetBakingRewardCommission {
                baker_id,
                baking_reward_commission,
            } => PreparedBakerEvent::SetBakingRewardCommission {
                baker_id:   baker_id.id.index.try_into()?,
                commission: u32::from(PartsPerHundredThousands::from(*baking_reward_commission))
                    .into(),
            },
            BakerEvent::BakerSetFinalizationRewardCommission {
                baker_id,
                finalization_reward_commission,
            } => PreparedBakerEvent::SetFinalizationRewardCommission {
                baker_id:   baker_id.id.index.try_into()?,
                commission: u32::from(PartsPerHundredThousands::from(
                    *finalization_reward_commission,
                ))
                .into(),
            },
            BakerEvent::DelegationRemoved {
                delegator_id,
            } => PreparedBakerEvent::RemoveDelegation {
                delegator_id: delegator_id.id.index.try_into()?,
            },
        };
        Ok(prepared)
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        match self {
            PreparedBakerEvent::Add {
                baker_id,
                staked,
                restake_earnings,
            } => {
                sqlx::query!(
                    "INSERT INTO bakers (id, staked, restake_earnings) VALUES ($1, $2, $3)",
                    baker_id,
                    staked,
                    restake_earnings,
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::Remove {
                baker_id,
            } => {
                sqlx::query!("DELETE FROM bakers WHERE id=$1", baker_id,)
                    .execute(tx.as_mut())
                    .await?;
            }
            PreparedBakerEvent::StakeIncrease {
                baker_id,
                staked,
            } => {
                sqlx::query!("UPDATE bakers SET staked = $2 WHERE id=$1", baker_id, staked,)
                    .execute(tx.as_mut())
                    .await?;
            }
            PreparedBakerEvent::StakeDecrease {
                baker_id,
                staked,
            } => {
                sqlx::query!("UPDATE bakers SET staked = $2 WHERE id=$1", baker_id, staked,)
                    .execute(tx.as_mut())
                    .await?;
            }
            PreparedBakerEvent::SetRestakeEarnings {
                baker_id,
                restake_earnings,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET restake_earnings = $2 WHERE id=$1",
                    baker_id,
                    restake_earnings,
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::SetOpenStatus {
                baker_id,
                open_status,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET open_status = $2 WHERE id=$1",
                    baker_id,
                    *open_status as BakerPoolOpenStatus,
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::SetMetadataUrl {
                baker_id,
                metadata_url,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET metadata_url = $2 WHERE id=$1",
                    baker_id,
                    metadata_url
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::SetTransactionFeeCommission {
                baker_id,
                commission,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET transaction_commission = $2 WHERE id=$1",
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::SetBakingRewardCommission {
                baker_id,
                commission,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET baking_commission = $2 WHERE id=$1",
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::SetFinalizationRewardCommission {
                baker_id,
                commission,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET finalization_commission = $2 WHERE id=$1",
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::RemoveDelegation {
                delegator_id,
            } => {
                sqlx::query!(
                    r#"UPDATE accounts SET delegated_stake = 0, delegated_restake_earnings = false, delegated_target_baker_id = NULL WHERE index = $1"#,
                    delegator_id
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::NoOperation => (),
        }
        Ok(())
    }
}

struct PreparedModuleDeployed {
    module_reference: String,
    schema:           Option<Vec<u8>>,
}

impl PreparedModuleDeployed {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        module_reference: sdk_types::hashes::ModuleReference,
    ) -> anyhow::Result<Self> {
        let block_height = data.finalized_block_info.height;

        let wasm_module = node_client
            .get_module_source(&module_reference, BlockIdentifier::AbsoluteHeight(block_height))
            .await?
            .response;
        let schema = match wasm_module.version {
            WasmVersion::V0 => get_embedded_schema_v0(wasm_module.source.as_ref()),
            WasmVersion::V1 => get_embedded_schema_v1(wasm_module.source.as_ref()),
        }
        .ok();

        let schema = schema.as_ref().map(to_bytes);

        Ok(Self {
            module_reference: module_reference.into(),
            schema,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO smart_contract_modules (
                module_reference,
                transaction_index,
                schema
            ) VALUES ($1, $2, $3)",
            self.module_reference,
            transaction_index,
            self.schema
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedModuleLinkAction {
    module_reference:   String,
    contract_index:     i64,
    contract_sub_index: i64,
    link_action:        ModuleReferenceContractLinkAction,
}
impl PreparedModuleLinkAction {
    fn prepare(
        module_reference: sdk_types::hashes::ModuleReference,
        contract_address: sdk_types::ContractAddress,
        link_action: ModuleReferenceContractLinkAction,
    ) -> anyhow::Result<Self> {
        Ok(Self {
            contract_index: i64::try_from(contract_address.index)?,
            contract_sub_index: i64::try_from(contract_address.subindex)?,
            module_reference: module_reference.into(),
            link_action,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            r#"INSERT INTO link_smart_contract_module_transactions (
                index,
                module_reference,
                transaction_index,
                contract_index,
                contract_sub_index,
                link_action
            ) VALUES (
                (SELECT COALESCE(MAX(index) + 1, 0)
                 FROM link_smart_contract_module_transactions
                 WHERE module_reference = $1),
                $1, $2, $3, $4, $5)"#,
            self.module_reference,
            transaction_index,
            self.contract_index,
            self.contract_sub_index,
            self.link_action as ModuleReferenceContractLinkAction
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedContractInitialized {
    index:             i64,
    sub_index:         i64,
    module_reference:  String,
    name:              String,
    amount:            i64,
    module_link_event: PreparedModuleLinkAction,
    cis2_token_events: Vec<ScalarCis2TokenEvent>,
    account_statement: PreparedAccountStatement,
}

impl PreparedContractInitialized {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        event: &ContractInitializedEvent,
        sender_account: String,
    ) -> anyhow::Result<Self> {
        let contract_address = event.address;
        let index = i64::try_from(event.address.index)?;
        let sub_index = i64::try_from(event.address.subindex)?;
        let module_reference = event.origin_ref;
        // We remove the `init_` prefix from the name to get the contract name.
        let name = event.init_name.as_contract_name().contract_name().to_string();
        let amount = i64::try_from(event.amount.micro_ccd)?;

        let module_link_event = PreparedModuleLinkAction::prepare(
            module_reference,
            event.address,
            ModuleReferenceContractLinkAction::Added,
        )?;
        let account_statement = PreparedAccountStatement {
            account_address:  sender_account,
            transaction_type: AccountStatementEntryType::TransferOut,
            block_height:     data.block_info.block_height.height.try_into()?,
            amount:           -amount,
        };

        // To track CIS2 tokens (e.g., token balances, total supply, token metadata
        // URLs), we gather the CIS2 events here. We check if logged contract
        // events can be parsed as CIS2 events. In addition, we check if the
        // contract supports the `CIS2` standard by calling the on-chain
        // `supports` endpoint before considering the CIS2 events valid.
        //
        // There are two edge cases that the indexer would not identify a CIS2 event
        // correctly. Nonetheless, to avoid complexity it was deemed acceptable
        // behavior.
        // - Edge case 1: A contract code upgrades and no longer
        // supports CIS2 then logging a CIS2-like event within the same block.
        // - Edge case 2: A contract logs a CIS2-like event and then upgrades to add
        // support for CIS2 in the same block.
        //
        // There are three chain events (`ContractInitializedEvent`,
        // `ContractInterruptedEvent` and `ContractUpdatedEvent`) that can generate
        // `contract_logs`. CIS2 events logged by the first chain event are
        // handled here while CIS2 events logged in the `ContractInterruptedEvent` and
        // `ContractUpdatedEvent` are handled at its corresponding
        // transaction type.
        let potential_cis2_events =
            event.events.iter().filter_map(|log| log.try_into().ok()).collect::<Vec<_>>();

        // If the vector `potential_cis2_events` is not empty, we verify that the smart
        // contract supports the CIS2 standard before accepting the events as
        // valid.
        let cis2_token_events = if potential_cis2_events.is_empty() {
            vec![]
        } else {
            let supports_cis2 = cis0::supports(
                node_client,
                &BlockIdentifier::AbsoluteHeight(data.block_info.block_height),
                contract_address,
                event.init_name.as_contract_name(),
                cis0::StandardIdentifier::CIS2,
            )
            .await
            .is_ok_and(|r| r.response.is_support());

            if supports_cis2 {
                potential_cis2_events.into_iter().map(|event: cis2::Event| event.into()).collect()
            } else {
                // If contract does not support `CIS2`, don't consider the events as CIS2
                // events.
                vec![]
            }
        };

        Ok(Self {
            index,
            sub_index,
            module_reference: module_reference.into(),
            name,
            amount,
            module_link_event,
            cis2_token_events,
            account_statement,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO contracts (
                index,
                sub_index,
                module_reference,
                name,
                amount,
                transaction_index
            ) VALUES ($1, $2, $3, $4, $5, $6)",
            self.index,
            self.sub_index,
            self.module_reference,
            self.name,
            self.amount,
            transaction_index
        )
        .execute(tx.as_mut())
        .await
        .context("Failed inserting new to 'contracts' table")?;

        self.module_link_event
            .save(tx, transaction_index)
            .await
            .context("Failed linking new contract to module")?;

        for log in self.cis2_token_events.iter() {
            process_cis2_token_event(log, self.index, self.sub_index, transaction_index, tx)
                .await
                .context("Failed processing a CIS-2 event")?
        }
        self.account_statement.save(tx, Some(transaction_index)).await?;
        Ok(())
    }
}

struct PreparedRejectModuleTransaction {
    module_reference: String,
}

impl PreparedRejectModuleTransaction {
    fn prepare(module_reference: sdk_types::hashes::ModuleReference) -> anyhow::Result<Self> {
        Ok(Self {
            module_reference: module_reference.into(),
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO rejected_smart_contract_module_transactions (
                index,
                module_reference,
                transaction_index
            ) VALUES (
                (SELECT
                    COALESCE(MAX(index) + 1, 0)
                FROM rejected_smart_contract_module_transactions
                WHERE module_reference = $1),
            $1, $2)",
            self.module_reference,
            transaction_index
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedContractUpdate {
    height:                     i64,
    contract_index:             i64,
    contract_sub_index:         i64,
    /// Potential module link events from a smart contract upgrade
    module_link_event:          Option<PreparedContractUpgrade>,
    cis2_token_events:          Vec<ScalarCis2TokenEvent>,
    trace_element_index:        i64,
    prepared_account_statement: Option<PreparedUpdateAccountBalance>,
}

impl PreparedContractUpdate {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        event: &ContractTraceElement,
        trace_element_index: usize,
    ) -> anyhow::Result<Self> {
        let contract_address = event.affected_address();

        let trace_element_index = trace_element_index.try_into()?;
        let height = data.finalized_block_info.height;
        let index = i64::try_from(contract_address.index)?;
        let sub_index = i64::try_from(contract_address.subindex)?;

        let module_link_event = match event {
            &ContractTraceElement::Upgraded {
                address,
                from,
                to,
            } => Some(PreparedContractUpgrade::prepare(address, from, to)?),
            _ => None,
        };

        // To track CIS2 tokens (e.g., token balances, total supply, token metadata
        // URLs), we gather the CIS2 events here. We check if logged contract
        // events can be parsed as CIS2 events. In addition, we check if the
        // contract supports the `CIS2` standard by calling the on-chain
        // `supports` endpoint before considering the CIS2 events valid.
        //
        // There are two edge cases that the indexer would not identify a CIS2 event
        // correctly. Nonetheless, to avoid complexity it was deemed acceptable
        // behavior.
        // - Edge case 1: A contract code upgrades and no longer
        // supports CIS2 then logging a CIS2-like event within the same block.
        // - Edge case 2: A contract logs a CIS2-like event and then upgrades to add
        // support for CIS2 in the same block.
        //
        // There are three chain events (`ContractInitializedEvent`,
        // `ContractInterruptedEvent` and `ContractUpdatedEvent`) that can generate
        // `contract_logs`. CIS2 events logged by the last two chain events are
        // handled here while CIS2 events logged in the
        // `ContractInitializedEvent` are handled at its corresponding
        // transaction type.
        let potential_cis2_events = match event {
            ContractTraceElement::Updated {
                data,
            } => data.events.iter().filter_map(|log| log.try_into().ok()).collect::<Vec<_>>(),
            ContractTraceElement::Transferred {
                ..
            } => vec![],
            ContractTraceElement::Interrupted {
                events,
                ..
            } => events.iter().filter_map(|log| log.try_into().ok()).collect::<Vec<_>>(),
            ContractTraceElement::Resumed {
                ..
            } => vec![],
            ContractTraceElement::Upgraded {
                ..
            } => vec![],
        };

        let prepared_account_statement = match event {
            ContractTraceElement::Updated {
                data,
            } => {
                if let Address::Account(account_address) = data.instigator {
                    Some(PreparedUpdateAccountBalance::prepare(
                        account_address.to_string(),
                        -i64::try_from(data.amount.micro_ccd)?,
                        height,
                        AccountStatementEntryType::TransferOut,
                    )?)
                } else {
                    None
                }
            }
            ContractTraceElement::Transferred {
                amount,
                to,
                ..
            } => Some(PreparedUpdateAccountBalance::prepare(
                to.to_string(),
                amount.micro_ccd.try_into()?,
                height,
                AccountStatementEntryType::TransferIn,
            )?),
            _ => None,
        };

        // If the vector `potential_cis2_events` is not empty, we verify that the smart
        // contract supports the CIS2 standard before accepting the events as
        // valid.
        let cis2_token_events = if potential_cis2_events.is_empty() {
            vec![]
        } else {
            let contract_info = node_client
                .get_instance_info(
                    contract_address,
                    &BlockIdentifier::AbsoluteHeight(data.block_info.block_height),
                )
                .await?;
            let contract_name = contract_info.response.name().as_contract_name();

            let supports_cis2 = cis0::supports(
                node_client,
                &BlockIdentifier::AbsoluteHeight(data.block_info.block_height),
                contract_address,
                contract_name,
                cis0::StandardIdentifier::CIS2,
            )
            .await
            .is_ok_and(|r| r.response.is_support());

            if supports_cis2 {
                potential_cis2_events.into_iter().map(|event: cis2::Event| event.into()).collect()
            } else {
                // If contract does not support `CIS2`, don't consider the events as CIS2
                // events.
                vec![]
            }
        };

        Ok(Self {
            trace_element_index,
            height: height.height.try_into()?,
            contract_index: index,
            contract_sub_index: sub_index,
            module_link_event,
            cis2_token_events,
            prepared_account_statement,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO contract_events (
                transaction_index,
                trace_element_index,
                block_height,
                contract_index,
                contract_sub_index,
                event_index_per_contract
            )
            VALUES (
                $1, $2, $3, $4, $5, (SELECT COALESCE(MAX(event_index_per_contract) + 1, 0) FROM \
             contract_events WHERE contract_index = $4 AND contract_sub_index = $5)
            )",
            transaction_index,
            self.trace_element_index,
            self.height,
            self.contract_index,
            self.contract_sub_index
        )
        .execute(tx.as_mut())
        .await?;

        if let Some(upgrade) = self.module_link_event.as_ref() {
            upgrade.save(tx, transaction_index).await?;
        }

        if let Some(prepared_account_statement) = self.prepared_account_statement.as_ref() {
            prepared_account_statement.save(tx, transaction_index).await?;
        }

        for log in self.cis2_token_events.iter() {
            process_cis2_token_event(
                log,
                self.contract_index,
                self.contract_sub_index,
                transaction_index,
                tx,
            )
            .await?
        }
        Ok(())
    }
}

struct PreparedContractUpgrade {
    module_removed:        PreparedModuleLinkAction,
    module_added:          PreparedModuleLinkAction,
    contract_last_upgrade: PreparedUpdateContractLastUpgrade,
}

impl PreparedContractUpgrade {
    fn prepare(
        address: ContractAddress,
        from: sdk_types::hashes::ModuleReference,
        to: sdk_types::hashes::ModuleReference,
    ) -> anyhow::Result<Self> {
        Ok(Self {
            module_removed:        PreparedModuleLinkAction::prepare(
                from,
                address,
                ModuleReferenceContractLinkAction::Removed,
            )?,
            module_added:          PreparedModuleLinkAction::prepare(
                to,
                address,
                ModuleReferenceContractLinkAction::Added,
            )?,
            contract_last_upgrade: PreparedUpdateContractLastUpgrade::prepare(address)?,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.module_removed.save(tx, transaction_index).await?;
        self.module_added.save(tx, transaction_index).await?;
        self.contract_last_upgrade.save(tx, transaction_index).await
    }
}

struct PreparedUpdateContractLastUpgrade {
    contract_index:     i64,
    contract_sub_index: i64,
}
impl PreparedUpdateContractLastUpgrade {
    fn prepare(address: ContractAddress) -> anyhow::Result<Self> {
        Ok(Self {
            contract_index:     i64::try_from(address.index)?,
            contract_sub_index: i64::try_from(address.subindex)?,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE contracts
             SET last_upgrade_transaction_index = $1
             WHERE index = $2 AND sub_index = $3",
            transaction_index,
            self.contract_index,
            self.contract_sub_index
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedRejectContractUpdateTransaction {
    contract_index:     i64,
    contract_sub_index: i64,
}
impl PreparedRejectContractUpdateTransaction {
    fn prepare(address: ContractAddress) -> anyhow::Result<Self> {
        Ok(Self {
            contract_index:     i64::try_from(address.index)?,
            contract_sub_index: i64::try_from(address.subindex)?,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO contract_reject_transactions (
                 contract_index,
                 contract_sub_index,
                 transaction_index,
                 transaction_index_per_contract
             ) VALUES (
                 $1,
                 $2,
                 $3,
                 (SELECT
                     COALESCE(MAX(transaction_index_per_contract) + 1, 0)
                  FROM contract_reject_transactions
                  WHERE
                      contract_index = $1 AND contract_sub_index = $2
                 )
             )",
            self.contract_index,
            self.contract_sub_index,
            transaction_index,
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

async fn process_cis2_token_event(
    cis2_token_event: &ScalarCis2TokenEvent,
    contract_index: i64,
    contract_sub_index: i64,
    transaction_index: i64,
    tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
) -> anyhow::Result<()> {
    match cis2_token_event {
        // - The `total_supply` value of a token is inserted/updated in the database here.
        // Only `Mint` and `Burn` events affect the `total_supply` of a
        // token.
        // - The `balance` value of the token owner is inserted/updated in the database here.
        // Only `Mint`, `Burn`, and `Transfer` events affect the `balance` of a token owner.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        cis2_mint_event @ ScalarCis2TokenEvent::Mint(Cis2MintEvent {
            raw_token_id,
            amount,
            owner,
        }) => {
            let token_address = TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            // Note: Some `buggy` CIS2 token contracts might mint more tokens than the
            // MAX::TOKEN_AMOUNT specified in the CIS2 standard. The
            // `total_supply/balance` eventually overflows in that case.
            let tokens_minted = BigDecimal::from_biguint(amount.0.clone(), 0);
            // If the `token_address` does not exist, insert the new token with its
            // `total_supply` set to `tokens_minted`. If the `token_address` exists,
            // update the `total_supply` value by adding the `tokens_minted` to the existing
            // value in the database.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, total_supply, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        $4,
                        $5,
                        $6
                    )
                    ON CONFLICT (token_address)
                    DO UPDATE SET total_supply = tokens.total_supply + EXCLUDED.total_supply",
                token_address,
                contract_index,
                contract_sub_index,
                tokens_minted.clone(),
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting or updating token from mint event")?;

            // If the owner doesn't already hold this token, insert a new row with a balance
            // of `tokens_minted`. Otherwise, update the existing row by
            // incrementing the owner's balance by `tokens_minted`.
            // Note: CCDScan currently only tracks token balances of accounts (issue #357).
            if let Address::Account(owner) = owner {
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                    DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    owner.to_string(),
                    token_address,
                    tokens_minted,
                )
                .execute(tx.as_mut())
                .await
                .context("Failed inserting or updating account balance from mint event")?;
            }

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_mint_event)?,
            )
            .execute(tx.as_mut())
            .await?;
        }

        // - The `total_supply` value of a token is inserted/updated in the database here.
        // Only `Mint` and `Burn` events affect the `total_supply` of a
        // token.
        // - The `balance` value of the token owner is inserted/updated in the database here.
        // Only `Mint`, `Burn`, and `Transfer` events affect the `balance` of a token owner.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        // Note: Some `buggy` CIS2 token contracts might burn more tokens than they have
        // initially minted. The `total_supply/balance` can have a negative value in that case
        // and even underflow.
        cis2_burn_event @ ScalarCis2TokenEvent::Burn(Cis2BurnEvent {
            raw_token_id,
            amount,
            owner,
        }) => {
            let token_address = TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            // Note: Some `buggy` CIS2 token contracts might burn more tokens than they have
            // initially minted. The `total_supply/balance` will be set to a negative value
            // and eventually underflow in that case.
            let tokens_burned = BigDecimal::from_biguint(amount.0.clone(), 0);
            // If the `token_address` does not exist (likely a `buggy` CIS2 token contract),
            // insert the new token with its `total_supply` set to `-tokens_burned`. If the
            // `token_address` exists, update the `total_supply` value by
            // subtracting the `tokens_burned` from the existing value in the
            // database.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, total_supply, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        $4,
                        $5,
                        $6
                    )
                    ON CONFLICT (token_address)
                    DO UPDATE SET total_supply = tokens.total_supply + EXCLUDED.total_supply",
                token_address,
                contract_index,
                contract_sub_index,
                -tokens_burned.clone(),
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting or updating token from burn event")?;

            if let Address::Account(owner) = owner {
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                    DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    owner.to_string(),
                    token_address.to_string(),
                    -tokens_burned
                )
                .execute(tx.as_mut())
                .await
                .context("Failed inserting or updating account balance from burn event")?;
            }

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_burn_event)?,
            )
            .execute(tx.as_mut())
            .await?;
        }

        // - The `balance` values of the token are inserted/updated in the database here for the
        //   `from` and `to` addresses.
        // Only `Mint`, `Burn`, and `Transfer` events affect the `balance` of a token owner.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        // Note: Some `buggy` CIS2 token contracts might transfer more tokens than an owner owns.
        // The `balance` can have a negative value in that case.
        cis2_transfer_event @ ScalarCis2TokenEvent::Transfer(Cis2TransferEvent {
            raw_token_id,
            amount,
            from,
            to,
        }) => {
            let token_address = TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            let tokens_transferred = BigDecimal::from_biguint(amount.0.clone(), 0);

            // If the `from` address doesn't already hold this token, insert a new row with
            // a balance of `-tokens_transferred`. Otherwise, update the existing row
            // by decrementing the owner's balance by `tokens_transferred`.
            // Note: CCDScan currently only tracks token balances of accounts (issue #357).
            if let Address::Account(from) = from {
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                    DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    from.to_string(),
                    token_address,
                    -tokens_transferred.clone(),
                )
                .execute(tx.as_mut())
                .await
                .context(
                    "Failed inserting or updating account balance from transfer event (sender)",
                )?;
            }

            // If the `to` address doesn't already hold this token, insert a new row with a
            // balance of `tokens_transferred`. Otherwise, update the existing row by
            // incrementing the owner's balance by `tokens_transferred`.
            // Note: CCDScan currently only tracks token balances of accounts (issue #357).
            if let Address::Account(to) = to {
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                        DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    to.to_string(),
                    token_address,
                    tokens_transferred
                )
                .execute(tx.as_mut())
                .await
                .context("Failed inserting or updating account balance from transfer event (to)")?;
            }

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_transfer_event)?,
            )
            .execute(tx.as_mut())
            .await?;
        }

        // - The `metadata_url` of a token is inserted/updated in the database here.
        // Only `TokenMetadata` events affect the `metadata_url` of a
        // token.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        cis2_token_metadata_event @ ScalarCis2TokenEvent::TokenMetadata(
            Cis2TokenMetadataEvent {
                raw_token_id,
                metadata_url,
            },
        ) => {
            let token_address = TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            // Since PostgreSQL Text data type does not support NUL we must replace these
            // before inserting. These are replaced by the a Unicode 'REPLACEMENT CHARACTER'
            // (U+FFFD).
            let metadata_url = metadata_url.url().replace('\0', "\u{FFFD}");
            // If the `token_address` does not exist, insert the new token.
            // If the `token_address` exists, update the `metadata_url` value in the
            // database.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, metadata_url, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        $4,
                        $5,
                        $6
                    )
                    ON CONFLICT (token_address)
                    DO UPDATE SET metadata_url = EXCLUDED.metadata_url",
                token_address,
                contract_index,
                contract_sub_index,
                metadata_url.as_str(),
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting or updating token from token metadata event")?;

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_token_metadata_event)?,
            )
            .execute(tx.as_mut())
            .await?;
        }
        _ => (),
    }
    Ok(())
}

struct PreparedScheduledReleases {
    account_address: Arc<String>,
    release_times: Vec<DateTime<Utc>>,
    amounts: Vec<i64>,
    target_account_balance_update: PreparedUpdateAccountBalance,
    source_account_balance_update: PreparedUpdateAccountBalance,
}

impl PreparedScheduledReleases {
    fn prepare(
        target_address: Arc<String>,
        source_address: String,
        scheduled_releases: &[(Timestamp, Amount)],
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let capacity = scheduled_releases.len();
        let mut release_times: Vec<DateTime<Utc>> = Vec::with_capacity(capacity);
        let mut amounts: Vec<i64> = Vec::with_capacity(capacity);
        let mut total_amount = 0;
        for (timestamp, amount) in scheduled_releases.iter() {
            release_times.push(DateTime::<Utc>::try_from(*timestamp)?);
            let micro_ccd = i64::try_from(amount.micro_ccd())?;
            amounts.push(micro_ccd);
            total_amount += micro_ccd;
        }
        let target_account_balance_update = PreparedUpdateAccountBalance::prepare(
            target_address.to_string(),
            total_amount,
            block_height,
            AccountStatementEntryType::TransferIn,
        )?;

        let source_account_balance_update = PreparedUpdateAccountBalance::prepare(
            source_address,
            -total_amount,
            block_height,
            AccountStatementEntryType::TransferOut,
        )?;

        Ok(Self {
            account_address: target_address,
            release_times,
            amounts,
            target_account_balance_update,
            source_account_balance_update,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO scheduled_releases (
                transaction_index,
                account_index,
                release_time,
                amount
            )
            SELECT
                $1,
                (SELECT index FROM accounts WHERE address = $2),
                UNNEST($3::TIMESTAMPTZ[]),
                UNNEST($4::BIGINT[])
            ",
            transaction_index,
            self.account_address.as_ref(),
            &self.release_times,
            &self.amounts
        )
        .execute(tx.as_mut())
        .await?;
        self.target_account_balance_update.save(tx, transaction_index).await?;
        self.source_account_balance_update.save(tx, transaction_index).await?;
        Ok(())
    }
}

/// Represents change in the balance of some account.
pub struct PreparedUpdateAccountBalance {
    /// Address of the account.
    account_address:   Arc<String>,
    /// Difference in the balance.
    change:            i64,
    account_statement: PreparedAccountStatement,
}

impl PreparedUpdateAccountBalance {
    fn prepare(
        sender: String,
        amount: i64,
        block_height: AbsoluteBlockHeight,
        transaction_type: AccountStatementEntryType,
    ) -> anyhow::Result<Self> {
        let account_statement = PreparedAccountStatement {
            block_height: block_height.height.try_into()?,
            amount,
            account_address: sender.clone(),
            transaction_type,
        };
        Ok(Self {
            account_address: Arc::new(sender),
            change: amount,
            account_statement,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE accounts SET amount = amount + $1 WHERE address = $2",
            self.change,
            self.account_address.as_ref(),
        )
        .execute(tx.as_mut())
        .await?;
        self.account_statement.save(tx, Some(transaction_index)).await?;
        Ok(())
    }
}

/// Represent the event of a transfer of CCD from one account to another.
pub struct PreparedCcdTransferEvent {
    /// Updating the sender account balance.
    update_sender:   PreparedUpdateAccountBalance,
    /// Updating the receivers account balance.
    update_receiver: PreparedUpdateAccountBalance,
}

impl PreparedCcdTransferEvent {
    pub fn prepare(
        sender_address: Arc<String>,
        receiver_address: Arc<String>,
        amount: i64,
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let update_sender = PreparedUpdateAccountBalance::prepare(
            sender_address.clone().to_string(),
            -amount,
            block_height,
            AccountStatementEntryType::TransferOut,
        )?;
        let update_receiver = PreparedUpdateAccountBalance::prepare(
            receiver_address.clone().to_string(),
            amount,
            block_height,
            AccountStatementEntryType::TransferIn,
        )?;
        Ok(Self {
            update_sender,
            update_receiver,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.update_sender.save(tx, transaction_index).await?;
        self.update_receiver.save(tx, transaction_index).await?;
        Ok(())
    }
}
