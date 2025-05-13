use crate::{
    graphql_api::AccountStatementEntryType,
    indexer::{
        db::account::PreparedUpdateAccountBalance, ensure_affected_rows::EnsureAffectedRows,
    },
};
use anyhow::Context;
use chrono::{DateTime, Utc};
use concordium_rust_sdk::{
    base::contracts_common::CanonicalAccountAddress,
    common::types::{Amount, Timestamp},
    id::types::AccountAddress,
    types::AbsoluteBlockHeight,
};

/// Represent the event of a transfer of CCD from one account to another.
#[derive(Debug)]
pub struct PreparedCcdTransferEvent {
    /// Updating the sender account balance.
    update_sender:   PreparedUpdateAccountBalance,
    /// Updating the receivers account balance.
    update_receiver: PreparedUpdateAccountBalance,
}

impl PreparedCcdTransferEvent {
    pub fn prepare(
        sender_address: &AccountAddress,
        receiver_address: &AccountAddress,
        amount: Amount,
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let amount: i64 = amount.micro_ccd().try_into()?;
        let update_sender = PreparedUpdateAccountBalance::prepare(
            sender_address,
            -amount,
            block_height,
            AccountStatementEntryType::TransferOut,
        )?;
        let update_receiver = PreparedUpdateAccountBalance::prepare(
            receiver_address,
            amount,
            block_height,
            AccountStatementEntryType::TransferIn,
        )?;
        Ok(Self {
            update_sender,
            update_receiver,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.update_sender
            .save(tx, Some(transaction_index))
            .await
            .context("Failed processing sender balance update")?;
        self.update_receiver
            .save(tx, Some(transaction_index))
            .await
            .context("Failed processing receiver balance update")?;
        Ok(())
    }
}

#[derive(Debug)]
pub struct PreparedScheduledReleases {
    canonical_address: CanonicalAccountAddress,
    release_times: Vec<DateTime<Utc>>,
    amounts: Vec<i64>,
    target_account_balance_update: PreparedUpdateAccountBalance,
    source_account_balance_update: PreparedUpdateAccountBalance,
}

impl PreparedScheduledReleases {
    pub fn prepare(
        target_address: &AccountAddress,
        source_address: &AccountAddress,
        scheduled_releases: &[(Timestamp, Amount)],
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let capacity = scheduled_releases.len();
        let mut release_times: Vec<DateTime<Utc>> = Vec::with_capacity(capacity);
        let mut amounts: Vec<i64> = Vec::with_capacity(capacity);
        let mut total_amount = 0;
        for (timestamp, amount) in scheduled_releases.iter() {
            release_times.push(DateTime::<Utc>::try_from(*timestamp)?);
            let micro_ccd = i64::try_from(amount.micro_ccd())?;
            amounts.push(micro_ccd);
            total_amount += micro_ccd;
        }
        let target_account_balance_update = PreparedUpdateAccountBalance::prepare(
            target_address,
            total_amount,
            block_height,
            AccountStatementEntryType::TransferIn,
        )?;

        let source_account_balance_update = PreparedUpdateAccountBalance::prepare(
            source_address,
            -total_amount,
            block_height,
            AccountStatementEntryType::TransferOut,
        )?;
        Ok(Self {
            canonical_address: target_address.get_canonical_address(),
            release_times,
            amounts,
            target_account_balance_update,
            source_account_balance_update,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO scheduled_releases (
                transaction_index,
                account_index,
                release_time,
                amount
            )
            SELECT
                $1,
                (SELECT index FROM accounts WHERE canonical_address = $2),
                UNNEST($3::TIMESTAMPTZ[]),
                UNNEST($4::BIGINT[])
            ",
            transaction_index,
            &self.canonical_address.0.as_slice(),
            &self.release_times,
            &self.amounts
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_rows(self.release_times.len().try_into()?)?;
        self.target_account_balance_update.save(tx, Some(transaction_index)).await?;
        self.source_account_balance_update.save(tx, Some(transaction_index)).await?;
        Ok(())
    }
}

/// Represents either moving funds from or to the encrypted balance.
#[derive(Debug)]
pub struct PreparedUpdateEncryptedBalance {
    /// Update the public balance with the amount being moved.
    public_balance_change: PreparedUpdateAccountBalance,
}

impl PreparedUpdateEncryptedBalance {
    pub fn prepare(
        sender: &AccountAddress,
        amount: Amount,
        block_height: AbsoluteBlockHeight,
        operation: CryptoOperation,
    ) -> anyhow::Result<Self> {
        let amount: i64 = amount.micro_ccd().try_into()?;
        let amount = match operation {
            CryptoOperation::Encrypt => -amount,
            CryptoOperation::Decrypt => amount,
        };

        let public_balance_change =
            PreparedUpdateAccountBalance::prepare(sender, amount, block_height, operation.into())?;
        Ok(Self {
            public_balance_change,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.public_balance_change.save(tx, Some(transaction_index)).await?;
        Ok(())
    }
}

pub enum CryptoOperation {
    Decrypt,
    Encrypt,
}

impl From<CryptoOperation> for AccountStatementEntryType {
    fn from(operation: CryptoOperation) -> Self {
        match operation {
            CryptoOperation::Decrypt => AccountStatementEntryType::AmountDecrypted,
            CryptoOperation::Encrypt => AccountStatementEntryType::AmountEncrypted,
        }
    }
}
