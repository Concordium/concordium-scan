use crate::address::AccountAddress;
use async_graphql::{Enum, SimpleObject, Union};
use bigdecimal::BigDecimal;
use std::str::FromStr;

use concordium_rust_sdk::protocol_level_tokens;
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

impl From<concordium_rust_sdk::protocol_level_tokens::TokenModuleRejectReasonType>
    for TokenModuleRejectReasonType
{
    fn from(
        reject_reason: concordium_rust_sdk::protocol_level_tokens::TokenModuleRejectReasonType,
    ) -> Self {
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

/// PreparedTokenUpdate:
///   - Normalize and extract all relevant fields from a TokenUpdate for
///     database storage and metrics.
///   - Handle Mint, Burn, Transfer, and TokenModule events.
///   - Track event type, module type, amount changes, and involved accounts
///     (from, to, target).
///   - Updates db for:
///       * Account balances
///       * Token supply (minted/burned)
///       * Metrics tables (cumulative event count, transfer amount, unique
///         accounts)

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
        slot_time: chrono::DateTime<chrono::Utc>,
    ) -> anyhow::Result<()> {
        let prepared: PreparedTokenUpdate = self.clone().try_into()?;
        prepared.save(tx, transaction_index, slot_time).await
    }
}

impl TryFrom<TokenUpdate> for PreparedTokenUpdate {
    type Error = anyhow::Error;

    fn try_from(update: TokenUpdate) -> anyhow::Result<Self> {
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
                    BigDecimal::from(e.amount.value.parse::<u64>().map_err(|e| {
                        anyhow::anyhow!("Failed to parse transfer amount value: {}", e)
                    })?);

                TokenUpdateEventType::Transfer
            }
            TokenEventDetails::Mint(e) => {
                target = Some(e.target.address.to_string());
                plt_amount_change =
                    BigDecimal::from(e.amount.value.parse::<u64>().map_err(|e| {
                        anyhow::anyhow!("Failed to parse mint amount value: {}", e)
                    })?);

                TokenUpdateEventType::Mint
            }
            TokenEventDetails::Burn(e) => {
                target = Some(e.target.address.to_string());
                plt_amount_change =
                    BigDecimal::from(e.amount.value.parse::<u64>().map_err(|e| {
                        anyhow::anyhow!("Failed to parse burn amount value: {}", e)
                    })?);

                TokenUpdateEventType::Burn
            }
        };

        Ok(Self {
            token_id: update.token_id,
            event_type,
            token_module_type,
            target,
            from,
            to,
            plt_amount_change,
            event,
        })
    }
}

impl PreparedTokenUpdate {
    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        slot_time: chrono::DateTime<chrono::Utc>,
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

        // Update cumulative event count metrics (metrics_plt table)
        self.update_metrics_plt_cumulative_event_count(tx, slot_time).await?;

        match self.event_type {
            TokenUpdateEventType::Mint => {
                if let Some(ref target) = self.target {
                    let previous_amount =
                        self.plt_amount_accross_tokens_by_account(tx, target).await?;
                    self.update_total_minted(tx).await?;
                    self.update_account_balance(tx, target, &self.plt_amount_change).await?;
                    let current_amount =
                        self.plt_amount_accross_tokens_by_account(tx, target).await?;
                    if previous_amount == BigDecimal::from(0)
                        && current_amount > BigDecimal::from(0)
                    {
                        self.update_metrics_plt_unique_account_count_delta(tx, slot_time, 1)
                            .await?;
                    }
                }
            }
            TokenUpdateEventType::Burn => {
                if let Some(ref target) = self.target {
                    let previous_amount =
                        self.plt_amount_accross_tokens_by_account(tx, target).await?;

                    self.update_total_burned(tx).await?;
                    self.update_account_balance(tx, target, &(-&self.plt_amount_change)).await?;
                    let current_amount =
                        self.plt_amount_accross_tokens_by_account(tx, target).await?;
                    if previous_amount > BigDecimal::from(0)
                        && current_amount == BigDecimal::from(0)
                    {
                        self.update_metrics_plt_unique_account_count_delta(tx, slot_time, -1)
                            .await?;
                    }
                }
            }
            TokenUpdateEventType::Transfer => {
                if let (Some(ref from), Some(ref to)) = (&self.from, &self.to) {
                    let previous_from_amount =
                        self.plt_amount_accross_tokens_by_account(tx, from).await?;
                    let previous_to_amount =
                        self.plt_amount_accross_tokens_by_account(tx, to).await?;
                    self.update_transfer_balances(tx, from, to, &self.plt_amount_change).await?;

                    // This SQL block updates the global PLT metrics table (metrics_plt) for a
                    // transfer event:
                    // It fetches the latest cumulative transfer amount and unique account count.
                    // Inserts a new row for the current event timestamp, incrementing the transfer
                    // amount by the transferred value. If a row for the
                    // timestamp already exists, it updates the cumulative transfer amount using the
                    // maximum value (to ensure monotonic increase). The unique
                    // account count is preserved and not changed by this query.
                    sqlx::query!(
                        "
                        WITH latest_metrics AS (
                            SELECT 
                                COALESCE(cumulative_transfer_amount, 0) AS prev_transfer_amount,
                                COALESCE(unique_account_count, 0) AS prev_unique_count
                            FROM metrics_plt 
                            ORDER BY event_timestamp DESC 
                            LIMIT 1
                        )
                        INSERT INTO metrics_plt (
                            event_timestamp, 
                            cumulative_transfer_amount,
                            unique_account_count
                        )
                        SELECT 
                            $1,
                            lm.prev_transfer_amount + $2,
                            lm.prev_unique_count
                        FROM latest_metrics lm
                        ON CONFLICT (event_timestamp) DO UPDATE SET
                            cumulative_transfer_amount = \
                         GREATEST(metrics_plt.cumulative_transfer_amount, \
                         EXCLUDED.cumulative_transfer_amount),
                            unique_account_count = GREATEST(metrics_plt.unique_account_count, \
                         EXCLUDED.unique_account_count)
                        ",
                        slot_time,
                        self.plt_amount_change
                    )
                    .execute(tx.as_mut())
                    .await?;
                    // This SQL block updates the per-token transfer metrics table
                    // (metrics_plt_transfer) for a transfer event:
                    // It inserts a new row for the current event timestamp and token, incrementing
                    // the transfer count and amount by 1 and the transferred value, respectively.
                    // If a row for the same timestamp and token already exists, it updates the
                    // cumulative transfer count and amount using the maximum value (ensuring
                    // monotonic increase). This query tracks how many transfers
                    // and how much value has been transferred for each token, per block.
                    sqlx::query!(
                        "
                    INSERT INTO metrics_plt_transfer (
                        event_timestamp, 
                        token_index,
                        cumulative_transfer_count,
                        cumulative_transfer_amount
                    )
                    SELECT 
                        $1,
                        (SELECT index FROM plt_tokens WHERE token_id = $2),
                        COALESCE((SELECT cumulative_transfer_count FROM metrics_plt_transfer WHERE \
                         token_index = (SELECT index FROM plt_tokens WHERE token_id = $2) ORDER \
                         BY event_timestamp DESC LIMIT 1), 0) + 1,
                        COALESCE((SELECT cumulative_transfer_amount FROM metrics_plt_transfer \
                         WHERE token_index = (SELECT index FROM plt_tokens WHERE token_id = $2) \
                         ORDER BY event_timestamp DESC LIMIT 1), 0) + $3
                    ON CONFLICT (event_timestamp, token_index) DO UPDATE SET
                        cumulative_transfer_count = \
                         GREATEST(metrics_plt_transfer.cumulative_transfer_count, \
                         EXCLUDED.cumulative_transfer_count),
                        cumulative_transfer_amount = \
                         GREATEST(metrics_plt_transfer.cumulative_transfer_amount, \
                         EXCLUDED.cumulative_transfer_amount)
                    ",
                        slot_time,
                        self.token_id,
                        self.plt_amount_change
                    )
                    .execute(tx.as_mut())
                    .await?;

                    let current_from_amount =
                        self.plt_amount_accross_tokens_by_account(tx, from).await?;
                    let current_to_amount =
                        self.plt_amount_accross_tokens_by_account(tx, to).await?;

                    if previous_from_amount > BigDecimal::from(0)
                        && current_from_amount == BigDecimal::from(0)
                    {
                        self.update_metrics_plt_unique_account_count_delta(tx, slot_time, -1)
                            .await?;
                    }
                    if previous_to_amount == BigDecimal::from(0)
                        && current_to_amount > BigDecimal::from(0)
                    {
                        self.update_metrics_plt_unique_account_count_delta(tx, slot_time, 1)
                            .await?;
                    }
                }
            }
            TokenUpdateEventType::TokenModule => {}
        }

        Ok(())
    }

    async fn plt_amount_accross_tokens_by_account(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        account_address: &str,
    ) -> anyhow::Result<BigDecimal> {
        let account_address =
            concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(account_address)
                .map_err(|_| {
                    anyhow::anyhow!(
                        "Failed to convert string into account address type: {}",
                        account_address
                    )
                })?;
        let canonical_address = account_address.get_canonical_address();
        let row = sqlx::query!(
            "SELECT COALESCE(
                (SELECT total_amount FROM public.plt_accounts_sum_amounts WHERE account_index = 
                (SELECT index FROM accounts WHERE canonical_address = $1::bytea)
                ), 0) 
                AS total_amount;",
            canonical_address.0.as_slice(),
        )
        .fetch_one(tx.as_mut())
        .await?;
        Ok(row.total_amount.unwrap_or(BigDecimal::from(0)))
    }

    // METRICS UPDATE: PLT event counter increment
    //
    // Increments cumulative_event_count in metrics_plt table for any PLT operation.
    // Called for all event types: Transfer, Mint, Burn, TokenModule.
    //
    // Query execution:
    // - CTE fetches current metric values from latest row
    // - For the first PLT event in a block, a new row is inserted in metrics_plt
    //   (unique event_timestamp).
    // - For further PLT events in the same block, the row is updated instead of
    //   inserting a new one.
    // - This ensures only one row per block/event_timestamp is created and updated
    //   for all PLT events in that block.
    // - GREATEST() ensures values only increase;
    // - Preserves existing cumulative_transfer_amount and unique_account_count
    //   values
    async fn update_metrics_plt_cumulative_event_count(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        event_timestamp: chrono::DateTime<chrono::Utc>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "
            WITH latest_metrics AS (
                SELECT
                    COALESCE(MAX(cumulative_event_count), 0) AS prev_event_count,
                    COALESCE(MAX(cumulative_transfer_amount), 0) AS prev_transfer_amount,
                    COALESCE(MAX(unique_account_count), 0) AS prev_unique_count
                FROM metrics_plt
            )
            INSERT INTO metrics_plt (
                event_timestamp, 
                cumulative_event_count, 
                cumulative_transfer_amount,
                unique_account_count
            )
            SELECT 
                $1,
                lm.prev_event_count + 1,
                lm.prev_transfer_amount,
                lm.prev_unique_count
            FROM latest_metrics lm
            ON CONFLICT (event_timestamp) DO UPDATE SET
                cumulative_event_count = GREATEST(metrics_plt.cumulative_event_count, \
             EXCLUDED.cumulative_event_count)
            ",
            event_timestamp
        )
        .execute(tx.as_mut())
        .await?;

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

    async fn update_transfer_balances(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        from: &String,
        to: &String,
        amount: &BigDecimal,
    ) -> anyhow::Result<()> {
        let to_account_address =
            concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(to.as_str())
                .map_err(|_| {
                    anyhow::anyhow!("Failed to convert string into account address type: {}", to)
                })?;
        let to_canonical_address = to_account_address.get_canonical_address();

        let from_account_address =
            concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(from).map_err(
                |_| anyhow::anyhow!("Failed to convert string into account address type: {}", from),
            )?;
        let from_canonical_address = from_account_address.get_canonical_address();

        // to Avoid two separate queries, we use a CTE approach
        sqlx::query!(
            "WITH updated_accounts AS (
                INSERT INTO plt_accounts (account_index, token_index, amount, decimal)
                VALUES
                    ( (SELECT index FROM accounts WHERE canonical_address = $1::bytea),
                    (SELECT index FROM plt_tokens WHERE token_id = $3),
                    -$4::numeric,
                    (SELECT decimal FROM plt_tokens WHERE token_id = $3)
                    ),
                    ( (SELECT index FROM accounts WHERE canonical_address = $2::bytea),
                    (SELECT index FROM plt_tokens WHERE token_id = $3),
                    $4::numeric,
                    (SELECT decimal FROM plt_tokens WHERE token_id = $3)
                    )
                ON CONFLICT (account_index, token_index) DO UPDATE
                SET amount = plt_accounts.amount + EXCLUDED.amount
                RETURNING account_index
            )
            INSERT INTO plt_accounts_sum_amounts (account_index, total_amount)
            VALUES
                ( (SELECT index FROM accounts WHERE canonical_address = $1::bytea), -$4::numeric ),
                ( (SELECT index FROM accounts WHERE canonical_address = $2::bytea),  $4::numeric )
            ON CONFLICT (account_index) DO UPDATE
            SET total_amount = plt_accounts_sum_amounts.total_amount + EXCLUDED.total_amount;
            ",
            from_canonical_address.0.as_slice(),
            to_canonical_address.0.as_slice(),
            self.token_id,
            amount,
        )
        .execute(tx.as_mut())
        .await?;

        Ok(())
    }

    async fn update_account_balance(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        account: &String,
        amount: &BigDecimal,
    ) -> anyhow::Result<()> {
        let account_address =
            concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(account.as_str())
                .map_err(|_| {
                    anyhow::anyhow!(
                        "Failed to convert string into account address type: {}",
                        account
                    )
                })?;
        let canonical_address = account_address.get_canonical_address();
        sqlx::query!(
            "WITH 
                acc AS (
                    SELECT index AS account_index FROM accounts WHERE canonical_address = $1::bytea
                ),
                tok AS (
                    SELECT index AS token_index, decimal FROM plt_tokens WHERE token_id = $2
                ),
                upsert_token AS (
                    INSERT INTO plt_accounts (account_index, token_index, amount, decimal)
                    SELECT acc.account_index, tok.token_index, $3, tok.decimal
                    FROM acc, tok
                    ON CONFLICT (account_index, token_index) DO UPDATE
                    SET amount = plt_accounts.amount + $3
                    RETURNING account_index
                )
                INSERT INTO plt_accounts_sum_amounts (account_index, total_amount)
                SELECT acc.account_index, $3 FROM acc
                ON CONFLICT (account_index) DO UPDATE
                SET total_amount = plt_accounts_sum_amounts.total_amount + EXCLUDED.total_amount;
                ",
            canonical_address.0.as_slice(),
            self.token_id,
            amount
        )
        .execute(tx.as_mut())
        .await?;

        Ok(())
    }

    /// Updates the unique_account_count in metrics_plt by incrementing or
    /// decrementing. If `delta` is positive, increments; if negative,
    async fn update_metrics_plt_unique_account_count_delta(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        event_timestamp: chrono::DateTime<chrono::Utc>,
        delta: i64,
    ) -> anyhow::Result<()> {
        if delta == 0 {
            return Ok(());
        }
        let unique_account_count = sqlx::query_scalar!(
            "
        WITH latest_metrics AS (
            SELECT 
                COALESCE(cumulative_event_count, 0) AS prev_event_count,
                COALESCE(cumulative_transfer_amount, 0) AS prev_transfer_amount,
                COALESCE(unique_account_count, 0) AS prev_unique_count
            FROM metrics_plt 
            ORDER BY event_timestamp DESC 
            LIMIT 1
        ),
        upsert AS (
            INSERT INTO metrics_plt (
                event_timestamp, 
                cumulative_event_count,
                cumulative_transfer_amount,
                unique_account_count
            )
            SELECT 
                $1,
                lm.prev_event_count,
                lm.prev_transfer_amount,
                lm.prev_unique_count + $2
            FROM latest_metrics lm
            ON CONFLICT (event_timestamp) DO UPDATE SET
                unique_account_count = EXCLUDED.unique_account_count
            RETURNING unique_account_count
        )
        SELECT unique_account_count FROM upsert
        ",
            event_timestamp,
            delta
        )
        .fetch_one(tx.as_mut())
        .await?;

        if unique_account_count < 0 {
           panic!("unique_account_count went negative: {}", unique_account_count);
        }
        Ok(())
    }
}
