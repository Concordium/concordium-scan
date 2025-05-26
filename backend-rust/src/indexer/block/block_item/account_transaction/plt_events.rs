use concordium_rust_sdk::{protocol_level_tokens, types::ProtocolVersion};

#[derive(Debug)]
pub struct PreparedTokenHolderEvents {
    pub events: Vec<PreparedTokenHolderEvent>,
}

impl PreparedTokenHolderEvents {
    pub async fn save(
        &self,
        _tx: &mut sqlx::PgTransaction<'_>,
        _transaction_index: i64,
        _protocol_version: ProtocolVersion,
    ) -> anyhow::Result<()> {
        Ok(())
    }
}

#[derive(Debug)]
struct TokenHolderEvent {
    // Define the fields of the TokenHolderEvent
    token_id:   String,
    event_type: String,
    details:    serde_json::Value,
}

impl TokenHolderEvent {
    pub fn prepare(event: &protocol_level_tokens::TokenHolderEvent) -> anyhow::Result<Self> {
        // Prepare the event for insertion into the database
        let prepared = TokenHolderEvent {
            token_id:   event.token_id.clone().into(),
            event_type: event.event_type.clone().into(),
            details:    serde_cbor::from_slice(event.details.as_ref())?,
        };
        Ok(prepared)
    }
}

#[derive(Debug)]
pub struct PreparedTokenHolderEvent {
    pub event: Option<TokenHolderEvent>,
}

impl PreparedTokenHolderEvent {
    pub fn prepare(event: &protocol_level_tokens::TokenHolderEvent) -> anyhow::Result<Self> {
        // Prepare the event for insertion into the database

        let prepared = PreparedTokenHolderEvent {
            event: Some(TokenHolderEvent::prepare(event)?),
        };
        Ok(prepared)
    }
}

// TokenGovernanceEvent
#[derive(Debug)]
pub struct TokenGovernanceEvent {
    // Define the fields of the TokenGovernanceEvent
    token_id:   String,
    event_type: String,
    details:    serde_json::Value,
}

impl TokenGovernanceEvent {
    fn prepare(event: &protocol_level_tokens::TokenGovernanceEvent) -> anyhow::Result<Self> {
        // Prepare the event for insertion into the database
        let prepared = TokenGovernanceEvent {
            token_id:   event.token_id.clone().into(),
            event_type: event.event_type.clone().into(),
            details:    serde_cbor::from_slice(event.details.as_ref())?,
        };
        Ok(prepared)
    }
}

#[derive(Debug)]
pub struct PreparedTokenGovernanceEvents {
    pub events: Vec<PreparedTokenGovernanceEvent>,
}

impl PreparedTokenGovernanceEvents {
    pub async fn save(
        &self,
        _tx: &mut sqlx::PgTransaction<'_>,
        _transaction_index: i64,
        _protocol_version: ProtocolVersion,
    ) -> anyhow::Result<()> {
        Ok(())
    }
}

#[derive(Debug)]
pub struct PreparedTokenGovernanceEvent {
    pub event: Option<TokenGovernanceEvent>,
}

impl PreparedTokenGovernanceEvent {
    pub fn prepare(event: &protocol_level_tokens::TokenGovernanceEvent) -> anyhow::Result<Self> {
        // Prepare the event for insertion into the database

        let prepared = PreparedTokenGovernanceEvent {
            event: Some(TokenGovernanceEvent::prepare(event)?),
        };
        Ok(prepared)
    }
}
