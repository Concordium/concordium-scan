//! This module contains information computed for a single block item during the
//! concurrent preprocessing and the logic for how to do the sequential
//! processing into the database.

use crate::{
    indexer::{
        block_preprocessor::BlockData, ensure_affected_rows::EnsureAffectedRows,
        statistics::Statistics,
    },
    transaction_event,
    transaction_reject::PreparedTransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use anyhow::Context;
use concordium_rust_sdk::{
    base::{
        contracts_common::HashSet,
        transactions::{BlockItem, EncodedPayload},
    },
    types::{
        AccountTransactionDetails, AccountTransactionEffects, BlockItemSummary,
        BlockItemSummaryDetails,
    },
    v2,
};

mod account_creation;
mod account_transaction;
mod plt_token_creation;

/// Prepared block item (transaction), ready to be inserted in the database
#[derive(Debug)]
pub struct PreparedBlockItem {
    /// Hash of the transaction
    pub block_item_hash: String,
    /// Cost for the account signing the block item (in microCCD), always 0 for
    /// update and credential deployments.
    ccd_cost:            i64,
    /// Energy cost of the execution of the block item.
    energy_cost:         i64,
    /// Absolute height of the block.
    pub block_height:    i64,
    /// Base58check representation of the account address which signed the
    /// block, none for update and credential deployments.
    sender:              Option<String>,
    /// Whether the block item is an account transaction, update or credential
    /// deployment.
    transaction_type:    DbTransactionType,
    /// The type of account transaction, is none if not an account transaction
    /// or if the account transaction got rejected due to deserialization
    /// failing.
    account_type:        Option<AccountTransactionType>,
    /// The type of credential deployment transaction, is none if not a
    /// credential deployment transaction.
    credential_type:     Option<CredentialDeploymentTransactionType>,
    /// The type of update transaction, is none if not an update transaction.
    update_type:         Option<UpdateTransactionType>,
    /// Whether the block item was successful i.e. not rejected.
    success:             bool,
    /// Events of the block item. Is none for rejected block items.
    events:              Option<serde_json::Value>,
    /// Reject reason the block item. Is none for successful block items.
    reject:              Option<PreparedTransactionRejectReason>,
    /// All affected accounts for this transaction. Each entry is the binary
    /// representation of an account address.
    affected_accounts:   Vec<Vec<u8>>,
    /// Block item events prepared for inserting into the database.
    prepared_event:      PreparedBlockItemEvent,
}

impl PreparedBlockItem {
    pub async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        item_summary: &BlockItemSummary,
        item: &BlockItem<EncodedPayload>,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        let block_height = i64::try_from(data.finalized_block_info.height.height)?;
        let block_item_hash = item_summary.hash.to_string();
        let ccd_cost = if let BlockItemSummaryDetails::AccountCreation(_) = item_summary.details {
            // Account creation does not involve any transaction fees, but still have a
            // non-zero energy_cost.
            0
        } else {
            i64::try_from(data.chain_parameters.ccd_cost(item_summary.energy_cost).micro_ccd)?
        };

        let energy_cost = i64::try_from(item_summary.energy_cost.energy)?;
        let sender = item_summary.sender_account().map(|a| a.to_string());
        let (transaction_type, account_type, credential_type, update_type) =
            match &item_summary.details {
                BlockItemSummaryDetails::AccountTransaction(details) => {
                    let account_transaction_type =
                        details.transaction_type().map(AccountTransactionType::from);
                    (DbTransactionType::Account, account_transaction_type, None, None)
                }
                BlockItemSummaryDetails::AccountCreation(details) => {
                    let credential_type =
                        CredentialDeploymentTransactionType::from(details.credential_type);
                    (DbTransactionType::CredentialDeployment, None, Some(credential_type), None)
                }
                BlockItemSummaryDetails::Update(details) => {
                    let update_type = UpdateTransactionType::from(details.update_type());
                    (DbTransactionType::Update, None, None, Some(update_type))
                }
                BlockItemSummaryDetails::TokenCreationDetails(_token_creation_details) => (
                    DbTransactionType::Update,
                    None,
                    None,
                    Some(UpdateTransactionType::CreatePltUpdate),
                ),
            };

        let success = item_summary.is_success();
        let (events, reject) = if success {
            let events = serde_json::to_value(transaction_event::events_from_summary(
                item_summary.details.clone(),
                data.block_info.block_slot_time,
            )?)?;
            (Some(events), None)
        } else {
            let reject =
                if let BlockItemSummaryDetails::AccountTransaction(AccountTransactionDetails {
                    effects:
                        AccountTransactionEffects::None {
                            reject_reason,
                            ..
                        },
                    ..
                }) = &item_summary.details
                {
                    PreparedTransactionRejectReason::prepare(reject_reason.clone())?
                } else {
                    anyhow::bail!("Invariant violation: Failed transaction without a reject reason")
                };
            (None, Some(reject))
        };
        let affected_accounts = item_summary
            .affected_addresses()
            .iter()
            .map(|acc| acc.get_canonical_address().0.to_vec())
            .collect::<HashSet<Vec<u8>>>()
            .into_iter()
            .collect();

        let prepared_event =
            PreparedBlockItemEvent::prepare(node_client, data, item_summary, item, statistics)
                .await?;

        Ok(Self {
            block_item_hash,
            ccd_cost,
            energy_cost,
            block_height,
            sender,
            transaction_type,
            account_type,
            credential_type,
            update_type,
            success,
            events,
            reject,
            affected_accounts,
            prepared_event,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        let reject = if let Some(reason) = &self.reject {
            Some(reason.process(tx).await?)
        } else {
            None
        };

        let tx_idx = sqlx::query_scalar!(
            "INSERT INTO transactions (
                index,
                hash,
                ccd_cost,
                energy_cost,
                block_height,
                sender_index,
                type,
                type_account,
                type_credential_deployment,
                type_update,
                success,
                events,
                reject
            ) VALUES (
                (SELECT COALESCE(MAX(index) + 1, 0) FROM transactions),
                $1,
                $2,
                $3,
                $4,
                (SELECT index FROM accounts WHERE address = $5),
                $6,
                $7,
                $8,
                $9,
                $10,
                $11,
                $12
            ) RETURNING index",
            self.block_item_hash,
            self.ccd_cost,
            self.energy_cost,
            self.block_height,
            self.sender,
            self.transaction_type as DbTransactionType,
            self.account_type as Option<AccountTransactionType>,
            self.credential_type as Option<CredentialDeploymentTransactionType>,
            self.update_type as Option<UpdateTransactionType>,
            self.success,
            self.events,
            reject
        )
        .fetch_one(tx.as_mut())
        .await
        .context("Failed inserting into transactions")?;
        // Note that this does not include account creation. We handle that when saving
        // the account creation event.
        sqlx::query!(
            "INSERT INTO affected_accounts (transaction_index, account_index)
            SELECT $1, index FROM accounts WHERE canonical_address = ANY($2)",
            tx_idx,
            &self.affected_accounts,
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_rows(self.affected_accounts.len().try_into()?)
        .context("Failed insert into affected_accounts")?;

        // We also need to keep track of the number of transactions on the accounts
        // table.
        sqlx::query!(
            "UPDATE accounts
            SET num_txs = num_txs + 1
            WHERE canonical_address = ANY($1)",
            &self.affected_accounts,
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_rows(self.affected_accounts.len().try_into()?)
        .context("Failed incrementing num_txs for account")?;
        self.prepared_event.save(tx, tx_idx).await.with_context(|| {
            format!(
                "Failed processing block item event from {:?} transaction",
                self.transaction_type
            )
        })?;
        Ok(())
    }
}

/// Different types of block item events that can be prepared.
#[derive(Debug)]
enum PreparedBlockItemEvent {
    /// A new account got created.
    AccountCreation(account_creation::PreparedAccountCreation),
    /// An account transaction event.
    AccountTransaction(Box<account_transaction::PreparedAccountTransaction>),
    /// Chain update transaction event.
    ChainUpdate,
    /// Token creation transaction event
    TokenCreation(plt_token_creation::PreparedTokenCreationDetails),
}

impl PreparedBlockItemEvent {
    async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        item_summary: &BlockItemSummary,
        item: &BlockItem<EncodedPayload>,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        match &item_summary.details {
            BlockItemSummaryDetails::AccountCreation(details) => {
                Ok(PreparedBlockItemEvent::AccountCreation(
                    account_creation::PreparedAccountCreation::prepare(details)?,
                ))
            }
            BlockItemSummaryDetails::AccountTransaction(details) => {
                Ok(PreparedBlockItemEvent::AccountTransaction(Box::new(
                    account_transaction::PreparedAccountTransaction::prepare(
                        node_client,
                        data,
                        details,
                        item,
                        statistics,
                    )
                    .await?,
                )))
            }
            BlockItemSummaryDetails::Update(_) => Ok(PreparedBlockItemEvent::ChainUpdate),
            BlockItemSummaryDetails::TokenCreationDetails(token_creation_details) => {
                Ok(PreparedBlockItemEvent::TokenCreation(
                    plt_token_creation::PreparedTokenCreationDetails::prepare(
                        token_creation_details,
                    )?,
                ))
            }
        }
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        match self {
            PreparedBlockItemEvent::AccountCreation(event) => {
                event.save(tx, transaction_index).await
            }
            PreparedBlockItemEvent::AccountTransaction(account_transaction_event) => {
                account_transaction_event.save(tx, transaction_index).await
            }
            PreparedBlockItemEvent::ChainUpdate => Ok(()),
            PreparedBlockItemEvent::TokenCreation(event) => event.save(tx, transaction_index).await,
        }
    }
}
