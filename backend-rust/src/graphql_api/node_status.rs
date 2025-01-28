use super::{get_config, get_pool, ApiError, ApiResult};
use async_graphql::{connection, types, ComplexObject, Context, Enum, Object, SimpleObject};
use reqwest::Client;
use serde::{Deserialize, Serialize};
use std::{
    cmp::{min, Ordering::Equal},
    time::Duration,
};
use tokio::sync::watch::{Receiver, Sender};
use tokio_util::sync::CancellationToken;
use tracing::{error, info};

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
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, NodeStatus>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let handler =
            ctx.data::<Receiver<Option<Vec<NodeStatus>>>>().map_err(ApiError::NoReceiver)?;
        if first.is_some() && last.is_some() {
            return Err(ApiError::QueryConnectionFirstLast);
        }

        let mut statuses = if let Some(statuses) = handler.borrow().clone() {
            statuses
        } else {
            Err(ApiError::InternalError("Node collector backend has not responded".to_string()))?
        };

        statuses.sort_by(|a, b| {
            let ordering = match sort_field {
                NodeSortField::AveragePing => a.average_ping.partial_cmp(&b.average_ping),
                NodeSortField::BlocksReceivedCount => {
                    a.blocks_received_count.partial_cmp(&b.blocks_received_count)
                }
                NodeSortField::ClientVersion => a.client.partial_cmp(&b.client),
                NodeSortField::ConsensusBakerId => {
                    a.consensus_baker_id.partial_cmp(&b.consensus_baker_id)
                }
                NodeSortField::FinalizedBlockHeight => {
                    a.finalized_block_height.partial_cmp(&b.finalized_block_height)
                }
                NodeSortField::NodeName => a.node_name.partial_cmp(&b.node_name),
                NodeSortField::PeersCount => a.peers_count.partial_cmp(&b.peers_count),
                NodeSortField::Uptime => a.uptime.partial_cmp(&b.uptime),
            };

            match sort_direction {
                NodeSortDirection::Asc => ordering.unwrap_or(Equal),
                NodeSortDirection::Desc => ordering.unwrap_or(Equal).reverse(),
            }
        });

        let after_cursor_index = if let Some(after_cursor) = after {
            let index = after_cursor.parse::<u64>()?;
            index + 1
        } else {
            0
        };

        let length = statuses.len() as u64;

        let before_cursor_index = if let Some(before_cursor) = before {
            min(before_cursor.parse::<u64>()?, length)
        } else {
            length
        };

        let (range, has_previous_page, has_next_page) = if let Some(first_count) = first {
            (
                after_cursor_index..min(after_cursor_index + first_count, length),
                after_cursor_index > 0,
                after_cursor_index + first_count < length,
            )
        } else if let Some(last_count) = last {
            (
                before_cursor_index.saturating_sub(last_count)..before_cursor_index,
                before_cursor_index > last_count,
                before_cursor_index < length,
            )
        } else {
            (
                after_cursor_index..before_cursor_index,
                after_cursor_index > 0,
                before_cursor_index < length,
            )
        };
        let mut connection: connection::Connection<String, NodeStatus> =
            connection::Connection::new(has_previous_page, has_next_page);
        for i in range {
            let value = statuses[i as usize].clone();
            connection.edges.push(connection::Edge::new(format!("{}", i), value));
        }

        Ok(connection)
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
    sender:                 Sender<Option<Vec<NodeStatus>>>,
    node_collector_backend: NodeCollectorBackendClient,
    pull_frequency:         Duration,
    cancellation_token:     CancellationToken,
}

impl Service {
    pub fn new(
        sender: Sender<Option<Vec<NodeStatus>>>,
        origin: &str,
        pull_frequency: Duration,
        client: Client,
        cancellation_token: CancellationToken,
    ) -> Self {
        let node_collector_backend = NodeCollectorBackendClient::new(client, origin);
        Self {
            sender,
            node_collector_backend,
            pull_frequency,
            cancellation_token,
        }
    }

    pub async fn serve(self) -> anyhow::Result<()> {
        let mut interval = tokio::time::interval(self.pull_frequency);

        loop {
            tokio::select! {
                _ = interval.tick() => {
                    match self.node_collector_backend.get_summary().await {

                        Ok(node_info) => {
                            if let Err(err) = self.sender.send(Some(node_info)) {
                                info!("Node status receiver has been closed");
                                break;
                            }
                        }
                        Err(err) => {
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

#[derive(Serialize, Deserialize, Debug, PartialEq, Clone)]
#[serde(rename_all = "camelCase")]
#[derive(SimpleObject)]
#[graphql(complex)]
pub struct NodeStatus {
    pub node_id: String,
    pub node_name: Option<String>,
    pub average_ping: Option<f64>,
    pub uptime: u64,
    #[graphql(skip)]
    pub client: String,
    pub average_bytes_per_second_in: f64,
    pub average_bytes_per_second_out: f64,
    pub packets_sent: u64,
    pub packets_received: u64,
    pub baking_committee_member: String,
    pub best_block: String,
    pub best_block_height: u64,
    pub best_arrived_time: Option<String>,
    pub block_receive_period_ema: Option<f64>,
    pub block_receive_period_emsd: Option<f64>,
    pub peers_count: u64,
    pub peers_list: Vec<String>,
    pub finalized_block: String,
    pub finalized_block_height: u64,
    pub finalized_time: Option<String>,
    pub finalization_period_ema: Option<f64>,
    pub finalization_period_emsd: Option<f64>,
    pub consensus_baker_id: Option<u64>,
    pub blocks_received_count: Option<u64>,
}

#[ComplexObject]
impl NodeStatus {
    async fn id(&self) -> types::ID { types::ID::from(self.node_id.clone()) }

    async fn client_version(&self) -> String { self.client.to_string() }
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

    async fn get_summary(&self) -> anyhow::Result<Vec<NodeStatus>> {
        let response = self
            .client
            .get(&self.url)
            .send()
            .await
            .map_err(|err| anyhow::anyhow!("Failed to send request: {:?}", err))?;

        if !response.status().is_success() {
            return Err(anyhow::anyhow!(
                "Failed to fetch data, HTTP Status: {}",
                response.status()
            ));
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
            Err(anyhow::anyhow!(
                "Missing Content-Length header in response from node backend collector"
            ))?
        }

        let node_info_statuses = response
            .json::<Vec<NodeStatus>>()
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
        let deserialized: Vec<NodeStatus> = from_str(response).expect("Failed to deserialize JSON");
        let client = Client::new();
        let gc = NodeCollectorBackendClient::new(client, server.url().as_str());
        let summary = gc.get_summary().await;
        assert!(summary.is_ok());
        assert_eq!(deserialized, summary.unwrap());
        mock.assert();
    }
}
