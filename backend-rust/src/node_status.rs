#![allow(non_snake_case)]
#![recursion_limit = "512"]
use reqwest::Client;
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug, PartialEq)]
#[serde(rename_all = "camelCase")]
pub struct NodeInfoStatus {
    pub node_id: String,
    pub node_name: Option<String>,
    pub average_ping: Option<f64>,
    pub uptime: u64,
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

struct NodeCollectorBackend {
    client: Client,
    origin: String,
}

impl NodeCollectorBackend {
    pub fn new(client: Client, origin: String) -> Self {
        println!("origin: {}", origin);
        Self {
            client,
            origin,
        }
    }

    async fn get_summary(&self) -> anyhow::Result<Vec<NodeInfoStatus>> {
        let response = self
            .client
            .get(format!("{}/nodesSummary", &self.origin))
            .send()
            .await
            .map_err(|err| anyhow::anyhow!("Failed to send request: {}", err))?;

        if !response.status().is_success() {
            return Err(anyhow::anyhow!(
                "Failed to fetch data, HTTP Status: {}",
                response.status()
            ));
        }

        let node_info_statuses = response
            .json::<Vec<NodeInfoStatus>>()
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
        let deserialized: Vec<NodeInfoStatus> = from_str(response).expect("Failed to deserialize JSON");
        let client = Client::new();
        let mut gc = NodeCollectorBackend::new(client, server.url());
        let summary = gc.get_summary().await;
        assert!(summary.is_ok());
        assert_eq!(deserialized, summary.unwrap());
        mock.assert();
    }
}
