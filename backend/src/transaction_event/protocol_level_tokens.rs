use crate::address::AccountAddress;
use async_graphql::{Enum, SimpleObject, Union};
use bigdecimal::BigDecimal;

use concordium_rust_sdk::protocol_level_tokens::{self};
use serde::{Deserialize, Serialize};
const CONCORDIUM_SLIP_0044_CODE: u64 = 919;

#[derive(Union, Serialize, Deserialize, Clone, Debug)]
#[serde(tag = "type")]
pub enum TokenModuleRejectReasonType {
    /// Address not found
    AddressNotFound(AddressNotFoundRejectReason),
    /// Token balance is insufficient
    TokenBalanceInsufficient(TokenBalanceInsufficientRejectReason),
    /// The transaction could not be deserialized
    DeserializationFailure(DeserializationFailureRejectReason),
    /// The operation is not supported by the token module
    UnsupportedOperation(UnsupportedOperationRejectReason),
    /// Operation authorization check failed
    OperationNotPermitted(OperationNotPermittedRejectReason),
    /// Minting the requested amount would overflow the representable token
    /// amount.
    MintWouldOverflow(MintWouldOverflowRejectReason),
    /// Unknown reject reason
    Unknown(UnknownRejectReason),
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct UnknownRejectReason {
    pub message: String,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct AddressNotFoundRejectReason {
    pub index:   String,
    pub address: CborTokenHolder,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct TokenBalanceInsufficientRejectReason {
    pub index:             String,
    pub available_balance: TokenAmount,
    pub required_balance:  TokenAmount,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct DeserializationFailureRejectReason {
    pub cause: Option<String>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct UnsupportedOperationRejectReason {
    pub index:          String,
    pub operation_type: String,
    pub reason:         Option<String>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct OperationNotPermittedRejectReason {
    pub index:   String,
    pub address: Option<CborTokenHolder>,
    pub reason:  Option<String>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct MintWouldOverflowRejectReason {
    /// The index in the list of operations of the failing operation.
    pub index:                    String,
    /// The requested amount to mint.
    pub requested_amount:         TokenAmount,
    /// The current supply of the token.
    pub current_supply:           TokenAmount,
    /// The maximum representable token amount.
    pub max_representable_amount: TokenAmount,
}

impl From<concordium_rust_sdk::protocol_level_tokens::TokenModuleRejectReasonType> for TokenModuleRejectReasonType {
    fn from(reject_reason: concordium_rust_sdk::protocol_level_tokens::TokenModuleRejectReasonType) -> Self {
        use concordium_rust_sdk::protocol_level_tokens::TokenModuleRejectReasonType as Reason;
        match reject_reason {
            Reason::AddressNotFound(reason) => {
                TokenModuleRejectReasonType::AddressNotFound(reason.into())
            }
            Reason::TokenBalanceInsufficient(reason) => {
                TokenModuleRejectReasonType::TokenBalanceInsufficient(reason.into())
            }
            Reason::DeserializationFailure(reason) => {
                TokenModuleRejectReasonType::DeserializationFailure(reason.into())
            }
            Reason::UnsupportedOperation(reason) => {
                TokenModuleRejectReasonType::UnsupportedOperation(reason.into())
            }
            Reason::OperationNotPermitted(reason) => {
                TokenModuleRejectReasonType::OperationNotPermitted(reason.into())
            }
            Reason::MintWouldOverflow(reason) => {
                TokenModuleRejectReasonType::MintWouldOverflow(reason.into())
            }
            Reason::Unknow => TokenModuleRejectReasonType::Unknown(UnknownRejectReason {
                message: "Unknown reject reason".into(),
            }),
        }
    }
}
// Implement From conversions for each reject reason type
impl From<concordium_rust_sdk::protocol_level_tokens::AddressNotFoundRejectReason>
    for AddressNotFoundRejectReason
{
    fn from(
        reason: concordium_rust_sdk::protocol_level_tokens::AddressNotFoundRejectReason,
    ) -> Self {
        AddressNotFoundRejectReason {
            index:   reason.index.to_string(),
            address: reason.address.into(),
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::TokenBalanceInsufficientRejectReason>
    for TokenBalanceInsufficientRejectReason
{
    fn from(
        reason: concordium_rust_sdk::protocol_level_tokens::TokenBalanceInsufficientRejectReason,
    ) -> Self {
        TokenBalanceInsufficientRejectReason {
            index:             reason.index.to_string(),
            available_balance: reason.available_balance.into(),
            required_balance:  reason.required_balance.into(),
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::DeserializationFailureRejectReason>
    for DeserializationFailureRejectReason
{
    fn from(
        reason: concordium_rust_sdk::protocol_level_tokens::DeserializationFailureRejectReason,
    ) -> Self {
        DeserializationFailureRejectReason {
            cause: reason.cause,
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::UnsupportedOperationRejectReason>
    for UnsupportedOperationRejectReason
{
    fn from(
        reason: concordium_rust_sdk::protocol_level_tokens::UnsupportedOperationRejectReason,
    ) -> Self {
        UnsupportedOperationRejectReason {
            index:          reason.index.to_string(),
            operation_type: reason.operation_type,
            reason:         reason.reason,
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::OperationNotPermittedRejectReason>
    for OperationNotPermittedRejectReason
{
    fn from(
        reason: concordium_rust_sdk::protocol_level_tokens::OperationNotPermittedRejectReason,
    ) -> Self {
        OperationNotPermittedRejectReason {
            index:   reason.index.to_string(),
            address: reason.address.map(Into::into),
            reason:  reason.reason,
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::MintWouldOverflowRejectReason>
    for MintWouldOverflowRejectReason
{
    fn from(
        reason: concordium_rust_sdk::protocol_level_tokens::MintWouldOverflowRejectReason,
    ) -> Self {
        MintWouldOverflowRejectReason {
            index:                    reason.index.to_string(),
            requested_amount:         reason.requested_amount.into(),
            current_supply:           reason.current_supply.into(),
            max_representable_amount: reason.max_representable_amount.into(),
        }
    }
}

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
    pub initialization_parameters: InitializationParameters,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct MetadataUrl {
    pub url:              String,
    pub checksum_sha_256: Option<String>,
    pub additional:       Option<serde_json::Value>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct InitializationParameters {
    pub name:               String,
    pub metadata:           MetadataUrl,
    pub allow_list:         Option<bool>,
    pub deny_list:          Option<bool>,
    pub mintable:           Option<bool>,
    pub burnable:           Option<bool>,
    pub initial_supply:     Option<TokenAmount>,
    // Todo: Refactior convert this to CborTokenHolder (to ensure backwards compatibility will
    // update the type when we reset devnet db)
    pub governance_account: CborHolderAccount,
}

#[derive(SimpleObject, Debug, Serialize, Deserialize, Clone)]
pub struct CborTokenHolder {
    pub account: CborHolderAccount,
}

impl From<concordium_rust_sdk::protocol_level_tokens::CborTokenHolder> for CborTokenHolder {
    fn from(holder: concordium_rust_sdk::protocol_level_tokens::CborTokenHolder) -> Self {
        match holder {
            concordium_rust_sdk::protocol_level_tokens::CborTokenHolder::Account(account) => {
                CborTokenHolder {
                    account: CborHolderAccount {
                        address:   account.address.into(),
                        coin_info: account.coin_info.map(|info| CoinInfo {
                            coin_info_code: match info {
                                concordium_rust_sdk::protocol_level_tokens::CoinInfo::CCD => {
                                    CONCORDIUM_SLIP_0044_CODE.to_string()
                                }
                            },
                        }),
                    },
                }
            }
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::CborTokenHolder> for CborHolderAccount {
    fn from(holder: concordium_rust_sdk::protocol_level_tokens::CborTokenHolder) -> Self {
        match holder {
            concordium_rust_sdk::protocol_level_tokens::CborTokenHolder::Account(account) => {
                CborHolderAccount {
                    address:   account.address.into(),
                    coin_info: account.coin_info.map(|info| CoinInfo {
                        coin_info_code: match info {
                            concordium_rust_sdk::protocol_level_tokens::CoinInfo::CCD => {
                                CONCORDIUM_SLIP_0044_CODE.to_string()
                            }
                        },
                    }),
                }
            }
        }
    }
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct CborHolderAccount {
    pub address:   AccountAddress,
    pub coin_info: Option<CoinInfo>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct CoinInfo {
    pub coin_info_code: String,
}

impl From<concordium_rust_sdk::protocol_level_tokens::TokenModuleInitializationParameters>
    for InitializationParameters
{
    fn from(
        params: concordium_rust_sdk::protocol_level_tokens::TokenModuleInitializationParameters,
    ) -> Self {
        InitializationParameters {
            name:               params.name,
            metadata:           MetadataUrl {
                url:              params.metadata.url,
                checksum_sha_256: params.metadata.checksum_sha_256.map(|h| hex::encode(h.as_ref())),
                additional:       if params.metadata.additional.is_empty() {
                    None
                } else {
                    Some(serde_json::Value::Object(
                        params
                            .metadata
                            .additional
                            .into_iter()
                            .map(|(key, value)| {
                                (key, serde_json::Value::String(format!("{:?}", value)))
                            })
                            .collect(),
                    ))
                },
            },
            allow_list:         params.allow_list,
            deny_list:          params.deny_list,
            mintable:           params.mintable,
            burnable:           params.burnable,
            initial_supply:     params.initial_supply.map(Into::into),
            governance_account: params.governance_account.into(),
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
    pub decimals: String,
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
            decimals: amount.decimals().to_string(),
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
