use super::{ApiError, ApiResult};
use crate::connection::connection_from_slice;
use async_graphql::{connection, types, ComplexObject, Context, Enum, Object, SimpleObject};
use prometheus_client::{metrics::counter::Counter, registry::Registry};
use reqwest::{Client, StatusCode};
use serde::{Deserialize, Serialize};
use std::{cmp::Ordering::Equal, collections::HashMap, time::Duration};
use tokio::sync::watch::{Receiver, Sender};
use tokio_util::sync::CancellationToken;
use tracing::{error, info};

pub type NodeInfoReceiver = Receiver<Option<Vec<NodeStatus>>>;

#[derive(Default)]
pub(crate) struct QueryNodeStatus;

#[allow(clippy::too_many_arguments)]
#[Object]
impl QueryNodeStatus {
    async fn node_statuses(
        &self,
        ctx: &Context<'_>,
        sort_direction: NodeSortDirection,
        sort_field: NodeSortField,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, NodeStatus>> {
        let handler = ctx.data::<NodeInfoReceiver>().map_err(ApiError::NoReceiver)?;
        let mut statuses = if let Some(statuses) = handler.borrow().clone() {
            statuses
        } else {
            Err(ApiError::InternalError("Node collector backend has not responded".to_string()))?
        };

        statuses.sort_by(|a, b| {
            let ordering = match sort_field {
                NodeSortField::AveragePing => {
                    a.external.average_ping.partial_cmp(&b.external.average_ping)
                }
                NodeSortField::BlocksReceivedCount => {
                    a.external.blocks_received_count.partial_cmp(&b.external.blocks_received_count)
                }
                NodeSortField::ClientVersion => a.external.client.partial_cmp(&b.external.client),
                NodeSortField::ConsensusBakerId => {
                    a.external.consensus_baker_id.partial_cmp(&b.external.consensus_baker_id)
                }
                NodeSortField::FinalizedBlockHeight => a
                    .external
                    .finalized_block_height
                    .partial_cmp(&b.external.finalized_block_height),
                NodeSortField::NodeName => a.external.node_name.partial_cmp(&b.external.node_name),
                NodeSortField::PeersCount => {
                    a.external.peers_count.partial_cmp(&b.external.peers_count)
                }
                NodeSortField::Uptime => a.external.uptime.partial_cmp(&b.external.uptime),
            };

            match sort_direction {
                NodeSortDirection::Asc => ordering.unwrap_or(Equal),
                NodeSortDirection::Desc => ordering.unwrap_or(Equal).reverse(),
            }
        });
        connection_from_slice(statuses, first, after, last, before)
    }

    async fn node_status(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Return node with corresponding id")] id: types::ID,
    ) -> ApiResult<Option<NodeStatus>> {
        let handler = ctx.data::<NodeInfoReceiver>().map_err(ApiError::NoReceiver)?;
        let statuses_ref = handler.borrow();
        let statuses = statuses_ref.as_ref().ok_or(ApiError::InternalError(
            "Node collector backend has not responded".to_string(),
        ))?;
        let node = statuses.iter().find(|x| x.external.node_id == id.0).cloned();
        Ok(node)
    }
}

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum NodeSortField {
    AveragePing,
    BlocksReceivedCount,
    ClientVersion,
    ConsensusBakerId,
    FinalizedBlockHeight,
    NodeName,
    PeersCount,
    Uptime,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum NodeSortDirection {
    Asc,
    Desc,
}

pub struct Service {
    sender: Sender<Option<Vec<NodeStatus>>>,
    node_collector_backend: NodeCollectorBackendClient,
    pull_frequency: Duration,
    cancellation_token: CancellationToken,
    failed_node_status_fetch_counter: Counter,
}

impl Service {
    pub fn new(
        sender: Sender<Option<Vec<NodeStatus>>>,
        origin: &str,
        pull_frequency: Duration,
        client: Client,
        max_content_length: u64,
        cancellation_token: CancellationToken,
        registry: &mut Registry,
    ) -> Self {
        let failed_node_status_fetch_counter = Counter::default();
        registry.register(
            "failed_node_status_fetch_counter",
            "Number of failed attempts to retrieve data from the node status collector",
            failed_node_status_fetch_counter.clone(),
        );

        let node_collector_backend =
            NodeCollectorBackendClient::new(client, origin, max_content_length);
        Self {
            sender,
            node_collector_backend,
            pull_frequency,
            cancellation_token,
            failed_node_status_fetch_counter,
        }
    }

    pub async fn serve(self) -> anyhow::Result<()> {
        let mut interval = tokio::time::interval(self.pull_frequency);

        loop {
            tokio::select! {
                _ = interval.tick() => {
                    match self.node_collector_backend.get_summary().await {

                        Ok(external_node_info) => {
                            let map: HashMap<&str, &ExternalNodeStatus> = external_node_info.iter().map(|ns| (ns.node_id.as_str(), ns)).collect();
                            let node_info = external_node_info.iter().map(|node| {
                                let peers: Vec<PeerReference> = node.peers_list.iter().map(|node_id| {
                                        let peer: Option<Peer> = map.get(node_id.as_str()).map(|external| {
                                            Peer {
                                                node_id: external.node_id.to_string(),
                                                node_name: external.node_name.to_string()
                                            }
                                        });
                                        PeerReference {
                                            node_id: node_id.to_string(),
                                            node_status: peer
                                        }
                                    }).collect();
                                NodeStatus {
                                    external: node.clone(),
                                    peers_list: peers
                                }
                            }).collect();
                            if let Err(err) = self.sender.send(Some(node_info)) {
                                info!("Node status receiver has been closed: {:?}", err);
                                break;
                            }
                        }
                        Err(err) => {
                            self.failed_node_status_fetch_counter.inc();
                            error!("Error querying node summary: {}", err);
                        }
                    }
                }
                _ = self.cancellation_token.cancelled() => {
                    info!("Cancellation token triggered. Shutting down gracefully.");
                    break;
                }
            }
        }
        Ok(())
    }
}

#[allow(non_snake_case)]
#[derive(Serialize, Deserialize, Debug, PartialEq, Clone)]
#[serde(rename_all = "camelCase")]
#[derive(SimpleObject)]
#[graphql(complex)]
pub struct ExternalNodeStatus {
    pub average_bytes_per_second_in: f64,
    pub average_bytes_per_second_out: f64,
    pub average_ping: Option<f64>,
    #[graphql(skip)]
    pub client: String,
    pub baking_committee_member: String,
    pub best_arrived_time: Option<String>,
    pub best_block: String,
    pub best_block_baker_id: Option<u64>,
    pub best_block_central_bank_amount: Option<u64>,
    pub best_block_execution_cost: Option<u64>,
    pub best_block_height: u64,
    pub best_block_total_amount: Option<u64>,
    pub best_block_total_encrypted_amount: Option<u64>,
    pub best_block_transaction_count: Option<u64>,
    pub best_block_transaction_energy_cost: Option<u64>,
    pub best_block_transactions_size: Option<u64>,
    #[graphql(skip)]
    pub block_arrive_latency_EMA: Option<f64>,
    #[graphql(skip)]
    pub block_arrive_latency_EMSD: Option<f64>,
    #[graphql(skip)]
    pub block_arrive_period_EMA: Option<f64>,
    #[graphql(skip)]
    pub block_arrive_period_EMSD: Option<f64>,
    #[graphql(skip)]
    pub block_receive_latency_EMA: Option<f64>,
    #[graphql(skip)]
    pub block_receive_latency_EMSD: Option<f64>,
    #[graphql(skip)]
    pub block_receive_period_EMA: Option<f64>,
    #[graphql(skip)]
    pub block_receive_period_EMSD: Option<f64>,
    pub blocks_received_count: Option<u64>,
    pub blocks_verified_count: Option<u64>,
    pub consensus_baker_id: Option<u64>,
    pub consensus_running: bool,
    pub finalization_committee_member: bool,
    pub finalization_count: Option<u64>,
    #[graphql(skip)]
    pub finalization_period_EMA: Option<f64>,
    #[graphql(skip)]
    pub finalization_period_EMSD: Option<f64>,
    pub finalized_block: String,
    pub finalized_block_height: u64,
    pub finalized_block_parent: String,
    pub finalized_time: Option<String>,
    pub genesis_block: String,
    pub node_id: String,
    pub node_name: String,
    pub packets_received: u64,
    pub packets_sent: u64,
    pub peers_count: u64,
    pub peer_type: String,
    #[graphql(skip)]
    pub peers_list: Vec<String>,
    #[graphql(skip)]
    pub transactions_per_block_EMA: Option<f64>,
    #[graphql(skip)]
    pub transactions_per_block_EMSD: Option<f64>,
    pub uptime: u64,
}

#[ComplexObject]
impl ExternalNodeStatus {
    async fn id(&self) -> types::ID { types::ID::from(&self.node_id) }

    async fn client_version(&self) -> &str { &self.client }

    async fn block_arrive_latency_ema(&self) -> Option<f64> { self.block_arrive_latency_EMA }

    async fn block_arrive_latency_emsd(&self) -> Option<f64> { self.block_arrive_latency_EMSD }

    async fn block_arrive_period_ema(&self) -> Option<f64> { self.block_arrive_period_EMA }

    async fn block_arrive_period_emsd(&self) -> Option<f64> { self.block_arrive_period_EMSD }

    async fn block_receive_latency_ema(&self) -> Option<f64> { self.block_receive_latency_EMA }

    async fn block_receive_latency_emsd(&self) -> Option<f64> { self.block_receive_latency_EMSD }

    async fn block_receive_period_ema(&self) -> Option<f64> { self.block_receive_period_EMA }

    async fn block_receive_period_emsd(&self) -> Option<f64> { self.block_receive_period_EMSD }

    async fn finalization_period_ema(&self) -> Option<f64> { self.finalization_period_EMA }

    async fn finalization_period_emsd(&self) -> Option<f64> { self.finalization_period_EMSD }

    async fn transactions_per_block_ema(&self) -> Option<f64> { self.transactions_per_block_EMA }

    async fn transactions_per_block_emsd(&self) -> Option<f64> { self.transactions_per_block_EMSD }
}

#[derive(SimpleObject, Clone)]
#[graphql(complex)]
struct Peer {
    node_name: String,
    node_id:   String,
}

#[ComplexObject]
impl Peer {
    async fn id(&self) -> types::ID { types::ID::from(&self.node_id) }
}

#[derive(Clone)]
struct PeerReference {
    node_status: Option<Peer>,
    node_id:     String,
}
#[Object]
impl PeerReference {
    async fn node_status(&self) -> &Option<Peer> { &self.node_status }

    async fn node_id(&self) -> &str { &self.node_id }
}

#[derive(SimpleObject, Clone)]
pub struct NodeStatus {
    #[graphql(flatten)]
    pub(crate) external: ExternalNodeStatus,
    peers_list:          Vec<PeerReference>,
}

struct NodeCollectorBackendClient {
    client:             Client,
    url:                String,
    max_content_length: u64,
}

impl NodeCollectorBackendClient {
    pub fn new(client: Client, origin: &str, max_content_length: u64) -> Self {
        Self {
            client,
            url: format!("{}/nodesSummary", origin),
            max_content_length,
        }
    }

    async fn get_summary(&self) -> anyhow::Result<Vec<ExternalNodeStatus>> {
        let response = self
            .client
            .get(&self.url)
            .send()
            .await
            .map_err(|err| anyhow::anyhow!("Failed to send request: {:?}", err))?;

        if response.status() != StatusCode::OK {
            return Err(anyhow::anyhow!("Invalid status code, HTTP Status: {}", response.status()));
        }

        if let Some(content_length) = response.content_length() {
            if content_length > self.max_content_length {
                Err(anyhow::anyhow!(
                    "Response size {} exceeds the maximum allowed size of {} bytes",
                    content_length,
                    &self.max_content_length
                ))?
            }
        } else {
            Err(anyhow::anyhow!("Missing Content-Length header in response"))?
        }

        let node_info_statuses = response
            .json::<Vec<ExternalNodeStatus>>()
            .await
            .map_err(|err| anyhow::anyhow!("Failed to deserialize response: {}", err))?;

        Ok(node_info_statuses)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use reqwest::Client;
    use serde_json::from_str;

    #[tokio::test]
    async fn test_get_summary_positive() {
        let mut server = mockito::Server::new_async().await;
        let response = r#"
        [
            {
                "nodeId": "b185fa8ba28a6dbb",
                "nodeName": "001gpc-testnet",
                "peerType": "Node",
                "uptime": 64787412170,
                "client": "5.0.6",
                "peersCount": 0,
                "peersList": [],
                "bestBlock": "32d6fe8fd201639834e466f626847d7f1c58a315003202c5400216a865b15f01",
                "bestBlockHeight": 3656111,
                "bestBlockBakerId": 3,
                "bestArrivedTime": "2023-08-21T12:00:24.700370651Z",
                "blockArrivePeriodEMA": 16.6399989715912,
                "blockArrivePeriodEMSD": 15.5779083459107,
                "blockArriveLatencyEMA": 0.189644091592817,
                "blockArriveLatencyEMSD": 0.0504744217410237,
                "blockReceivePeriodEMA": 16.6397739628504,
                "blockReceivePeriodEMSD": 15.5781216338144,
                "blockReceiveLatencyEMA": 0.173204050259552,
                "blockReceiveLatencyEMSD": 0.0491522930027314,
                "finalizedBlock": "32d6fe8fd201639834e466f626847d7f1c58a315003202c5400216a865b15f01",
                "finalizedBlockHeight": 3656111,
                "finalizedTime": "2023-08-21T12:00:25.42453255Z",
                "finalizationPeriodEMA": 17.8069556222305,
                "finalizationPeriodEMSD": 15.1385586829404,
                "packetsSent": 574077850,
                "packetsReceived": 612037045,
                "consensusRunning": false,
                "bakingCommitteeMember": "NotInCommittee",
                "finalizationCommitteeMember": false,
                "transactionsPerBlockEMA": 0.002903832409826,
                "transactionsPerBlockEMSD": 0.0538089227467122,
                "bestBlockTransactionsSize": 0,
                "bestBlockTransactionCount": 0,
                "bestBlockTransactionEnergyCost": 0,
                "blocksReceivedCount": 2298046,
                "blocksVerifiedCount": 2298040,
                "genesisBlock": "4221332d34e1694168c2a0c0b3fd0f273809612cb13d000d5c2e00e85f50f796",
                "finalizationCount": 2076825,
                "finalizedBlockParent": "8ec52823feabbef291befdcdb006b0b0ef0020903b800fa9a6b830ac723b24bb",
                "averageBytesPerSecondIn": 2004,
                "averageBytesPerSecondOut": 2986
            }
        ]
        "#;
        let mock = server
            .mock("GET", "/nodesSummary")
            .with_status(200)
            .with_body(response)
            .expect(1)
            .create_async()
            .await;
        let deserialized: Vec<ExternalNodeStatus> =
            from_str(response).expect("Failed to deserialize JSON");
        let client = Client::new();
        let gc = NodeCollectorBackendClient::new(client, server.url().as_str(), 10000);
        let summary = gc.get_summary().await;
        assert!(summary.is_ok());
        assert_eq!(deserialized, summary.unwrap());
        mock.assert();
    }

    #[tokio::test]
    async fn test_get_invalid_response() {
        let mut server = mockito::Server::new_async().await;
        let response = r#"
        [
            {
            }
        ]
        "#;
        let mock = server
            .mock("GET", "/nodesSummary")
            .with_status(200)
            .with_body(response)
            .expect(1)
            .create_async()
            .await;
        let client = Client::new();
        let gc = NodeCollectorBackendClient::new(client, server.url().as_str(), 10000);
        let summary = gc.get_summary().await;
        assert!(summary.is_err());
        mock.assert();
    }

    #[tokio::test]
    async fn test_too_large_response() {
        let mut server = mockito::Server::new_async().await;
        let response = r#"
        [
            {
                "nodeId": "b185fa8ba28a6dbb",
                "nodeName": "001gpc-testnet",
                "peerType": "Node",
                "uptime": 64787412170,
                "client": "5.0.6",
                "peersCount": 0,
                "peersList": [],
                "bestBlock": "32d6fe8fd201639834e466f626847d7f1c58a315003202c5400216a865b15f01",
                "bestBlockHeight": 3656111,
                "bestBlockBakerId": 3,
                "bestArrivedTime": "2023-08-21T12:00:24.700370651Z",
                "blockArrivePeriodEMA": 16.6399989715912,
                "blockArrivePeriodEMSD": 15.5779083459107,
                "blockArriveLatencyEMA": 0.189644091592817,
                "blockArriveLatencyEMSD": 0.0504744217410237,
                "blockReceivePeriodEMA": 16.6397739628504,
                "blockReceivePeriodEMSD": 15.5781216338144,
                "blockReceiveLatencyEMA": 0.173204050259552,
                "blockReceiveLatencyEMSD": 0.0491522930027314,
                "finalizedBlock": "32d6fe8fd201639834e466f626847d7f1c58a315003202c5400216a865b15f01",
                "finalizedBlockHeight": 3656111,
                "finalizedTime": "2023-08-21T12:00:25.42453255Z",
                "finalizationPeriodEMA": 17.8069556222305,
                "finalizationPeriodEMSD": 15.1385586829404,
                "packetsSent": 574077850,
                "packetsReceived": 612037045,
                "consensusRunning": false,
                "bakingCommitteeMember": "NotInCommittee",
                "finalizationCommitteeMember": false,
                "transactionsPerBlockEMA": 0.002903832409826,
                "transactionsPerBlockEMSD": 0.0538089227467122,
                "bestBlockTransactionsSize": 0,
                "bestBlockTransactionCount": 0,
                "bestBlockTransactionEnergyCost": 0,
                "blocksReceivedCount": 2298046,
                "blocksVerifiedCount": 2298040,
                "genesisBlock": "4221332d34e1694168c2a0c0b3fd0f273809612cb13d000d5c2e00e85f50f796",
                "finalizationCount": 2076825,
                "finalizedBlockParent": "8ec52823feabbef291befdcdb006b0b0ef0020903b800fa9a6b830ac723b24bb",
                "averageBytesPerSecondIn": 2004,
                "averageBytesPerSecondOut": 2986
            }
        ]
        "#;
        let mock = server
            .mock("GET", "/nodesSummary")
            .with_status(200)
            .with_body(response)
            .expect(1)
            .create_async()
            .await;
        let client = Client::new();
        let gc = NodeCollectorBackendClient::new(client, server.url().as_str(), 1);
        let summary = gc.get_summary().await;
        assert!(summary.is_err());
        mock.assert();
    }

    #[tokio::test]
    async fn test_invalid_response_code() {
        let mut server = mockito::Server::new_async().await;
        let response = r#"
        [
            {
                "nodeId": "b185fa8ba28a6dbb",
                "nodeName": "001gpc-testnet",
                "peerType": "Node",
                "uptime": 64787412170,
                "client": "5.0.6",
                "peersCount": 0,
                "peersList": [],
                "bestBlock": "32d6fe8fd201639834e466f626847d7f1c58a315003202c5400216a865b15f01",
                "bestBlockHeight": 3656111,
                "bestBlockBakerId": 3,
                "bestArrivedTime": "2023-08-21T12:00:24.700370651Z",
                "blockArrivePeriodEMA": 16.6399989715912,
                "blockArrivePeriodEMSD": 15.5779083459107,
                "blockArriveLatencyEMA": 0.189644091592817,
                "blockArriveLatencyEMSD": 0.0504744217410237,
                "blockReceivePeriodEMA": 16.6397739628504,
                "blockReceivePeriodEMSD": 15.5781216338144,
                "blockReceiveLatencyEMA": 0.173204050259552,
                "blockReceiveLatencyEMSD": 0.0491522930027314,
                "finalizedBlock": "32d6fe8fd201639834e466f626847d7f1c58a315003202c5400216a865b15f01",
                "finalizedBlockHeight": 3656111,
                "finalizedTime": "2023-08-21T12:00:25.42453255Z",
                "finalizationPeriodEMA": 17.8069556222305,
                "finalizationPeriodEMSD": 15.1385586829404,
                "packetsSent": 574077850,
                "packetsReceived": 612037045,
                "consensusRunning": false,
                "bakingCommitteeMember": "NotInCommittee",
                "finalizationCommitteeMember": false,
                "transactionsPerBlockEMA": 0.002903832409826,
                "transactionsPerBlockEMSD": 0.0538089227467122,
                "bestBlockTransactionsSize": 0,
                "bestBlockTransactionCount": 0,
                "bestBlockTransactionEnergyCost": 0,
                "blocksReceivedCount": 2298046,
                "blocksVerifiedCount": 2298040,
                "genesisBlock": "4221332d34e1694168c2a0c0b3fd0f273809612cb13d000d5c2e00e85f50f796",
                "finalizationCount": 2076825,
                "finalizedBlockParent": "8ec52823feabbef291befdcdb006b0b0ef0020903b800fa9a6b830ac723b24bb",
                "averageBytesPerSecondIn": 2004,
                "averageBytesPerSecondOut": 2986
            }
        ]
        "#;
        let mock = server
            .mock("GET", "/nodesSummary")
            .with_status(404)
            .with_body(response)
            .expect(1)
            .create_async()
            .await;
        let client = Client::new();
        let gc = NodeCollectorBackendClient::new(client, server.url().as_str(), 10000);
        let summary = gc.get_summary().await;
        assert!(summary.is_err());
        mock.assert();
    }
}
