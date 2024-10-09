use crate::graphql_api::{
    events_from_summary, AccountTransactionType, BakerPoolOpenStatus,
    CredentialDeploymentTransactionType, DbTransactionType, UpdateTransactionType,
};
use anyhow::Context;
use chrono::NaiveDateTime;
use concordium_rust_sdk::{
    base::hashes::ModuleReference,
    indexer::{async_trait, Indexer, ProcessEvent, TraverseConfig, TraverseError},
    types::{
        self as sdk_types, queries::BlockInfo, AccountStakingInfo, AccountTransactionDetails,
        AccountTransactionEffects, BlockItemSummary, BlockItemSummaryDetails,
        PartsPerHundredThousands, RewardsOverview,
    },
    v2::{self, ChainParameters, FinalizedBlockInfo, QueryError, QueryResult, RPCError},
};
use futures::{StreamExt, TryStreamExt};
use prometheus_client::{
    metrics::{
        counter::Counter,
        family::Family,
        gauge::Gauge,
        histogram::{self, Histogram},
    },
    registry::Registry,
};
use serde_json::Value;
use sqlx::PgPool;
use tokio::{time::Instant, try_join};
use tokio_util::sync::CancellationToken;
use tracing::{error, info, warn};

/// Service traversing each block of the chain, indexing it into a database.
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
            r#"
SELECT height FROM blocks ORDER BY height DESC LIMIT 1
"#
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
            sqlx::query!(r#"SELECT hash FROM blocks WHERE height=0"#)
                .fetch_one(&pool)
                .await?
                .hash
                .parse()?;

        let block_pre_processor = BlockPreProcessor::new(
            genesis_block_hash,
            registry.sub_registry_with_prefix("preprocessor"),
        );
        let block_processor =
            BlockProcessor::new(pool, registry.sub_registry_with_prefix("processor")).await?;

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
        let mut new_endpoints = Vec::new();
        for endpoint in &self.endpoints {
            if endpoint
                .uri()
                .scheme()
                .map_or(false, |x| x == &concordium_rust_sdk::v2::Scheme::HTTPS)
            {
                let new_endpoint = endpoint
                    .clone()
                    .tls_config(tonic::transport::ClientTlsConfig::new())
                    .context("Unable to construct TLS configuration for the Concordium node.")?;
                new_endpoints.push(new_endpoint);
            } else {
                new_endpoints.push(endpoint.clone());
            }
        }

        let traverse_config = TraverseConfig::new(self.endpoints, self.start_height.into())
            .context("Failed setting up TraverseConfig")?
            .set_max_parallel(self.config.max_parallel_block_preprocessors)
            .set_max_behind(std::time::Duration::from_secs(self.config.node_max_behind));
        let processor_config = concordium_rust_sdk::indexer::ProcessorConfig::new()
            .set_stop_signal(cancel_token.cancelled_owned());

        let (sender, receiver) = tokio::sync::mpsc::channel(self.config.max_processing_batch);
        let receiver = tokio_stream::wrappers::ReceiverStream::from(receiver)
            .ready_chunks(self.config.max_processing_batch);
        let traverse_future = traverse_config.traverse(self.block_pre_processor, sender);
        let process_future = processor_config.process_event_stream(self.block_processor, receiver);
        info!("Indexing from block height {}", self.start_height);
        let (result, ()) = futures::join!(traverse_future, process_future);
        Ok(result?)
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
}
impl BlockPreProcessor {
    fn new(genesis_hash: sdk_types::hashes::BlockHash, registry: &mut Registry) -> Self {
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
        let result = async move {
            let mut client1 = client.clone();
            let mut client2 = client.clone();
            let mut client3 = client.clone();
            let get_events = async move {
                let events = client3
                    .get_block_transaction_events(fbi.height)
                    .await?
                    .response
                    .try_collect::<Vec<_>>()
                    .await?;
                Ok(events)
            };

            let start_fetching = Instant::now();
            let (block_info, chain_parameters, events, tokenomics_info) = try_join!(
                client1.get_block_info(fbi.height),
                client2.get_block_chain_parameters(fbi.height),
                get_events,
                client.get_tokenomics_info(fbi.height)
            )
            .map_err(|err| err)?;
            let node_response_time = start_fetching.elapsed();
            self.node_response_time.get_or_create(label).observe(node_response_time.as_secs_f64());

            let data = BlockData {
                finalized_block_info: fbi,
                block_info: block_info.response,
                events,
                chain_parameters: chain_parameters.response,
                tokenomics_info: tokenomics_info.response,
            };
            let prepared_block = PreparedBlock::prepare(&data, &client)
                .await
                .map_err(|err| RPCError::ParseError(err))?;
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
        true
    }
}

/// Type implementing the `ProcessEvent` handling the insertion of prepared
/// blocks.
struct BlockProcessor {
    /// Database connection pool
    pool: PgPool,
    /// The last finalized block height according to the latest indexed block.
    /// This is needed in order to compute the finalization time of blocks.
    last_finalized_height: i64,
    /// The last finalized block hash according to the latest indexed block.
    /// This is needed in order to compute the finalization time of blocks.
    last_finalized_hash: String,
    /// Metric counting how many blocks was saved to the database successfully.
    blocks_processed: Counter,
    /// Metric counting the total number of failed attempts to process
    /// blocks.
    processing_failures: Counter,
    /// Histogram collecting the time it took to process a block.
    processing_duration_seconds: Histogram,
}
impl BlockProcessor {
    /// Construct the block processor by loading the initial state from the
    /// database. This assumes at least the genesis block is in the
    /// database.
    async fn new(pool: PgPool, registry: &mut Registry) -> anyhow::Result<Self> {
        let rec = sqlx::query!(
            r#"
SELECT height, hash FROM blocks WHERE finalization_time IS NULL ORDER BY height ASC LIMIT 1
"#
        )
        .fetch_one(&pool)
        .await
        .context("Failed to query data for save context")?;

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
            last_finalized_height: rec.height,
            last_finalized_hash: rec.hash,
            blocks_processed,
            processing_failures,
            processing_duration_seconds,
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
        for data in batch {
            data.save(&mut self, &mut tx).await.context("Failed saving block")?;
            out.push_str(format!("\n- {}:{}", data.height, data.hash).as_str())
        }
        tx.commit().await.context("Failed to commit SQL transaction")?;
        let duration = start_time.elapsed();
        self.processing_duration_seconds.observe(duration.as_secs_f64());
        self.blocks_processed.inc_by(u64::try_from(batch.len())?);
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
        info!("Failed processing {} times in row: {}", successive_failures, error);
        self.processing_failures.inc();
        Ok(true)
    }
}

/// Raw block information fetched from a Concordium Node.
struct BlockData {
    finalized_block_info: FinalizedBlockInfo,
    block_info:           BlockInfo,
    events:               Vec<BlockItemSummary>,
    chain_parameters:     ChainParameters,
    tokenomics_info:      RewardsOverview,
}

/// Function for initializing the database with the genesis block.
/// This should only be called if the database is empty.
async fn save_genesis_data(endpoint: v2::Endpoint, pool: &PgPool) -> anyhow::Result<()> {
    let mut client = v2::Client::new(endpoint).await?;
    let genesis_height = v2::BlockIdentifier::AbsoluteHeight(0.into());

    let mut tx = pool.begin().await.context("Failed to create SQL transaction")?;

    let genesis_block_info = client.get_block_info(genesis_height).await?.response;
    let block_hash = genesis_block_info.block_hash.to_string();
    let slot_time = genesis_block_info.block_slot_time.naive_utc();
    let baker_id = if let Some(index) = genesis_block_info.block_baker {
        Some(i64::try_from(index.id.index)?)
    } else {
        None
    };
    let genesis_tokenomics = client.get_tokenomics_info(genesis_height).await?.response;
    let common_reward = match genesis_tokenomics {
        RewardsOverview::V0 {
            data,
        } => data,
        RewardsOverview::V1 {
            common,
            ..
        } => common,
    };
    let total_staked = match genesis_tokenomics {
        RewardsOverview::V0 {
            ..
        } => {
            // TODO Compute the total staked capital.
            0i64
        }
        RewardsOverview::V1 {
            total_staked_capital,
            ..
        } => i64::try_from(total_staked_capital.micro_ccd())?,
    };

    let total_amount = i64::try_from(common_reward.total_amount.micro_ccd())?;
    sqlx::query!(
            r#"INSERT INTO blocks (height, hash, slot_time, block_time, baker_id, total_amount, total_staked) VALUES ($1, $2, $3, 0, $4, $5, $6);"#,
            0,
            block_hash,
            slot_time,
            baker_id,
        total_amount,
        total_staked
        )
        .execute(&mut *tx)
            .await?;

    let mut genesis_accounts = client.get_account_list(genesis_height).await?.response;
    while let Some(account) = genesis_accounts.try_next().await? {
        let info = client.get_account_info(&account.into(), genesis_height).await?.response;
        let index = i64::try_from(info.account_index.index)?;
        let account_address = account.to_string();
        let amount = i64::try_from(info.account_amount.micro_ccd)?;
        sqlx::query!(
            r#"INSERT INTO accounts (index, address, created_block, amount)
        VALUES ($1, $2, $3, $4)"#,
            index,
            account_address,
            0,
            amount
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
                r#"INSERT INTO bakers (id, staked, restake_earnings, open_status, metadata_url, transaction_commission, baking_commission, finalization_commission)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8)"#,
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
    hash:                 String,
    height:               i64,
    slot_time:            NaiveDateTime,
    baker_id:             Option<i64>,
    total_amount:         i64,
    total_staked:         i64,
    block_last_finalized: String,
    prepared_block_items: Vec<PreparedBlockItem>,
}

impl PreparedBlock {
    async fn prepare(data: &BlockData, node_client: &v2::Client) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let hash = data.finalized_block_info.block_hash.to_string();
        let block_last_finalized = data.block_info.block_last_finalized.to_string();
        let slot_time = data.block_info.block_slot_time.naive_utc();
        let baker_id = if let Some(index) = data.block_info.block_baker {
            Some(i64::try_from(index.id.index)?)
        } else {
            None
        };
        let common_reward_data = match data.tokenomics_info {
            RewardsOverview::V0 {
                data,
            } => data,
            RewardsOverview::V1 {
                common,
                ..
            } => common,
        };
        let total_amount = i64::try_from(common_reward_data.total_amount.micro_ccd())?;
        let total_staked = match data.tokenomics_info {
            RewardsOverview::V0 {
                ..
            } => {
                // TODO Compute the total staked capital.
                0i64
            }
            RewardsOverview::V1 {
                total_staked_capital,
                ..
            } => i64::try_from(total_staked_capital.micro_ccd())?,
        };

        let mut prepared_block_items = Vec::new();
        for (event_index, block_item) in data.events.iter().enumerate() {
            prepared_block_items.push(
                PreparedBlockItem::prepare(
                    data,
                    block_item,
                    event_index.try_into()?,
                    node_client.clone(),
                )
                .await?,
            )
        }

        Ok(Self {
            hash,
            height,
            slot_time,
            baker_id,
            total_amount,
            total_staked,
            block_last_finalized,
            prepared_block_items,
        })
    }

    async fn save(
        &self,
        context: &mut BlockProcessor,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
                    r#"INSERT INTO blocks (height, hash, slot_time, block_time, baker_id, total_amount, total_staked)
        VALUES ($1, $2, $3,
          (SELECT EXTRACT("MILLISECONDS" FROM $3 - b.slot_time) FROM blocks b WHERE b.height=($1 - 1::bigint)),
          $4, $5, $6);"#,
                    self.height,
                    self.hash,
                    self.slot_time,
                    self.baker_id,
                    self.total_amount,
                    self.total_staked
                )
                .execute(tx.as_mut())
                    .await?;

        // Check if this block knows of a new finalized block.
        // If so, mark the blocks since last time as finalized by this block.
        if self.block_last_finalized != context.last_finalized_hash {
            let last_height = context.last_finalized_height;

            let rec = sqlx::query!(
                r#"
        WITH finalizer
           AS (SELECT height FROM blocks WHERE hash = $1)
        UPDATE blocks b
           SET finalization_time = EXTRACT("MILLISECONDS" FROM $3 - b.slot_time),
               finalized_by = finalizer.height
        FROM finalizer
        WHERE $2 <= b.height AND b.height < finalizer.height
        RETURNING finalizer.height"#,
                self.block_last_finalized,
                last_height,
                self.slot_time
            )
            .fetch_one(tx.as_mut())
            .await
            .context("Failed updating finalization_time")?;

            // TODO: Updating the context should be done, when we know nothing has failed.
            context.last_finalized_height = rec.height;
            context.last_finalized_hash = self.block_last_finalized.clone();
        }

        for item in self.prepared_block_items.iter() {
            item.save(tx).await?;
        }
        Ok(())
    }
}

struct PreparedBlockItem {
    block_index:      i64,
    tx_hash:          String,
    ccd_cost:         i64,
    energy_cost:      i64,
    height:           i64,
    sender:           Option<String>,
    transaction_type: DbTransactionType,
    account_type:     Option<AccountTransactionType>,
    credential_type:  Option<CredentialDeploymentTransactionType>,
    update_type:      Option<UpdateTransactionType>,
    success:          bool,
    events:           Option<serde_json::Value>,
    reject:           Option<serde_json::Value>,
    // This is an option temporarily, until we are able to handle every type of event.
    prepared_event:   Option<PreparedEvent>,
}

impl PreparedBlockItem {
    async fn prepare(
        data: &BlockData,
        block_item: &BlockItemSummary,
        event_index: i64,
        node_client: v2::Client,
    ) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let block_index = i64::try_from(block_item.index.index)?;
        let tx_hash = block_item.hash.to_string();
        let ccd_cost =
            i64::try_from(data.chain_parameters.ccd_cost(block_item.energy_cost).micro_ccd)?;
        let energy_cost = i64::try_from(block_item.energy_cost.energy)?;
        let sender = block_item.sender_account().map(|a| a.to_string());
        let (transaction_type, account_type, credential_type, update_type) =
            match &block_item.details {
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
        let success = block_item.is_success();
        let (events, reject) = if success {
            let events = serde_json::to_value(&events_from_summary(block_item.details.clone())?)?;
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
                }) = &block_item.details
                {
                    serde_json::to_value(crate::graphql_api::TransactionRejectReason::try_from(
                        reject_reason.clone(),
                    )?)?
                } else {
                    anyhow::bail!("Invariant violation: Failed transaction without a reject reason")
                };
            (None, Some(reject))
        };

        let prepared_event =
            PreparedEvent::prepare(&data, &block_item, event_index, node_client).await?;

        Ok(Self {
            block_index,
            tx_hash,
            ccd_cost,
            energy_cost,
            height,
            sender,
            transaction_type,
            account_type,
            credential_type,
            update_type,
            success,
            events,
            reject,
            prepared_event,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
                        r#"INSERT INTO transactions
        (index, hash, ccd_cost, energy_cost, block, sender, type, type_account, type_credential_deployment, type_update, success, events, reject)
        VALUES
        ($1, $2, $3, $4, $5, (SELECT index FROM accounts WHERE address=$6), $7, $8, $9, $10, $11, $12, $13);"#,
                    self.block_index,
                    self.tx_hash,
                    self.ccd_cost,
                    self.energy_cost,
                    self.height,
                    self.sender,
                    self.transaction_type as DbTransactionType,
                    self.account_type as Option<AccountTransactionType>,
                    self.credential_type as Option<CredentialDeploymentTransactionType>,
                    self.update_type as Option<UpdateTransactionType>,
                    self.success,
                    self.events,
                    self.reject)
                    .execute(tx.as_mut())
                    .await?;

        if let Some(prepared_event) = &self.prepared_event {
            prepared_event.save(tx).await?;
        }
        Ok(())
    }
}

enum PreparedEvent {
    AccountCreation(PreparedAccountCreation),
    BakerEvents(Vec<PreparedBakerEvent>),
    ModuleDeployed(PreparedModuleDeployed),
    NoOperation,
}
impl PreparedEvent {
    async fn prepare(
        data: &BlockData,
        block_item: &BlockItemSummary,
        event_index: i64,
        node_client: v2::Client,
    ) -> anyhow::Result<Option<Self>> {
        let prepared_event = match &block_item.details {
            BlockItemSummaryDetails::AccountCreation(details) => {
                Some(PreparedEvent::AccountCreation(PreparedAccountCreation::prepare(
                    data,
                    &block_item,
                    details,
                )?))
            }
            BlockItemSummaryDetails::AccountTransaction(details) => match &details.effects {
                AccountTransactionEffects::None {
                    transaction_type,
                    reject_reason,
                } => None,
                AccountTransactionEffects::ModuleDeployed {
                    module_ref,
                } => Some(PreparedEvent::ModuleDeployed(
                    PreparedModuleDeployed::prepare(
                        data,
                        &block_item,
                        event_index,
                        *module_ref,
                        node_client,
                    )
                    .await?,
                )),
                AccountTransactionEffects::ContractInitialized {
                    ///////////// TODO
                    data,
                } => None,
                AccountTransactionEffects::ContractUpdateIssued {
                    effects,
                } => None,
                AccountTransactionEffects::AccountTransfer {
                    amount,
                    to,
                } => None,
                AccountTransactionEffects::AccountTransferWithMemo {
                    amount,
                    to,
                    memo,
                } => None,
                AccountTransactionEffects::BakerAdded {
                    data: event_data,
                } => {
                    let event = concordium_rust_sdk::types::BakerEvent::BakerAdded {
                        data: event_data.clone(),
                    };
                    let prepared = PreparedBakerEvent::prepare(&event)?;
                    Some(PreparedEvent::BakerEvents(vec![prepared]))
                }
                AccountTransactionEffects::BakerRemoved {
                    baker_id,
                } => {
                    let event = concordium_rust_sdk::types::BakerEvent::BakerRemoved {
                        baker_id: *baker_id,
                    };
                    let prepared = PreparedBakerEvent::prepare(&event)?;
                    Some(PreparedEvent::BakerEvents(vec![prepared]))
                }
                AccountTransactionEffects::BakerStakeUpdated {
                    data: update,
                } => {
                    if let Some(update) = update {
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
                        Some(PreparedEvent::BakerEvents(vec![prepared]))
                    } else {
                        Some(PreparedEvent::NoOperation)
                    }
                }
                AccountTransactionEffects::BakerRestakeEarningsUpdated {
                    baker_id,
                    restake_earnings,
                } => {
                    let prepared = PreparedEvent::BakerEvents(vec![PreparedBakerEvent::prepare(
                        &concordium_rust_sdk::types::BakerEvent::BakerRestakeEarningsUpdated {
                            baker_id:         *baker_id,
                            restake_earnings: *restake_earnings,
                        },
                    )?]);
                    Some(prepared)
                }
                AccountTransactionEffects::BakerKeysUpdated {
                    ..
                } => Some(PreparedEvent::NoOperation),
                AccountTransactionEffects::BakerConfigured {
                    data: events,
                } => Some(PreparedEvent::BakerEvents(
                    events
                        .iter()
                        .map(|event| PreparedBakerEvent::prepare(event))
                        .collect::<anyhow::Result<Vec<_>>>()?,
                )),

                AccountTransactionEffects::EncryptedAmountTransferred {
                    removed,
                    added,
                } => None,
                AccountTransactionEffects::EncryptedAmountTransferredWithMemo {
                    removed,
                    added,
                    memo,
                } => None,
                AccountTransactionEffects::TransferredToEncrypted {
                    data,
                } => None,
                AccountTransactionEffects::TransferredToPublic {
                    removed,
                    amount,
                } => None,
                AccountTransactionEffects::TransferredWithSchedule {
                    to,
                    amount,
                } => None,
                AccountTransactionEffects::TransferredWithScheduleAndMemo {
                    to,
                    amount,
                    memo,
                } => None,
                AccountTransactionEffects::CredentialKeysUpdated {
                    cred_id,
                } => None,
                AccountTransactionEffects::CredentialsUpdated {
                    new_cred_ids,
                    removed_cred_ids,
                    new_threshold,
                } => None,
                AccountTransactionEffects::DataRegistered {
                    data,
                } => None,

                AccountTransactionEffects::DelegationConfigured {
                    data,
                } => None,
            },
            details => {
                warn!("details = \n {:#?}", details);
                None
            }
        };
        Ok(prepared_event)
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        match self {
            PreparedEvent::AccountCreation(event) => event.save(tx).await,
            PreparedEvent::BakerEvents(events) => {
                for event in events {
                    event.save(tx).await?;
                }
                Ok(())
            }
            PreparedEvent::ModuleDeployed(module) => module.save(tx).await,
            PreparedEvent::NoOperation => Ok(()),
        }
    }
}

struct PreparedModuleDeployed {
    height:             i64,
    module_reference:   String,
    source:             Vec<u8>,
    schema:             Vec<u8>,
    schema_version:     i32,
    contract_names:     Value,
    entrypoint_names:   Value,
    // TODO:  contract_instances: Vec<(i64, i64)>,
    contract_instances: Value,
    creator:            String,
    tx_hash:            String,
    tx_index:           i64,
    tx_event_index:     i64,
    created_at:         NaiveDateTime,
}

impl PreparedModuleDeployed {
    async fn prepare(
        data: &BlockData,
        block_item: &BlockItemSummary,
        tx_event_index: i64,
        module_reference: ModuleReference,
        mut node_client: v2::Client,
    ) -> anyhow::Result<Self> {
        let block_height = data.finalized_block_info.height;
        let tx_hash = block_item.hash.to_string();
        let tx_index = block_item.index.index.try_into()?;
        let created_at = data.block_info.block_slot_time.naive_utc();
        let creator =
            block_item.sender_account().map(|a| a.to_string()).ok_or(anyhow::Error::msg(
                "Deploying a module transaction has an sender account. This error should not \
                 happen.",
            ))?;

        // TODO:
        // let source = node_client
        //     .get_module_source(&module_reference,
        // BlockIdentifier::AbsoluteHeight(block_height))     .await?
        //     .response
        //     .source
        //     .into();

        // TODO
        let source = vec![];
        let schema = vec![];
        let schema_version = 0;
        let contract_names = serde_json::to_value(vec![""])?;
        let entrypoint_names = serde_json::to_value(vec![""])?;
        let contract_instances = serde_json::to_value(vec![""])?;

        Ok(Self {
            height: i64::try_from(block_height.height)?,
            module_reference: module_reference.into(),
            source,
            schema,
            schema_version,
            contract_names,
            entrypoint_names,
            contract_instances,
            creator,
            tx_hash,
            tx_index,
            tx_event_index,
            created_at,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            r#"INSERT INTO wasm_modules (
                module_reference,
                source,
                schema,
                schema_version,
                contract_names,
                entrypoint_names,
                contract_instances,
                creator,
                created_block,
                created_tx_hash,
                created_tx_index,
                created_tx_event_index,
                created_at
                )
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)"#,
            self.module_reference,
            self.source,
            self.schema,
            self.schema_version,
            self.contract_names,
            self.entrypoint_names,
            self.contract_instances,
            self.creator,
            self.height,
            self.tx_hash,
            self.tx_index,
            self.tx_event_index,
            self.created_at.into()
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedAccountCreation {
    account_address: String,
    height:          i64,
    block_index:     i64,
}

impl PreparedAccountCreation {
    fn prepare(
        data: &BlockData,
        block_item: &BlockItemSummary,
        details: &concordium_rust_sdk::types::AccountCreationDetails,
    ) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let block_index = i64::try_from(block_item.index.index)?;
        Ok(Self {
            account_address: details.address.to_string(),
            height,
            block_index,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            r#"INSERT INTO accounts (index, address, created_block, created_index, amount)
VALUES ((SELECT COALESCE(MAX(index) + 1, 0) FROM accounts), $1, $2, $3, 0)"#,
            self.account_address,
            self.height,
            self.block_index
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

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
                    r#"
INSERT INTO bakers (id, staked, restake_earnings)
VALUES ($1, $2, $3)
"#,
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
                sqlx::query!(r#"DELETE FROM bakers WHERE id=$1"#, baker_id,)
                    .execute(tx.as_mut())
                    .await?;
            }
            PreparedBakerEvent::StakeIncrease {
                baker_id,
                staked,
            } => {
                sqlx::query!(r#"UPDATE bakers SET staked = $2 WHERE id=$1"#, baker_id, staked,)
                    .execute(tx.as_mut())
                    .await?;
            }
            PreparedBakerEvent::StakeDecrease {
                baker_id,
                staked,
            } => {
                sqlx::query!(r#"UPDATE bakers SET staked = $2 WHERE id=$1"#, baker_id, staked,)
                    .execute(tx.as_mut())
                    .await?;
            }
            PreparedBakerEvent::SetRestakeEarnings {
                baker_id,
                restake_earnings,
            } => {
                sqlx::query!(
                    r#"UPDATE bakers SET restake_earnings = $2 WHERE id=$1"#,
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
                    r#"UPDATE bakers SET open_status = $2 WHERE id=$1"#,
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
                    r#"UPDATE bakers SET metadata_url = $2 WHERE id=$1"#,
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
                    r#"UPDATE bakers SET transaction_commission = $2 WHERE id=$1"#,
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
                    r#"UPDATE bakers SET baking_commission = $2 WHERE id=$1"#,
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
                    r#"UPDATE bakers SET finalization_commission = $2 WHERE id=$1"#,
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?;
            }
            PreparedBakerEvent::RemoveDelegation {
                delegator_id: _,
            } => {
                // TODO: Implement this when database is tracking delegation as well.
                todo!()
            }
            PreparedBakerEvent::NoOperation => (),
        }
        Ok(())
    }
}