//! Database operations related to baking/validating.

use crate::indexer::ensure_affected_rows::EnsureAffectedRows;
use concordium_rust_sdk::types as sdk_types;

/// Represents the database operation of adding a removed baker to the
/// bakers_removed table.
#[derive(Debug)]
pub struct InsertRemovedBaker {
    baker_id: i64,
}
impl InsertRemovedBaker {
    pub fn prepare(baker_id: &sdk_types::BakerId) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id: baker_id.id.index.try_into()?,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO bakers_removed (id, removed_by_tx_index) VALUES ($1, $2)",
            self.baker_id,
            transaction_index
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()?;
        Ok(())
    }
}

/// Represents the database operation of deleting a baker from the
/// bakers_removed table when present.
#[derive(Debug)]
pub struct DeleteRemovedBakerWhenPresent {
    baker_id: i64,
}
impl DeleteRemovedBakerWhenPresent {
    pub fn prepare(baker_id: &sdk_types::BakerId) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id: baker_id.id.index.try_into()?,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!("DELETE FROM bakers_removed WHERE id = $1", self.baker_id)
            .execute(tx.as_mut())
            .await?
            .ensure_affected_rows_in_range(0..=1)?;
        Ok(())
    }
}
