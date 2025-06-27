//! Structures and helpers for protocol-level token events (TokenHolder,
//! TokenGovernance). These types are used to prepare and (optionally) persist
//! token events during block processing.

use concordium_rust_sdk::protocol_level_tokens;

use crate::{
    indexer::block::block_item::PreparedBlockItem,
    transaction_event::protocol_level_tokens::{TokenEventDetails, TokenListUpdateEventDetailsAddAllowList, TokenListUpdateEventDetailsAddDenyList, TokenListUpdateEventDetailsRemoveAllowList, TokenListUpdateEventDetailsRemoveDenyList, TokenModuleEventType, UnknownTokenListUpdateEventDetails},
};

/// Collection of prepared token holder events.
/// This struct is required by the event processing interface, even if not
/// always used directly.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenEvents {
    pub events: Vec<PreparedTokenEvent>,
}

impl PreparedTokenEvents {
    pub async  fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        // Placeholder for saving logic, if needed in the future.
        for event in &self.events {
            event.save(tx, transaction_index).await;
        }
        Ok(())
    }
}

/// Represents a single token event, prepared for further processing.
/// Fields may not always be accessed, but are kept for completeness and
/// possible future use.
#[derive(Debug)]
#[allow(dead_code)] // Fields are kept for completeness, even if not always used.
pub struct TokenEvent {
    pub token_id: String,
    pub event: TokenEventDetails,
}

impl TokenEvent {
    /// Converts a protocol-level token holder event into a prepared event.
    pub fn prepare(event: &protocol_level_tokens::TokenEvent) -> anyhow::Result<Self> {
        Ok(TokenEvent {
            token_id: event.token_id.clone().into(),
            event: event.event.clone().into(),
        })
    }

 pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        println!("========================");
        println!("Self: {:?}", self);
        println!("========================");
        println!("tx: {:?}", tx);
        println!("transaction_index: {:?}", transaction_index);
        let token_id = self.token_id.clone();
        match &self.event {
            TokenEventDetails::Module(details) => {
                let token_id = self.token_id.clone();
                let event_type = details.event_type.clone();
                // The match arms below return different types, which is not allowed.
                // If you need to handle each variant, do so without assigning to a single variable of a concrete type.
                let event_details: TokenModuleEventType = match &details.details {
                    TokenModuleEventType::AddAllowList(details) => {
                        TokenModuleEventType::AddAllowList(
                            TokenListUpdateEventDetailsAddAllowList {
                                target: details.target.clone(),
                            },
                        )
                    }
                    TokenModuleEventType::RemoveAllowList(details) => {
                        TokenModuleEventType::RemoveAllowList(
                            TokenListUpdateEventDetailsRemoveAllowList {
                                target: details.target.clone(),
                            },
                        )
                    }
                    TokenModuleEventType::AddDenyList(details) => {
                        TokenModuleEventType::AddDenyList(TokenListUpdateEventDetailsAddDenyList {
                            target: details.target.clone(),
                        })
                    }
                    TokenModuleEventType::RemoveDenyList(details) => {
                        TokenModuleEventType::RemoveDenyList(
                            TokenListUpdateEventDetailsRemoveDenyList {
                                target: details.target.clone(),
                            },
                        )
                    }
                    TokenModuleEventType::Unknow(details) => {
                        TokenModuleEventType::Unknow(UnknownTokenListUpdateEventDetails {
                            message: details.message.clone(),
                        })
                    }
                };

                sqlx::query!(
                    "INSERT INTO plt_events (id, transaction_index, event_type, token_module_type, token_id, token_event) VALUES (
                        (SELECT COALESCE(MAX(id) + 1, 0) FROM plt_events),
                        $1,
                        'TokenModule',
                        'AddAllowList',
                        $2,
                        $3
                    )",
                    transaction_index.clone(),
                    token_id,
                    serde_json::to_value(details).unwrap(),
                ).execute(tx.as_mut())
                .await?
                .rows_affected();
            }
            TokenEventDetails::Transfer(token_transfer_event) => {
                let from_address = token_transfer_event.from.address.clone();
                let to_address = token_transfer_event.to.address.clone();
                let amount_value = token_transfer_event.amount.value.clone();
                let amount_decimals = token_transfer_event.amount.decimals;
                let memo_bytes = token_transfer_event.memo.as_ref().map(|m| m.bytes.clone());

                sqlx::query!(
                    "INSERT INTO plt_events (id, transaction_index, event_type, token_id, token_event) VALUES (
                        (SELECT COALESCE(MAX(id) + 1, 0) FROM plt_events),
                        $1,
                        'Transfer',
                        $2,
                        $3
                    )",
                    transaction_index.clone(),
                    token_id,
                    serde_json::to_value(token_transfer_event).unwrap(),
                ).execute(tx.as_mut())
                .await?
                .rows_affected();
            }
            TokenEventDetails::Mint(mint_event) => {
                let target_address = mint_event.target.address.clone();
                let amount_value = mint_event.amount.value.clone();
                let amount_decimals = mint_event.amount.decimals;
                sqlx::query!(
                    "INSERT INTO plt_events (id, transaction_index, event_type, token_id, token_event) VALUES (
                        (SELECT COALESCE(MAX(id) + 1, 0) FROM plt_events),
                        $1,
                        'Mint',
                        $2,
                        $3
                    )",
                    transaction_index.clone(),
                    token_id,
                    serde_json::to_value(mint_event).unwrap(),
                ).execute(tx.as_mut())
                .await?
                .rows_affected();
            }
            TokenEventDetails::Burn(burn_event) => {
                let target_address = burn_event.target.address.clone();
                let amount_value = burn_event.amount.value.clone();
                let amount_decimals = burn_event.amount.decimals;
                sqlx::query!(
                    "INSERT INTO plt_events (id, transaction_index, event_type, token_id, token_event) VALUES (
                        (SELECT COALESCE(MAX(id) + 1, 0) FROM plt_events),
                        $1,
                        'Burn',
                        $2,
                        $3
                    )",
                    transaction_index.clone(),
                    token_id,
                    serde_json::to_value(burn_event).unwrap(),
                ).execute(tx.as_mut())
                .await?
                .rows_affected();
            }
        };

        Ok(())
    }
}

/// Wraps a token holder event for compatibility with the event processing
/// interface.
#[derive(Debug)]
#[allow(dead_code)] // This type is needed for interface compatibility, even if not always used.
pub struct PreparedTokenEvent {
    pub event: TokenEvent,
}

impl PreparedTokenEvent {
    /// Converts a protocol-level token holder event into a prepared wrapper.
    pub fn prepare(event: &protocol_level_tokens::TokenEvent) -> anyhow::Result<Self> {
        Ok(PreparedTokenEvent {
            event: TokenEvent::prepare(event)?,
        })
    }

    pub async  fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.event.save(tx, transaction_index).await
    }
}
