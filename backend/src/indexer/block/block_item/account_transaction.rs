//! Information for events computed for a single account transaction block item
//! during the concurrent preprocessing and the logic for how to do the
//! sequential processing into the database.
//!
//! Note: Storing of the block item (transaction) is already covered above
//! this point, here we processed the events and outcomes depending on the type
//! of account transaction.

use crate::{
    graphql_api::AccountStatementEntryType,
    indexer::{
        block::block_item::account_transaction::plt_events::{
            PreparedTokenEvent, PreparedTokenEvents,
        },
        block_preprocessor::BlockData,
        db::update_account_balance::PreparedUpdateAccountBalance,
        statistics::Statistics,
    },
};
use anyhow::{Context, Ok};
use chrono::{DateTime, Utc};
use concordium_rust_sdk::{
    base::transactions::{BlockItem, EncodedPayload},
    id::types::AccountAddress,
    types::{AccountTransactionDetails, AccountTransactionEffects, ProtocolVersion},
    v2,
};

mod baker_events;
mod contract_events;
mod delegation_events;
mod module_events;
mod plt_events;
mod rejected_events;
mod transfer_events;

#[derive(Debug)]
pub struct PreparedAccountTransaction {
    /// Update the balance of the sender account with the cost (transaction
    /// fee).
    fee: PreparedUpdateAccountBalance,
    /// Updates based on the events of the account transaction.
    event: PreparedEventEnvelope,
}

impl PreparedAccountTransaction {
    pub async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        details: &AccountTransactionDetails,
        item: &BlockItem<EncodedPayload>,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        let fee = PreparedUpdateAccountBalance::prepare(
            &details.sender,
            -i64::try_from(details.cost.micro_ccd())?,
            data.block_info.block_height,
            AccountStatementEntryType::TransactionFee,
        )?;
        let event = PreparedEventEnvelope::prepare(
            node_client,
            data,
            details,
            item,
            &details.sender,
            statistics,
        )
        .await?;
        Ok(Self { fee, event })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        slot_time: DateTime<Utc>,
    ) -> anyhow::Result<()> {
        self.fee.save(tx, Some(transaction_index)).await?;
        self.event.save(tx, transaction_index, slot_time).await
    }
}

/// Wraps a prepared event together with metadata needed for its processing.
///
/// Prior to protocol version 7, baker removal was delayed by a cooldown period
/// during which other baker-related transactions could still occur, potentially
/// resulting in no affected rows. This envelope provides the necessary context
/// (e.g. protocol version) to correctly validate the processing of events.
#[derive(Debug)]
struct PreparedEventEnvelope {
    metadata: EventMetadata,
    event: PreparedEvent,
}

impl PreparedEventEnvelope {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        details: &AccountTransactionDetails,
        item: &BlockItem<EncodedPayload>,
        sender: &AccountAddress,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        let event =
            PreparedEvent::prepare(node_client, data, details, item, sender, statistics).await?;
        let metadata = EventMetadata {
            protocol_version: data.block_info.protocol_version,
        };
        Ok(PreparedEventEnvelope { metadata, event })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        tx_idx: i64,
        slot_time: DateTime<Utc>,
    ) -> anyhow::Result<()> {
        self.event
            .save(tx, tx_idx, self.metadata.protocol_version, slot_time)
            .await
    }
}

/// Contains metadata required for event processing that is not directly tied to
/// individual events.
#[derive(Debug)]
struct EventMetadata {
    protocol_version: ProtocolVersion,
}

#[derive(Debug)]
enum PreparedEvent {
    /// A transfer of CCD from one account to another account.
    CcdTransfer(transfer_events::PreparedCcdTransferEvent),
    /// Event of moving funds either from or to the encrypted balance.
    EncryptedBalance(transfer_events::PreparedUpdateEncryptedBalance),
    /// Changes related to validators (previously referred to as bakers).
    BakerEvents(baker_events::PreparedBakerEvents),
    /// Account delegation events
    AccountDelegationEvents(delegation_events::PreparedAccountDelegationEvents),
    /// Smart contract module got deployed.
    ModuleDeployed(module_events::PreparedModuleDeployed),
    /// Contract got initialized.
    ContractInitialized(contract_events::PreparedContractInitialized),
    /// Contract got updated.
    ContractUpdate(contract_events::PreparedContractUpdates),
    /// A scheduled transfer got executed.
    ScheduledTransfer(transfer_events::PreparedScheduledReleases),
    /// Rejected transaction.
    RejectedTransaction(rejected_events::PreparedRejectedEvent),
    /// No changes in the database was caused by this event.
    NoOperation,

    /// Events related to token update AccountTransactionEffects.
    TokenUpdateEvents(plt_events::PreparedTokenEvents),
}
impl PreparedEvent {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        details: &AccountTransactionDetails,
        item: &BlockItem<EncodedPayload>,
        sender: &AccountAddress,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        let height = data.block_info.block_height;
        let prepared_event = match &details.effects {
            AccountTransactionEffects::None {
                transaction_type,
                reject_reason,
            } => {
                PreparedEvent::RejectedTransaction(rejected_events::PreparedRejectedEvent::prepare(
                    transaction_type.as_ref(),
                    reject_reason,
                    item,
                )?)
            }
            AccountTransactionEffects::ModuleDeployed { module_ref } => {
                PreparedEvent::ModuleDeployed(
                    module_events::PreparedModuleDeployed::prepare(node_client, *module_ref)
                        .await?,
                )
            }
            AccountTransactionEffects::ContractInitialized { data: event_data } => {
                PreparedEvent::ContractInitialized(
                    contract_events::PreparedContractInitialized::prepare(
                        node_client,
                        data,
                        event_data,
                        sender,
                    )
                    .await?,
                )
            }
            AccountTransactionEffects::ContractUpdateIssued { effects } => {
                PreparedEvent::ContractUpdate(
                    contract_events::PreparedContractUpdates::prepare(node_client, data, effects)
                        .await?,
                )
            }
            AccountTransactionEffects::AccountTransfer { amount, to }
            | AccountTransactionEffects::AccountTransferWithMemo { amount, to, .. } => {
                PreparedEvent::CcdTransfer(transfer_events::PreparedCcdTransferEvent::prepare(
                    sender, to, *amount, height,
                )?)
            }

            AccountTransactionEffects::BakerAdded { data: event_data } => {
                let event = concordium_rust_sdk::types::BakerEvent::BakerAdded {
                    data: event_data.clone(),
                };
                let prepared = baker_events::PreparedBakerEvent::prepare(&event, statistics)?;
                PreparedEvent::BakerEvents(baker_events::PreparedBakerEvents {
                    events: vec![prepared],
                })
            }
            AccountTransactionEffects::BakerRemoved { baker_id } => {
                let event = concordium_rust_sdk::types::BakerEvent::BakerRemoved {
                    baker_id: *baker_id,
                };
                let prepared = baker_events::PreparedBakerEvent::prepare(&event, statistics)?;
                PreparedEvent::BakerEvents(baker_events::PreparedBakerEvents {
                    events: vec![prepared],
                })
            }
            AccountTransactionEffects::BakerStakeUpdated { data: update } => {
                let Some(update) = update else {
                    // No change in baker stake
                    return Ok(PreparedEvent::NoOperation);
                };

                let event = if update.increased {
                    concordium_rust_sdk::types::BakerEvent::BakerStakeIncreased {
                        baker_id: update.baker_id,
                        new_stake: update.new_stake,
                    }
                } else {
                    concordium_rust_sdk::types::BakerEvent::BakerStakeDecreased {
                        baker_id: update.baker_id,
                        new_stake: update.new_stake,
                    }
                };
                let prepared = baker_events::PreparedBakerEvent::prepare(&event, statistics)?;

                PreparedEvent::BakerEvents(baker_events::PreparedBakerEvents {
                    events: vec![prepared],
                })
            }
            AccountTransactionEffects::BakerRestakeEarningsUpdated {
                baker_id,
                restake_earnings,
            } => {
                let events = vec![baker_events::PreparedBakerEvent::prepare(
                    &concordium_rust_sdk::types::BakerEvent::BakerRestakeEarningsUpdated {
                        baker_id: *baker_id,
                        restake_earnings: *restake_earnings,
                    },
                    statistics,
                )?];
                PreparedEvent::BakerEvents(baker_events::PreparedBakerEvents { events })
            }
            AccountTransactionEffects::BakerKeysUpdated { .. } => PreparedEvent::NoOperation,
            AccountTransactionEffects::BakerConfigured { data: events } => {
                PreparedEvent::BakerEvents(baker_events::PreparedBakerEvents {
                    events: events
                        .iter()
                        .map(|event| baker_events::PreparedBakerEvent::prepare(event, statistics))
                        .collect::<anyhow::Result<Vec<_>>>()?,
                })
            }

            AccountTransactionEffects::EncryptedAmountTransferred { .. }
            | AccountTransactionEffects::EncryptedAmountTransferredWithMemo { .. } => {
                PreparedEvent::NoOperation
            }
            AccountTransactionEffects::TransferredToEncrypted { data } => {
                PreparedEvent::EncryptedBalance(
                    transfer_events::PreparedUpdateEncryptedBalance::prepare(
                        sender,
                        data.amount,
                        height,
                        transfer_events::CryptoOperation::Encrypt,
                    )?,
                )
            }
            AccountTransactionEffects::TransferredToPublic { amount, .. } => {
                PreparedEvent::EncryptedBalance(
                    transfer_events::PreparedUpdateEncryptedBalance::prepare(
                        sender,
                        *amount,
                        height,
                        transfer_events::CryptoOperation::Decrypt,
                    )?,
                )
            }
            AccountTransactionEffects::TransferredWithSchedule {
                to,
                amount: scheduled_releases,
            }
            | AccountTransactionEffects::TransferredWithScheduleAndMemo {
                to,
                amount: scheduled_releases,
                ..
            } => PreparedEvent::ScheduledTransfer(
                transfer_events::PreparedScheduledReleases::prepare(
                    to,
                    sender,
                    scheduled_releases,
                    height,
                )?,
            ),
            AccountTransactionEffects::CredentialKeysUpdated { .. }
            | AccountTransactionEffects::CredentialsUpdated { .. }
            | AccountTransactionEffects::DataRegistered { .. } => PreparedEvent::NoOperation,
            AccountTransactionEffects::DelegationConfigured { data: events } => {
                PreparedEvent::AccountDelegationEvents(
                    delegation_events::PreparedAccountDelegationEvents {
                        events: events
                            .iter()
                            .map(|event| {
                                delegation_events::PreparedAccountDelegationEvent::prepare(
                                    event, statistics,
                                )
                            })
                            .collect::<anyhow::Result<Vec<_>>>()?,
                    },
                )
            }
            AccountTransactionEffects::TokenUpdate { events } => {
                PreparedEvent::TokenUpdateEvents(PreparedTokenEvents {
                    events: events
                        .iter()
                        .map(PreparedTokenEvent::prepare)
                        .collect::<anyhow::Result<Vec<_>>>()?,
                })
            }
        };
        Ok(prepared_event)
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        tx_idx: i64,
        protocol_version: ProtocolVersion,
        slot_time: DateTime<Utc>,
    ) -> anyhow::Result<()> {
        match self {
            PreparedEvent::CcdTransfer(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event of CCD transfer"),
            PreparedEvent::EncryptedBalance(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event of encrypted balance"),
            PreparedEvent::BakerEvents(event) => event
                .save(tx, tx_idx, protocol_version)
                .await
                .context("Failed processing block item event with baker event"),
            PreparedEvent::ModuleDeployed(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event with module deploy"),
            PreparedEvent::ContractInitialized(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event with contract initialized"),
            PreparedEvent::ContractUpdate(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event with contract update"),
            PreparedEvent::AccountDelegationEvents(event) => event
                .save(tx, tx_idx, protocol_version)
                .await
                .context("Failed processing block item event with account delegation event"),
            PreparedEvent::ScheduledTransfer(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event with scheduled transfer"),
            PreparedEvent::RejectedTransaction(event) => event
                .save(tx, tx_idx)
                .await
                .context("Failed processing block item event with rejected event"),
            PreparedEvent::TokenUpdateEvents(event) => event
                .save(tx, tx_idx, slot_time)
                .await
                .context("Failed processing block item event with token update events"),
            PreparedEvent::NoOperation => Ok(()),
        }
    }
}
