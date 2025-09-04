//! This module contains information computed for rejected account transaction
//! events in an account transaction during the concurrent preprocessing and the
//! logic for how to do the sequential processing into the database.

use anyhow::Context;
use concordium_rust_sdk::{
    base::transactions::{BlockItem, EncodedPayload, Payload},
    types::{self as sdk_types, RejectReason, TransactionType},
};

/// Represents updates related to rejected transactions.
#[derive(Debug)]
pub enum PreparedRejectedEvent {
    /// Rejected transaction attempting to initialize a smart contract
    /// instance or redeploying a module reference.
    ModuleTransaction(PreparedRejectModuleTransaction),
    /// Rejected transaction attempting to update a smart contract instance.
    ContractUpdateTransaction(PreparedRejectContractUpdateTransaction),
    /// Nothing needs to be updated.
    NoEvent,
}

impl PreparedRejectedEvent {
    pub fn prepare(
        transaction_type: Option<&TransactionType>,
        reject_reason: &RejectReason,
        item: &BlockItem<EncodedPayload>,
    ) -> anyhow::Result<Self> {
        let event = match transaction_type.as_ref() {
            Some(&TransactionType::InitContract) | Some(&TransactionType::DeployModule) => {
                if let RejectReason::ModuleNotWF | RejectReason::InvalidModuleReference { .. } =
                    reject_reason
                {
                    // Trying to initialize a smart contract from invalid module
                    // reference or deploying invalid smart contract modules are not
                    // indexed further.
                    Self::NoEvent
                } else {
                    let BlockItem::AccountTransaction(account_transaction) = item else {
                        anyhow::bail!("Block item was expected to be an account transaction")
                    };
                    let decoded = account_transaction
                        .payload
                        .decode()
                        .context("Failed decoding account transaction payload")?;
                    let module_reference = match decoded {
                        Payload::InitContract { payload } => payload.mod_ref,
                        Payload::DeployModule { module } => module.get_module_ref(),
                        _ => anyhow::bail!(
                            "Payload did not match InitContract or DeployModule as expected"
                        ),
                    };

                    Self::ModuleTransaction(PreparedRejectModuleTransaction::prepare(
                        module_reference,
                    )?)
                }
            }
            Some(&TransactionType::Update) => {
                if let RejectReason::InvalidContractAddress { .. } = reject_reason {
                    // Updating a smart contract instances using invalid contract
                    // addresses, i.e. non existing
                    // instance, are not indexed further.
                    Self::NoEvent
                } else {
                    anyhow::ensure!(
                        matches!(
                            reject_reason,
                            RejectReason::InvalidReceiveMethod { .. }
                                | RejectReason::RuntimeFailure
                                | RejectReason::AmountTooLarge { .. }
                                | RejectReason::OutOfEnergy
                                | RejectReason::RejectedReceive { .. }
                                | RejectReason::InvalidAccountReference { .. }
                        ),
                        "Unexpected reject reason for Contract Update transaction: {:?}",
                        reject_reason
                    );

                    let BlockItem::AccountTransaction(account_transaction) = item else {
                        anyhow::bail!("Block item was expected to be an account transaction")
                    };
                    let payload = account_transaction
                        .payload
                        .decode()
                        .context("Failed decoding account transaction payload")?;
                    let Payload::Update { payload } = payload else {
                        anyhow::bail!(
                            "Unexpected payload for transaction of type Update: {:?}",
                            payload
                        )
                    };
                    Self::ContractUpdateTransaction(
                        PreparedRejectContractUpdateTransaction::prepare(payload.address)?,
                    )
                }
            }
            _ => Self::NoEvent,
        };
        Ok(event)
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        match self {
            PreparedRejectedEvent::ModuleTransaction(event) => {
                event.save(tx, transaction_index).await?
            }
            PreparedRejectedEvent::ContractUpdateTransaction(event) => {
                event.save(tx, transaction_index).await?
            }
            PreparedRejectedEvent::NoEvent => (),
        }
        Ok(())
    }
}

#[derive(Debug)]
pub struct PreparedRejectModuleTransaction {
    module_reference: String,
}

impl PreparedRejectModuleTransaction {
    fn prepare(module_reference: sdk_types::hashes::ModuleReference) -> anyhow::Result<Self> {
        Ok(Self {
            module_reference: module_reference.into(),
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO rejected_smart_contract_module_transactions (
                index,
                module_reference,
                transaction_index
            ) VALUES (
                (SELECT
                    COALESCE(MAX(index) + 1, 0)
                FROM rejected_smart_contract_module_transactions
                WHERE module_reference = $1),
            $1, $2)",
            self.module_reference,
            transaction_index
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

#[derive(Debug)]
pub struct PreparedRejectContractUpdateTransaction {
    contract_index: i64,
    contract_sub_index: i64,
}
impl PreparedRejectContractUpdateTransaction {
    fn prepare(address: sdk_types::ContractAddress) -> anyhow::Result<Self> {
        Ok(Self {
            contract_index: i64::try_from(address.index)?,
            contract_sub_index: i64::try_from(address.subindex)?,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO contract_reject_transactions (
                 contract_index,
                 contract_sub_index,
                 transaction_index,
                 transaction_index_per_contract
             ) VALUES (
                 $1,
                 $2,
                 $3,
                 (SELECT
                     COALESCE(MAX(transaction_index_per_contract) + 1, 0)
                  FROM contract_reject_transactions
                  WHERE
                      contract_index = $1 AND contract_sub_index = $2
                 )
             )",
            self.contract_index,
            self.contract_sub_index,
            transaction_index,
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}
