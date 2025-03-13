use anyhow::Result;
use sqlx::{Postgres, Transaction};

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub(crate) enum BakerField {
    Added,
    Removed,
}

pub(crate) struct Statistics {
    baker_is_changed:    bool,
    baker_added_count:   i64,
    baker_removed_count: i64,
    block_height:        i64,
}

impl Statistics {
    pub(crate) fn new(block_height: i64) -> Self {
        Statistics {
            baker_is_changed: false,
            baker_added_count: 0,
            baker_removed_count: 0,
            block_height,
        }
    }

    /// Increments the counter for the given field.
    pub(crate) fn increment(&mut self, field: BakerField, count: i64) {
        let counter = match field {
            BakerField::Removed => &mut self.baker_removed_count,
            BakerField::Added => &mut self.baker_added_count,
        };
        *counter += count;
        self.baker_is_changed = true;
    }

    /// If any counter has been incremented, updates the latest row in
    /// metrics_bakers by adding the increments.
    ///
    /// The SQL query adds the current counter values to the corresponding
    /// columns: total_bakers_added, total_bakers_removed,
    /// total_bakers_resumed, total_bakers_suspended.
    ///
    /// If no increments were recorded (i.e. current is empty), no update is
    /// performed.
    pub(crate) async fn save(&self, tx: &mut Transaction<'static, Postgres>) -> Result<()> {
        if !&self.baker_is_changed {
            return Ok(());
        }

        let result = sqlx::query!(
            "INSERT INTO metrics_bakers (
              block_height,
              total_bakers_added,
              total_bakers_removed
            )
            SELECT
              $1,
              total_bakers_added + $2,
              total_bakers_removed + $3
            FROM (
              SELECT *
              FROM metrics_bakers
              ORDER BY block_height DESC
              LIMIT 1
            )",
            self.block_height,
            self.baker_added_count,
            self.baker_removed_count
        )
        .execute(tx.as_mut())
        .await?;
        let previous_baker_metrics_exists = result.rows_affected() == 0;
        if previous_baker_metrics_exists {
            sqlx::query!(
                "INSERT INTO metrics_bakers (
              block_height,
              total_bakers_added,
              total_bakers_removed
            ) VALUES (
              $1,
              $2,
              $3
            )",
                self.block_height,
                self.baker_added_count,
                self.baker_removed_count,
            )
            .execute(tx.as_mut())
            .await?;
        }

        Ok(())
    }
}
