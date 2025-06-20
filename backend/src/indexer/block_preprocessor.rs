//! Contains the block traverse logic for the preprocessing step.
//!
//! This step will be run concurrently for a number of blocks, fetch information
//! and compute as much work as possible without depending on a database
//! connection, reducing the work needed during the sequential processing step.

use super::block::PreparedBlock;
use anyhow::Context;
use concordium_rust_sdk::{
    base::transactions::{BlockItem, EncodedPayload},
    common::types::Amount,
    indexer::TraverseError,
    types::{
        self as sdk_types, block_certificates::BlockCertificates, queries::BlockInfo,
        BlockItemSummary, ProtocolVersion, RewardsOverview, SpecialTransactionOutcome,
    },
    v2,
};
use futures::TryStreamExt as _;
use prometheus_client::{
    metrics::{counter::Counter, family::Family, gauge::Gauge, histogram},
    registry::Registry,
};
use tokio::{time::Instant, try_join};
use tracing::{debug, error, info};

/// State tracked during block preprocessing, this also holds the implementation
/// of [`Indexer`](concordium_rust_sdk::indexer::Indexer). Since several
/// preprocessors can run in parallel, this must be `Sync`.
pub struct BlockPreProcessor {
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
    node_response_time:           Family<NodeMetricLabels, histogram::Histogram>,
    /// Max number of acceptable successive failures before shutting down the
    /// service.
    max_successive_failures:      u64,
}
impl BlockPreProcessor {
    pub fn new(
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
        let node_response_time: Family<NodeMetricLabels, histogram::Histogram> =
            Family::new_with_constructor(|| {
                histogram::Histogram::new(histogram::exponential_buckets(0.010, 2.0, 10))
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

/// Represents the labels used for metrics related to Concordium Node.
#[derive(Clone, Debug, Hash, PartialEq, Eq, prometheus_client::encoding::EncodeLabelSet)]
pub struct NodeMetricLabels {
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

#[tonic::async_trait]
impl concordium_rust_sdk::indexer::Indexer for BlockPreProcessor {
    type Context = NodeMetricLabels;
    type Data = PreparedBlock;

    /// Called when a new connection is established to the given endpoint.
    /// The return value from this method is passed to each call of
    /// on_finalized.
    async fn on_connect<'a>(
        &mut self,
        endpoint: v2::Endpoint,
        client: &'a mut v2::Client,
    ) -> v2::QueryResult<Self::Context> {
        let info = client.get_consensus_info().await?;
        if info.genesis_block != self.genesis_hash {
            error!(
                "Invalid client: {} is on network with genesis hash {} expected {}",
                endpoint.uri(),
                info.genesis_block,
                self.genesis_hash
            );
            return Err(v2::QueryError::RPCError(v2::RPCError::CallError(
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
        fbi: v2::FinalizedBlockInfo,
    ) -> v2::QueryResult<Self::Data> {
        self.blocks_being_preprocessed.get_or_create(label).inc();
        debug!("Preprocessing block {}:{}", fbi.height, fbi.block_hash);
        // We block together the computation, so we can update the metric in the error
        // case, before returning early.
        let result = async move {
            let mut client1 = client.clone();
            let mut client2 = client.clone();
            let mut client3 = client.clone();
            let mut client4 = client.clone();
            let mut client5 = client.clone();
            let mut client6 = client.clone();
            let get_block_info = async move {
                let block_info = client1.get_block_info(fbi.height).await?.response;
                // Fetching the block certificates prior to P6 results in a InvalidArgument gRPC
                // error, so we produce the empty type of certificates instead.
                // The information is only used when preparing blocks for P8 and up.
                let certificates = if block_info.protocol_version < ProtocolVersion::P8 {
                    BlockCertificates {
                        quorum_certificate:       None,
                        timeout_certificate:      None,
                        epoch_finalization_entry: None,
                    }
                } else {
                    let response = client1.get_block_certificates(fbi.height).await?;
                    response.response
                };
                Ok((block_info, certificates))
            };

            let get_events = async move {
                let events = client2
                    .get_block_transaction_events(fbi.height)
                    .await?
                    .response
                    .try_collect::<Vec<_>>()
                    .await?;
                Ok(events)
            };

            let get_tokenomics_info = async move {
                let tokenomics_info = client3.get_tokenomics_info(fbi.height).await?.response;
                let total_staked_capital = match &tokenomics_info {
                    RewardsOverview::V0 {
                        ..
                    } => {
                        compute_total_stake_capital(
                            &mut client3,
                            v2::BlockIdentifier::AbsoluteHeight(fbi.height),
                        )
                        .await?
                    }
                    RewardsOverview::V1 {
                        total_staked_capital,
                        ..
                    } => *total_staked_capital,
                };
                Ok((tokenomics_info, total_staked_capital))
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
            let (
                (block_info, certificates),
                chain_parameters,
                (tokenomics_info, total_staked_capital),
                events,
                items,
                special_events,
            ) = try_join!(
                get_block_info,
                client6.get_block_chain_parameters(fbi.height),
                get_tokenomics_info,
                get_events,
                get_items,
                get_special_items
            )?;
            let node_response_time = start_fetching.elapsed();
            self.node_response_time.get_or_create(label).observe(node_response_time.as_secs_f64());
            let data = BlockData {
                finalized_block_info: fbi,
                block_info,
                events,
                items,
                chain_parameters: chain_parameters.response,
                tokenomics_info,
                total_staked_capital,
                special_events,
                certificates,
            };

            let prepared_block = PreparedBlock::prepare(&mut client, &data)
                .await
                .map_err(v2::RPCError::ParseError)?;
            Ok(prepared_block)
        }
        .await;
        self.blocks_being_preprocessed.get_or_create(label).dec();
        debug!("Preprocessing block {}:{} completed", fbi.height, fbi.block_hash);
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
pub async fn compute_total_stake_capital(
    client: &mut v2::Client,
    block_height: v2::BlockIdentifier,
) -> v2::QueryResult<Amount> {
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
            .map_err(v2::RPCError::ParseError)?
            .staked_amount();
    }
    Ok(total_staked_capital)
}

/// Raw block information fetched from a Concordium Node.
pub struct BlockData {
    pub finalized_block_info: v2::FinalizedBlockInfo,
    pub block_info:           BlockInfo,
    pub events:               Vec<BlockItemSummary>,
    pub items:                Vec<BlockItem<EncodedPayload>>,
    pub chain_parameters:     v2::ChainParameters,
    pub tokenomics_info:      RewardsOverview,
    pub total_staked_capital: Amount,
    pub special_events:       Vec<SpecialTransactionOutcome>,
    /// Certificates included in the block.
    pub certificates:         BlockCertificates,
}
