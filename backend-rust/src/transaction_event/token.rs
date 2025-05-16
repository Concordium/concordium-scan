use async_graphql::SimpleObject;

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct TokenHolderEvent {
    pub token_id:   String,
    pub event_type: String,
    pub details:    serde_json::Value,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]

pub struct TokenGovernanceEvent {
    pub token_id: String,
    pub action:   String,
    pub details:  serde_json::Value,
}
