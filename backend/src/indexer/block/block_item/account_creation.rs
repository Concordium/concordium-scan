//! Information computed for a single account creation (aka. credential
//! deployment) block item during the concurrent preprocessing and the logic for
//! how to do the sequential processing into the database.

use concordium_rust_sdk::base::contracts_common::CanonicalAccountAddress;

/// Prepared database insertion of a new account.
#[derive(Debug)]
pub struct PreparedAccountCreation {
    /// The base58check representation of the canonical account address.
    account_address: String,
    canonical_address: CanonicalAccountAddress,
}

impl PreparedAccountCreation {
    pub fn prepare(
        details: &concordium_rust_sdk::types::AccountCreationDetails,
    ) -> anyhow::Result<Self> {
        Ok(Self {
            account_address: details.address.to_string(),
            canonical_address: details.address.get_canonical_address(),
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        let account_index = sqlx::query_scalar!(
            "INSERT INTO
                accounts (index, address, canonical_address, transaction_index)
            VALUES
                ((SELECT COALESCE(MAX(index) + 1, 0) FROM accounts), $1, $2, $3)
            RETURNING index",
            self.account_address,
            self.canonical_address.0.as_slice(),
            transaction_index,
        )
        .fetch_one(tx.as_mut())
        .await?;

        sqlx::query!(
            "INSERT INTO affected_accounts (transaction_index, account_index)
            VALUES ($1, $2)",
            transaction_index,
            account_index
        )
        .execute(tx.as_mut())
        .await?;

        Ok(())
    }
}
