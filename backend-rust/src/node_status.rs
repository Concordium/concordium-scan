use reqwest::Client;
use serde::{Deserialize, Serialize};
use std::time::Duration;

#[derive(Serialize, Deserialize, Debug)]
pub struct NodeInfoStatus {
    pub nodeId:                   String,
    pub nodeName:                 Option<String>,
    pub averagePing:              Option<f64>,
    pub uptime:                   u64,
    pub clientVersion:            String,
    pub averageBytesPerSecondIn:  f64,
    pub averageBytesPerSecondOut: f64,
    pub packetsSent:              u64,
    pub packetsReceived:          u64,
    pub bakingCommitteeMember:    String,
    pub bestBlock:                String,
    pub bestBlockHeight:          u64,
    pub bestArrivedTime:          Option<String>,
    pub blockReceivePeriodEma:    Option<f64>,
    pub blockReceivePeriodEmsd:   Option<f64>,
    pub peersCount:               u64,
    pub peersList:                Vec<String>,
    pub finalizedBlock:           String,
    pub finalizedBlockHeight:     u64,
    pub finalizedTime:            Option<String>,
    pub finalizationPeriodEma:    Option<f64>,
    pub finalizationPeriodEmsd:   Option<f64>,
    pub consensusBakerId:         Option<u64>,
    pub blocksReceivedCount:      Option<u64>,
}

struct NodeCollectorBackend {
    client:  Client,
    origin:  String,
}

impl NodeCollectorBackend {
    pub fn new(client: Client, url: String) -> Self {
        Self {
            client,
            origin: url,
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
    use anyhow::Result;
    use concordium_rust_sdk::{
        base::{hashes::TransactionHash, smart_contracts::OwnedContractName},
        cis2::{MetadataUrl, TokenId},
        id::types::AccountAddress,
        types::{hashes::Hash, ContractAddress},
    };
    use reqwest::Client;
    use serde_json::json;
    use std::{cmp::PartialEq, str::FromStr, sync::Arc, time::Duration};
    #[tokio::test]
    async fn test_get_summary_positive() {
        let mut server = mockito::Server::new_async().await;

        let mock = server
            .mock("GET", "/nodesSummary")
            .with_status(200)
            .with_body(json!([  {
    "nodeName": "deapst",
    "nodeId": "6610b5f5f576be14",
    "peerType": "Node",
    "uptime": 5280818170 as u64,
    "client": "7.0.5",
    "averagePing": 329.166666666667,
    "peersCount": 6,
    "peersList": [
      "6636bc59a727509d",
      "9d7837d0daa19416",
      "3bea23bf2e9fa507",
      "fd02d5392cf47c6a",
      "afb7ee1c355f4005",
      "fffd80ef2ab13164"
    ],
    "bestBlock": "8b222acf69fd2affe9e5b65aaf7cec2a83e3336a3e13b0263c999f880ef50ce6",
  }]).to_string())
            .expect(1)
            .create_async()
            .await;

        let client = Client::new();
        let mut gc = NodeCollectorBackend::new(client, server.url());
        assert!(gc
            .get_summary()
            .await
            .is_ok());
        mock.assert();
    }
}
