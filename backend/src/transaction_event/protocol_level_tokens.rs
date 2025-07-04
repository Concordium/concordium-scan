use async_graphql::{SimpleObject, Union};
use concordium_rust_sdk::base::protocol_level_tokens;
use serde::{Deserialize, Serialize};

use crate::address::AccountAddress;

#[derive(SimpleObject, Serialize, Deserialize, Clone)]
pub struct CreatePlt {
    /// The symbol of the token.
    pub token_id:                  String,
    /// A SHA256 hash that identifies the token module implementation.
    pub token_module:              String,
    /// The number of decimal places used in the representation of amounts of
    /// this token. This determines the smallest representable fraction of the
    /// token.
    pub decimals:                  u8,
    /// The initialization parameters of the token, encoded in CBOR.
    pub initialization_parameters: String,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone)]
pub struct TokenCreationDetails {
    // The update payload used to create the token.
    pub create_plt: CreatePlt,
    // The events generated by the token module during the creation of the token.
    pub events:     Vec<TokenUpdate>,
}

/// Common event struct for both Holder and Governance events.
#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenUpdate {
    pub token_id: String,
    pub event:    TokenEventDetails,
}

#[derive(Union, Serialize, Deserialize, Clone, Debug)]
pub enum TokenEventDetails {
    Module(TokenModuleEvent),
    Transfer(TokenTransferEvent),
    Mint(MintEvent),
    Burn(BurnEvent),
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenModuleEvent {
    pub event_type: String,
    pub details:    serde_json::Value, // Use serde_json::Value for flexible details
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenHolder {
    pub address: AccountAddress,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenAmount {
    pub value:    String,
    pub decimals: u8,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct Memo {
    pub bytes: String,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenTransferEvent {
    pub from:   TokenHolder,
    pub to:     TokenHolder,
    pub amount: TokenAmount,
    pub memo:   Option<Memo>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct MintEvent {
    pub target: TokenHolder,
    pub amount: TokenAmount,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct BurnEvent {
    pub target: TokenHolder,
    pub amount: TokenAmount,
}

// --- Conversion implementations from SDK types ---

impl From<concordium_rust_sdk::protocol_level_tokens::TokenHolder> for TokenHolder {
    fn from(holder: concordium_rust_sdk::protocol_level_tokens::TokenHolder) -> Self {
        match holder {
            concordium_rust_sdk::protocol_level_tokens::TokenHolder::Account {
                address,
            } => Self {
                address: address.into(),
            },
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::TokenAmount> for TokenAmount {
    fn from(amount: concordium_rust_sdk::protocol_level_tokens::TokenAmount) -> Self {
        Self {
            value:    amount.to_string(),
            decimals: amount.decimals(),
        }
    }
}

impl From<concordium_rust_sdk::types::Memo> for Memo {
    fn from(memo: concordium_rust_sdk::types::Memo) -> Self {
        Self {
            bytes: hex::encode(memo.as_ref()),
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::TokenEventDetails> for TokenEventDetails {
    fn from(event: concordium_rust_sdk::protocol_level_tokens::TokenEventDetails) -> Self {
        use concordium_rust_sdk::protocol_level_tokens::TokenEventDetails as TokenEventDetailsType;
        match event {
            TokenEventDetailsType::Module(e) => TokenEventDetails::Module(TokenModuleEvent {
                event_type: e.event_type.as_ref().to_string(),
                details:    {
                    match protocol_level_tokens::TokenModuleEvent::decode_token_module_event(&e) {
                        Ok(details) => {
                            serde_json::to_value(details).unwrap_or(serde_json::Value::Null)
                        }
                        Err(err) => {
                            // Represent the error as a JSON value
                            serde_json::json!({
                                "error": format!("Error decoding event details: {}", err)
                            })
                        }
                    }
                },
            }),
            TokenEventDetailsType::Transfer(e) => TokenEventDetails::Transfer(TokenTransferEvent {
                from:   e.from.into(),
                to:     e.to.into(),
                amount: e.amount.into(),
                memo:   e.memo.map(Into::into),
            }),
            TokenEventDetailsType::Mint(e) => TokenEventDetails::Mint(MintEvent {
                target: e.target.into(),
                amount: e.amount.into(),
            }),
            TokenEventDetailsType::Burn(e) => TokenEventDetails::Burn(BurnEvent {
                target: e.target.into(),
                amount: e.amount.into(),
            }),
        }
    }
}

impl TokenUpdate {
    /// Converts a protocol-level token event into a common event structure.
    pub fn prepare(
        event: &concordium_rust_sdk::protocol_level_tokens::TokenEvent,
    ) -> anyhow::Result<Self> {
        Ok(TokenUpdate {
            token_id: event.token_id.clone().into(),
            event:    event.event.clone().into(),
        })
    }
}
