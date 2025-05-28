//! Structures and helpers for protocol-level token events (TokenHolder,
//! TokenGovernance). These types are used to prepare and (optionally) persist
//! token events during block processing.

use concordium_rust_sdk::protocol_level_tokens;

/// Collection of prepared token holder events.
/// This struct is required by the event processing interface, even if not
/// always used directly.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenHolderEvents {
    pub events: Vec<PreparedTokenHolderEvent>,
}

/// Represents a single token holder event, prepared for further processing.
/// Fields may not always be accessed, but are kept for completeness and
/// possible future use.
#[derive(Debug)]
#[allow(dead_code)] // Fields are kept for completeness, even if not always used.
pub struct TokenHolderEvent {
    pub token_id:   String,
    pub event_type: String,
    pub details:    serde_json::Value,
}

impl TokenHolderEvent {
    /// Converts a protocol-level token holder event into a prepared event.
    pub fn prepare(event: &protocol_level_tokens::TokenHolderEvent) -> anyhow::Result<Self> {
        Ok(TokenHolderEvent {
            token_id:   event.token_id.clone().into(),
            event_type: event.event_type.clone().into(),
            details:    serde_json::to_value(ciborium::from_reader::<ciborium::Value, _>(
                event.details.as_ref(),
            )?)?,
        })
    }
}

/// Wraps a token holder event for compatibility with the event processing
/// interface.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenHolderEvent {
    pub event: TokenHolderEvent,
}

impl PreparedTokenHolderEvent {
    /// Converts a protocol-level token holder event into a prepared wrapper.
    pub fn prepare(event: &protocol_level_tokens::TokenHolderEvent) -> anyhow::Result<Self> {
        Ok(PreparedTokenHolderEvent {
            event: TokenHolderEvent::prepare(event)?,
        })
    }
}

/// Represents a single token governance event, prepared for further processing.
/// Fields may not always be accessed, but are kept for completeness and
/// possible future use.
#[derive(Debug)]
#[allow(dead_code)] // Fields are kept for completeness, even if not always used.
pub struct TokenGovernanceEvent {
    pub token_id:   String,
    pub event_type: String,
    pub details:    serde_json::Value,
}

impl TokenGovernanceEvent {
    /// Converts a protocol-level token governance event into a prepared event.
    pub fn prepare(event: &protocol_level_tokens::TokenGovernanceEvent) -> anyhow::Result<Self> {
        Ok(TokenGovernanceEvent {
            token_id:   event.token_id.clone().into(),
            event_type: event.event_type.clone().into(),
            details:    serde_json::to_value(ciborium::from_reader::<ciborium::Value, _>(
                event.details.as_ref(),
            )?)?,
        })
    }
}

/// Collection of prepared token governance events.
/// This struct is required by the event processing interface, even if not
/// always used directly.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenGovernanceEvents {
    pub events: Vec<PreparedTokenGovernanceEvent>,
}

/// Wraps a token governance event for compatibility with the event processing
/// interface.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenGovernanceEvent {
    pub event: TokenGovernanceEvent,
}

impl PreparedTokenGovernanceEvent {
    /// Converts a protocol-level token governance event into a prepared
    /// wrapper.
    pub fn prepare(event: &protocol_level_tokens::TokenGovernanceEvent) -> anyhow::Result<Self> {
        Ok(PreparedTokenGovernanceEvent {
            event: TokenGovernanceEvent::prepare(event)?,
        })
    }
}
