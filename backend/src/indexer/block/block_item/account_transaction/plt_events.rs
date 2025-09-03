//! Structures and helpers for protocol-level token events (TokenHolder,
//! TokenGovernance). These types are used to prepare and (optionally) persist
//! token events during block processing.

use crate::transaction_event::protocol_level_tokens::TokenUpdate;
use chrono::{DateTime, Utc};
use concordium_rust_sdk::protocol_level_tokens::{self};

/// Collection of prepared token holder events.
/// This struct is required by the event processing interface, even if not
/// always used directly.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenEvents {
    pub events: Vec<PreparedTokenEvent>,
}

impl PreparedTokenEvents {
    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        slot_time: DateTime<Utc>,
    ) -> anyhow::Result<()> {
        for event in &self.events {
            event.save(tx, transaction_index, slot_time).await?;
        }
        Ok(())
    }
}

/// Wraps a token holder event for compatibility with the event processing
/// interface.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenEvent {
    pub event: TokenUpdate,
}

impl PreparedTokenEvent {
    /// Converts a protocol-level token holder event into a prepared wrapper.
    pub fn prepare(event: &protocol_level_tokens::TokenEvent) -> anyhow::Result<Self> {
        Ok(PreparedTokenEvent {
            event: TokenUpdate::prepare(event)?,
        })
    }

    /// Saves the prepared token event to the database.
    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        slot_time: DateTime<Utc>,
    ) -> anyhow::Result<()> {
        self.event.save(tx, transaction_index, slot_time).await?;
        Ok(())
    }
}
