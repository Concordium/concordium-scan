//! Module with database operation for updating the balance of an account, while
//! building the account statements index.

use crate::{
    graphql_api::AccountStatementEntryType, indexer::ensure_affected_rows::EnsureAffectedRows,
};
use anyhow::Context;
use concordium_rust_sdk::{
    base::contracts_common::CanonicalAccountAddress, id::types::AccountAddress,
    types::AbsoluteBlockHeight,
};

/// Represents change in the balance of some account.
#[derive(Debug)]
pub struct PreparedUpdateAccountBalance {
    /// Address of the account.
    canonical_address: CanonicalAccountAddress,
    /// Difference in the balance.
    change: i64,
    /// Tracking the account statement causing the change in balance.
    account_statement: PreparedAccountStatement,
}

impl PreparedUpdateAccountBalance {
    pub fn prepare(
        sender: &AccountAddress,
        amount: i64,
        block_height: AbsoluteBlockHeight,
        transaction_type: AccountStatementEntryType,
    ) -> anyhow::Result<Self> {
        let canonical_address = sender.get_canonical_address();
        let account_statement = PreparedAccountStatement {
            block_height: block_height.height.try_into()?,
            amount,
            canonical_address,
            transaction_type,
        };
        Ok(Self {
            canonical_address,
            change: amount,
            account_statement,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: Option<i64>,
    ) -> anyhow::Result<()> {
        if self.change == 0 {
            // Difference of 0 means nothing needs to be updated.
            return Ok(());
        }
        sqlx::query!(
            "UPDATE accounts SET amount = amount + $1 WHERE canonical_address = $2",
            self.change,
            self.canonical_address.0.as_slice(),
        )
        .execute(tx.as_mut())
        .await
        .with_context(|| {
            format!(
                "Failed processing update to account balance, change: {}, canonical address: {:?}",
                self.change, self.canonical_address
            )
        })?
        .ensure_affected_one_row()
        .with_context(|| {
            format!(
                "Failed processing update to account balance, change: {}, canonical address: {:?}",
                self.change, self.canonical_address
            )
        })?;
        // Add the account statement, note that this operation assumes the account
        // balance is already updated.
        self.account_statement.save(tx, transaction_index).await?;
        Ok(())
    }
}

/// Database operation for adding new row into the account statement table.
/// This reads the current balance of the account and assumes the balance is
/// already updated with the amount part of the statement.
#[derive(Debug)]
struct PreparedAccountStatement {
    canonical_address: CanonicalAccountAddress,
    amount: i64,
    block_height: i64,
    transaction_type: AccountStatementEntryType,
}

impl PreparedAccountStatement {
    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: Option<i64>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "WITH 
                account_info AS (
                        SELECT index AS account_index, amount AS current_balance
                        FROM accounts
                        WHERE canonical_address = $1
                )
                INSERT INTO account_statements (
                    account_index,
                    entry_type,
                    amount,
                    block_height,
                    transaction_id,
                    account_balance,
                    slot_time
                )
                SELECT
                    account_index,
                    $2,
                    $3,
                    $4,
                    $5,
                    current_balance,
                    (SELECT slot_time FROM blocks WHERE height = $4)
                FROM account_info",
            self.canonical_address.0.as_slice(),
            self.transaction_type as AccountStatementEntryType,
            self.amount,
            self.block_height,
            transaction_index
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()
        .with_context(|| format!("Failed insert into account_statements: {:?}", self))?;

        Ok(())
    }
}
