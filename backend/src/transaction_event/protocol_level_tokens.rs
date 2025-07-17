use crate::address::AccountAddress;
use async_graphql::{Enum, SimpleObject, Union};
use bigdecimal::BigDecimal;
use concordium_rust_sdk::{
    base::protocol_level_tokens,
    id::types::{AccountAddress as CborAccountAddress, ACCOUNT_ADDRESS_SIZE},
};
use serde::{Deserialize, Deserializer, Serialize};

#[derive(Debug, Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "event_type")]
pub enum TokenUpdateEventType {
    Mint,
    Burn,
    Transfer,
    TokenModule,
}

#[derive(Debug, Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "token_module_type")]
#[allow(clippy::enum_variant_names)] // This is required because the types are used in a GraphQL schema.
pub enum TokenUpdateModuleType {
    AddAllowList,
    RemoveAllowList,
    AddDenyList,
    RemoveDenyList,
    Pause,
    Unpause,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
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
    pub initialization_parameters: CreatePLTInitializationParameters,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct MetadataUrl {
    pub url: String,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct CreatePLTInitializationParameters {
    pub name:               String,
    pub metadata:           MetadataUrl,
    pub allow_list:         Option<bool>,
    pub deny_list:          Option<bool>,
    pub mintable:           Option<bool>,
    pub burnable:           Option<bool>,
    pub initial_supply:     Option<TokenAmount>,
    pub governance_account: Option<TokenHolder>,
}

#[derive(serde::Serialize, serde::Deserialize, Clone, Debug)]
#[serde(rename_all = "camelCase")]
pub struct InitializationParameters {
    /// The name of the token
    pub name:               String,
    /// A URL pointing to the token metadata
    pub metadata:           MetadataUrl,
    /// Whether the token supports a deny list.
    #[serde(rename = "denyList")]
    pub deny_list:          Option<bool>,
    #[serde(rename = "allowList")]
    pub allow_list:         Option<bool>,
    /// Whether the token is burnable.
    pub burnable:           Option<bool>,
    /// Whether the token is mintable.
    pub mintable:           Option<bool>,
    /// Initial supply as decimal fraction
    #[serde(rename = "initialSupply")]
    pub initial_supply:     Option<CborTokenAmount>,
    /// Governance account
    #[serde(rename = "governanceAccount", deserialize_with = "deserialize_governance_account")]
    pub governance_account: Option<concordium_rust_sdk::protocol_level_tokens::TokenHolder>,
}

impl From<InitializationParameters> for CreatePLTInitializationParameters {
    fn from(params: InitializationParameters) -> Self {
        CreatePLTInitializationParameters {
            name:               params.name,
            metadata:           params.metadata,
            allow_list:         params.allow_list,
            deny_list:          params.deny_list,
            mintable:           params.mintable,
            burnable:           params.burnable,
            initial_supply:     params.initial_supply.map(Into::into),
            governance_account: params.governance_account.map(Into::into),
        }
    }
}

fn deserialize_governance_account<'de, D>(
    deserializer: D,
) -> Result<Option<concordium_rust_sdk::protocol_level_tokens::TokenHolder>, D::Error>
where
    D: Deserializer<'de>, {
    // The CBOR structure is: tag(40307) -> map(1) -> { 3: bytes(32) }
    use serde::de::Error;
    use std::collections::HashMap;
    let map: HashMap<u8, Vec<u8>> = Deserialize::deserialize(deserializer)?;
    if let Some(address_bytes) = map.get(&3) {
        if address_bytes.len() == ACCOUNT_ADDRESS_SIZE {
            let mut bytes_array = [0u8; ACCOUNT_ADDRESS_SIZE];
            bytes_array.copy_from_slice(address_bytes);
            Ok(Some(concordium_rust_sdk::protocol_level_tokens::TokenHolder::Account {
                address: CborAccountAddress(bytes_array),
            }))
        } else {
            Err(D::Error::custom(format!(
                "Invalid address length: expected {}, got {}",
                ACCOUNT_ADDRESS_SIZE,
                address_bytes.len()
            )))
        }
    } else {
        Err(D::Error::custom("Expected key 3 in governance account map"))
    }
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct CborTokenAmount(pub i8, pub u64);

impl From<CborTokenAmount> for TokenAmount {
    fn from(cbor: CborTokenAmount) -> Self {
        let decimals = cbor.0.unsigned_abs();
        let value = cbor.1;
        TokenAmount {
            value: value.to_string(),
            decimals,
        }
    }
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

#[derive(Debug, Clone)]
pub struct PreparedTokenUpdate {
    pub token_id:          String,
    pub event:             TokenEventDetails,
    pub event_type:        TokenUpdateEventType,
    pub token_module_type: Option<TokenUpdateModuleType>,
    pub plt_amount_change: BigDecimal,
    pub target:            Option<String>,
    pub to:                Option<String>,
    pub from:              Option<String>,
}

#[derive(Union, Serialize, Deserialize, Clone, Debug)]
#[serde(tag = "type")]
pub enum TokenEventDetails {
    Module(TokenModuleEvent),
    Transfer(TokenTransferEvent),
    Mint(MintEvent),
    Burn(BurnEvent),
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenModuleEvent {
    pub event_type: String,
    pub details:    serde_json::Value,
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
            value:    amount.value().to_string(),
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
    pub fn prepare(
        event: &concordium_rust_sdk::protocol_level_tokens::TokenEvent,
    ) -> anyhow::Result<Self> {
        Ok(TokenUpdate {
            token_id: event.token_id.clone().into(),
            event:    event.event.clone().into(),
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        let prepared: PreparedTokenUpdate = self.clone().into();
        prepared.save(tx, transaction_index).await
    }
}

impl From<TokenUpdate> for PreparedTokenUpdate {
    fn from(update: TokenUpdate) -> Self {
        let mut token_module_type = None;
        let mut target = None;
        let mut from = None;
        let mut to = None;
        let mut plt_amount_change = BigDecimal::from(0u64);
        let event: TokenEventDetails = update.event.clone();

        let event_type = match &update.event {
            TokenEventDetails::Module(e) => {
                token_module_type = match e.event_type.as_str() {
                    "addAllowList" => Some(TokenUpdateModuleType::AddAllowList),
                    "removeAllowList" => Some(TokenUpdateModuleType::RemoveAllowList),
                    "addDenyList" => Some(TokenUpdateModuleType::AddDenyList),
                    "removeDenyList" => Some(TokenUpdateModuleType::RemoveDenyList),
                    "pause" => Some(TokenUpdateModuleType::Pause),
                    "unpause" => Some(TokenUpdateModuleType::Unpause),
                    _ => None,
                };
                TokenUpdateEventType::TokenModule
            }
            TokenEventDetails::Transfer(e) => {
                from = Some(e.from.address.to_string());
                to = Some(e.to.address.to_string());
                plt_amount_change =
                    BigDecimal::from(e.amount.value.parse::<f64>().unwrap_or(0.0) as u64);
                TokenUpdateEventType::Transfer
            }
            TokenEventDetails::Mint(e) => {
                target = Some(e.target.address.to_string());
                plt_amount_change =
                    BigDecimal::from(e.amount.value.parse::<f64>().unwrap_or(0.0) as u64);
                TokenUpdateEventType::Mint
            }
            TokenEventDetails::Burn(e) => {
                target = Some(e.target.address.to_string());
                plt_amount_change =
                    BigDecimal::from(e.amount.value.parse::<f64>().unwrap_or(0.0) as u64);
                TokenUpdateEventType::Burn
            }
        };

        Self {
            token_id: update.token_id,
            event_type,
            token_module_type,
            target,
            from,
            to,
            plt_amount_change,
            event,
        }
    }
}

impl PreparedTokenUpdate {
    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        let token_event: serde_json::Value =
            serde_json::to_value(&self.event).unwrap_or(serde_json::Value::Null);

        sqlx::query!(
            "
            INSERT INTO plt_events (
                id,
                transaction_index,
                event_type,
                token_module_type,
                token_index,
                token_event
            )
            VALUES (
                (SELECT COALESCE(MAX(id) + 1, 0) FROM plt_events),
                $1,
                 $2,
                 $3,
                (SELECT index FROM plt_tokens WHERE token_id = $4),
                $5
                )
            ",
            transaction_index,
            self.event_type as TokenUpdateEventType,
            self.token_module_type as Option<TokenUpdateModuleType>,
            self.token_id,
            token_event
        )
        .execute(tx.as_mut())
        .await?;

        match self.event_type {
            TokenUpdateEventType::Mint => {
                if let Some(ref target) = self.target {
                    self.update_total_minted(tx).await?;
                    self.update_account_balance(tx, target, &self.plt_amount_change).await?;
                }
            }
            TokenUpdateEventType::Burn => {
                if let Some(ref target) = self.target {
                    self.update_total_burned(tx).await?;
                    self.update_account_balance(tx, target, &(-&self.plt_amount_change)).await?;
                }
            }
            TokenUpdateEventType::Transfer => {
                if let (Some(ref from), Some(ref to)) = (&self.from, &self.to) {
                    self.update_account_balance(tx, from, &(-&self.plt_amount_change)).await?;
                    self.update_account_balance(tx, to, &self.plt_amount_change).await?;
                }
            }
            TokenUpdateEventType::TokenModule => {}
        }

        Ok(())
    }

    async fn update_total_minted(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE plt_tokens SET total_minted = total_minted + $1 WHERE token_id = $2",
            self.plt_amount_change,
            self.token_id
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }

    async fn update_total_burned(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE plt_tokens SET total_burned = total_burned + $1 WHERE token_id = $2",
            self.plt_amount_change,
            self.token_id
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }

    async fn update_account_balance(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        account: &str,
        amount: &BigDecimal,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "
            INSERT INTO plt_accounts (account_index, token_index, amount, decimal)
            VALUES (
                (SELECT index FROM accounts WHERE address = $1),
                (SELECT index FROM plt_tokens WHERE token_id = $2),
                $3,
                (SELECT decimal FROM plt_tokens WHERE token_id = $2)
            )
            ON CONFLICT (account_index, token_index) DO UPDATE
            SET amount = plt_accounts.amount + $3
            ",
            account,
            self.token_id,
            amount
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}
