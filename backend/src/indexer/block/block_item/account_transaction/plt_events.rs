//! Structures and helpers for protocol-level token events (TokenHolder,
//! TokenGovernance). These types are used to prepare and (optionally) persist
//! token events during block processing.

use concordium_rust_sdk::protocol_level_tokens::{self, TokenEventDetails};

/// Collection of prepared token holder events.
/// This struct is required by the event processing interface, even if not
/// always used directly.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenHolderEvents {
    pub events: Vec<PreparedTokenHolderEvent>,
}

#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenGovernanceEvents {
    pub events: Vec<PreparedTokenHolderEvent>,
}

/// Represents a single token holder event, prepared for further processing.
/// Fields may not always be accessed, but are kept for completeness and
/// possible future use.
#[derive(Debug)]
#[allow(dead_code)] // Fields are kept for completeness, even if not always used.
pub struct TokenEvent {
    pub token_id: String,
    pub event:    TokenEventDetails,
}

impl TokenEvent {
    /// Converts a protocol-level token holder event into a prepared event.
    pub fn prepare(event: &protocol_level_tokens::TokenEvent) -> anyhow::Result<Self> {
        Ok(TokenEvent {
            token_id: event.token_id.clone().into(),
            event:    event.event.clone(),
        })
    }
}

/// Wraps a token holder event for compatibility with the event processing
/// interface.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenHolderEvent {
    pub event: TokenEvent,
}

#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenGovernanceEvent {
    pub event: TokenEvent,
}

impl PreparedTokenHolderEvent {
    /// Converts a protocol-level token holder event into a prepared wrapper.
    pub fn prepare(event: &protocol_level_tokens::TokenEvent) -> anyhow::Result<Self> {
        Ok(PreparedTokenHolderEvent {
            event: TokenEvent::prepare(event)?,
        })
    }
}
