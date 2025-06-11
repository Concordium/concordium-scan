use async_graphql::{SimpleObject, Union};
use serde::{Deserialize, Serialize};

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TokenHolderEvent {
    pub token_id: String,
    pub event:    TokenEventDetails,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TokenGovernanceEvent {
    pub token_id: String,
    pub event:    TokenEventDetails,
}

#[derive(Union, Serialize, Deserialize)]
pub enum TokenEventDetails {
    Module(TokenModuleEvent),
    Transfer(TokenTransferEvent),
    Mint(MintEvent),
    Burn(BurnEvent),
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TokenModuleEvent {
    pub event_type: String,
    pub details:    serde_json::Value,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone)]
pub struct TokenHolder {
    pub address:   String,         // Base58 or hex string representation
    pub coin_info: Option<String>, // e.g. "CCD"
}

#[derive(SimpleObject, Serialize, Deserialize, Clone)]
pub struct TokenAmount {
    pub value:    String, // Use string to avoid JS number issues
    pub decimals: u8,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone)]
pub struct Memo {
    pub bytes: String, // Hex or base64 string
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct TokenTransferEvent {
    pub from:   TokenHolder,
    pub to:     TokenHolder,
    pub amount: TokenAmount,
    pub memo:   Option<Memo>,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct MintEvent {
    pub target: TokenHolder,
    pub amount: TokenAmount,
}

#[derive(SimpleObject, Serialize, Deserialize)]
pub struct BurnEvent {
    pub target: TokenHolder,
    pub amount: TokenAmount,
}

// --- Conversion implementations from SDK types ---

impl From<concordium_rust_sdk::protocol_level_tokens::TokenHolder> for TokenHolder {
    fn from(holder: concordium_rust_sdk::protocol_level_tokens::TokenHolder) -> Self {
        match holder {
            concordium_rust_sdk::protocol_level_tokens::TokenHolder::HolderAccount(acc) => Self {
                address:   hex::encode(acc.address.0), // or use base58 if preferred
                coin_info: acc.coin_info.map(|c| format!("{:?}", c)),
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
        use concordium_rust_sdk::protocol_level_tokens::TokenEventDetails as SdkEvent;
        match event {
            SdkEvent::Module(e) => TokenEventDetails::Module(TokenModuleEvent {
                event_type: e.event_type.as_ref().to_string(),
                details:    serde_json::to_value(&e.details).unwrap_or_default(),
            }),
            SdkEvent::Transfer(e) => TokenEventDetails::Transfer(TokenTransferEvent {
                from:   e.from.into(),
                to:     e.to.into(),
                amount: e.amount.into(),
                memo:   e.memo.map(Into::into),
            }),
            SdkEvent::Mint(e) => TokenEventDetails::Mint(MintEvent {
                target: e.target.into(),
                amount: e.amount.into(),
            }),
            SdkEvent::Burn(e) => TokenEventDetails::Burn(BurnEvent {
                target: e.target.into(),
                amount: e.amount.into(),
            }),
        }
    }
}
