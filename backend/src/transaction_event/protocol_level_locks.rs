use crate::{
    scalar_types::DateTime,
    transaction_event::protocol_level_tokens::{CborHolderAccount, Memo, TokenAmount},
};
use anyhow::Context;
use async_graphql::SimpleObject;
use concordium_rust_sdk::{
    base::transactions::TokenUpdatePayload,
    common::cbor,
    protocol_level_tokens::{
        CborMemo, LockConfig as SdkLockConfig, LockConfigSimpleV0,
        LockControllerSimpleV0Capability, LockControllerSimpleV0Grant, LockEvent, LockMetadata,
        LockRecipients, Operation, OperationEvent, RawCbor,
    },
    v2::Upward,
};
use serde::{Deserialize, Serialize};

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockCreated {
    pub lock_id: Option<String>,
    pub config: Option<LockCreateConfig>,
    pub raw_config_present: bool,
    pub config_unavailable: bool,
    pub raw_config: Option<String>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockFunded {
    pub lock_id: String,
    pub token_id: String,
    pub amount: TokenAmount,
    pub memo: Option<Memo>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockSent {
    pub lock_id: String,
    pub token_id: String,
    pub source: CborHolderAccount,
    pub recipient: CborHolderAccount,
    pub amount: TokenAmount,
    pub memo: Option<Memo>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockReleased {
    pub lock_id: String,
    pub token_id: String,
    pub source: CborHolderAccount,
    pub amount: TokenAmount,
    pub memo: Option<Memo>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockCanceled {
    pub lock_id: String,
    pub memo: Option<Memo>,
    pub destroyed: bool,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockDestroyed {
    pub lock_id: String,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockCreateConfig {
    pub expiry: DateTime,
    pub recipients: LockRecipientsConfig,
    pub controller: LockControllerConfig,
    pub metadata: Option<LockMetadataConfig>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockRecipientsConfig {
    pub recipient_type: String,
    pub accounts: Vec<CborHolderAccount>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockControllerConfig {
    pub controller_type: String,
    pub simple_v0: Option<LockControllerSimpleV0Config>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockControllerSimpleV0Config {
    pub grants: Vec<LockControllerGrant>,
    pub token_ids: Vec<String>,
    pub keep_alive: bool,
    pub memo: Option<Memo>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockControllerGrant {
    pub account: CborHolderAccount,
    pub roles: Vec<String>,
}

#[derive(SimpleObject, Serialize, Deserialize, Clone, Debug)]
pub struct LockMetadataConfig {
    pub name: Option<String>,
    pub description: Option<String>,
}

pub fn events_from_token_update(
    events: &[Upward<OperationEvent>],
    payload: Option<&TokenUpdatePayload>,
) -> anyhow::Result<Vec<super::Event>> {
    let operations = payload
        .and_then(|payload| match payload {
            TokenUpdatePayload::Unscoped(payload) => Some(payload),
            TokenUpdatePayload::Scoped(_) => None,
        })
        .map(|payload| payload.decode_operations())
        .transpose()
        .context("Failed decoding token-update operations")?;

    let mut lock_create_events = events.iter().filter_map(|event| match event {
        Upward::Known(OperationEvent::Lock(LockEvent::Create(event))) => Some(event),
        _ => None,
    });
    let destroyed_lock_ids = events
        .iter()
        .filter_map(|event| match event {
            Upward::Known(OperationEvent::Lock(LockEvent::Destroy(event))) => {
                Some(event.lock_id.to_string())
            }
            _ => None,
        })
        .collect::<Vec<_>>();

    let mut output = Vec::new();
    let mut canceled_lock_ids = Vec::new();

    if let Some(operations) = operations {
        for operation in operations.operations {
            match operation {
                Operation::LockCreate(details) => {
                    let emitted = lock_create_events.next();
                    output.push(super::Event::LockCreated(LockCreated::from_operation(
                        details, emitted,
                    )?));
                }
                Operation::LockFund(details) => {
                    output.push(super::Event::LockFunded(details.into()));
                }
                Operation::LockSend(details) => {
                    output.push(super::Event::LockSent(details.into()));
                }
                Operation::LockRelease(details) => {
                    output.push(super::Event::LockReleased(details.into()));
                }
                Operation::LockCancel(details) => {
                    let lock_id = details.lock.to_string();
                    let destroyed = destroyed_lock_ids.contains(&lock_id);
                    canceled_lock_ids.push(lock_id);
                    output.push(super::Event::LockCanceled(LockCanceled::from_operation(
                        details, destroyed,
                    )));
                }
                _ => {}
            }
        }
    }

    for event in events {
        match event {
            Upward::Known(OperationEvent::Token(token_event)) => {
                output.push(super::Event::TokenUpdate(
                    super::protocol_level_tokens::TokenUpdate {
                        token_id: token_event.token_id.clone().into(),
                        event: token_event.event.clone().into(),
                    },
                ));
            }
            Upward::Known(OperationEvent::Lock(LockEvent::Create(event))) => {
                if payload.is_none() {
                    output.push(super::Event::LockCreated(LockCreated::from_emitted_event(
                        event,
                    )));
                }
            }
            Upward::Known(OperationEvent::Lock(LockEvent::Destroy(event))) => {
                let lock_id = event.lock_id.to_string();
                if !canceled_lock_ids.contains(&lock_id) {
                    output.push(super::Event::LockDestroyed(LockDestroyed { lock_id }));
                }
            }
            _ => {}
        }
    }

    Ok(output)
}

impl LockCreated {
    fn from_operation(
        details: concordium_rust_sdk::protocol_level_tokens::LockCreateDetails,
        event: Option<&concordium_rust_sdk::protocol_level_tokens::LockCreateEvent>,
    ) -> anyhow::Result<Self> {
        let raw_config = event.map(|event| event.lock_config.to_string());
        Ok(Self {
            lock_id: event.map(|event| event.lock_id.to_string()),
            config: Some(details.config.try_into()?),
            raw_config_present: event.is_some(),
            config_unavailable: false,
            raw_config,
        })
    }

    fn from_emitted_event(
        event: &concordium_rust_sdk::protocol_level_tokens::LockCreateEvent,
    ) -> Self {
        let config = decode_lock_config(&event.lock_config)
            .ok()
            .and_then(|config| config.try_into().ok());
        Self {
            lock_id: Some(event.lock_id.to_string()),
            config_unavailable: config.is_none(),
            config,
            raw_config_present: true,
            raw_config: Some(event.lock_config.to_string()),
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::LockFundDetails> for LockFunded {
    fn from(details: concordium_rust_sdk::protocol_level_tokens::LockFundDetails) -> Self {
        Self {
            lock_id: details.lock.to_string(),
            token_id: details.token.into(),
            amount: details.amount.into(),
            memo: details.memo.map(memo_from_cbor_memo),
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::LockSendDetails> for LockSent {
    fn from(details: concordium_rust_sdk::protocol_level_tokens::LockSendDetails) -> Self {
        Self {
            lock_id: details.lock.to_string(),
            token_id: details.token.into(),
            source: details.source.into(),
            recipient: details.recipient.into(),
            amount: details.amount.into(),
            memo: details.memo.map(memo_from_cbor_memo),
        }
    }
}

impl From<concordium_rust_sdk::protocol_level_tokens::LockReleaseDetails> for LockReleased {
    fn from(details: concordium_rust_sdk::protocol_level_tokens::LockReleaseDetails) -> Self {
        Self {
            lock_id: details.lock.to_string(),
            token_id: details.token.into(),
            source: details.source.into(),
            amount: details.amount.into(),
            memo: details.memo.map(memo_from_cbor_memo),
        }
    }
}

impl LockCanceled {
    fn from_operation(
        details: concordium_rust_sdk::protocol_level_tokens::LockCancelDetails,
        destroyed: bool,
    ) -> Self {
        Self {
            lock_id: details.lock.to_string(),
            memo: details.memo.map(memo_from_cbor_memo),
            destroyed,
        }
    }
}

impl TryFrom<SdkLockConfig> for LockCreateConfig {
    type Error = anyhow::Error;

    fn try_from(config: SdkLockConfig) -> anyhow::Result<Self> {
        let SdkLockConfig::SimpleV0(config) = config;
        let expiry = DateTime::from_timestamp(config.expiry.seconds.try_into()?, 0)
            .context("Failed to parse lock expiry")?;
        Ok(Self {
            expiry,
            recipients: config.recipients.clone().into(),
            controller: LockControllerConfig::from(config.clone()),
            metadata: config
                .metadata
                .as_ref()
                .and_then(|metadata| LockMetadata::decode_raw_cbor(metadata).ok())
                .map(Into::into),
        })
    }
}

impl From<LockRecipients> for LockRecipientsConfig {
    fn from(recipients: LockRecipients) -> Self {
        match recipients {
            LockRecipients::Any => Self {
                recipient_type: "Any".to_string(),
                accounts: Vec::new(),
            },
            LockRecipients::Limited(accounts) => Self {
                recipient_type: "Limited".to_string(),
                accounts: accounts.into_iter().map(Into::into).collect(),
            },
        }
    }
}

impl From<LockConfigSimpleV0> for LockControllerConfig {
    fn from(config: LockConfigSimpleV0) -> Self {
        Self {
            controller_type: "SimpleV0".to_string(),
            simple_v0: Some(LockControllerSimpleV0Config {
                grants: config.grants.into_iter().map(Into::into).collect(),
                token_ids: config.tokens.into_iter().map(Into::into).collect(),
                keep_alive: config.keep_alive,
                memo: config.memo.map(memo_from_cbor_memo),
            }),
        }
    }
}

impl From<LockControllerSimpleV0Grant> for LockControllerGrant {
    fn from(grant: LockControllerSimpleV0Grant) -> Self {
        Self {
            account: grant.account.into(),
            roles: grant.roles.into_iter().map(role_to_string).collect(),
        }
    }
}

impl From<LockMetadata> for LockMetadataConfig {
    fn from(metadata: LockMetadata) -> Self {
        Self {
            name: metadata.name,
            description: metadata.description,
        }
    }
}

fn decode_lock_config(raw_config: &RawCbor) -> anyhow::Result<SdkLockConfig> {
    cbor::cbor_decode(raw_config.as_ref()).context("Failed decoding lock config")
}

fn memo_from_cbor_memo(memo: CborMemo) -> Memo {
    let memo: concordium_rust_sdk::types::Memo = memo.into();
    memo.into()
}

fn role_to_string(role: LockControllerSimpleV0Capability) -> String {
    match role {
        LockControllerSimpleV0Capability::Fund => "fund",
        LockControllerSimpleV0Capability::Release => "release",
        LockControllerSimpleV0Capability::Send => "send",
        LockControllerSimpleV0Capability::Cancel => "cancel",
    }
    .to_string()
}
