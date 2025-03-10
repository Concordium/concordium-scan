use anyhow::Result;
use sqlx::{Postgres, Transaction};
use std::collections::HashMap;

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub(crate) enum Field {
    Added,
    Removed,
}

pub(crate) struct Statistics {
    /// The counters as updated via increments.
    current:      HashMap<Field, i64>,
    block_height: i64,
}

impl Statistics {
    pub(crate) fn new(block_height: i64) -> Self {
        Statistics {
            current: HashMap::new(),
            block_height,
        }
    }

    /// Increments the counter for the given field.
    pub(crate) fn increment(&mut self, field: Field, count: i64) {
        // Use entry to set to 0 if not present, then increment.
        *self.current.entry(field).or_insert(0) += count;
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
        if self.current.is_empty() {
            // No increments recorded, nothing to commit.
            return Ok(());
        }

        // Retrieve the increment values for each counter, defaulting to 0.
        let inc_added = self.current.get(&Field::Added).copied().unwrap_or(0);
        let inc_removed = self.current.get(&Field::Removed).copied().unwrap_or(0);

        // Update the latest row in metrics_bakers, adding the increments to the current
        // totals. This assumes that the table has a unique, increasing `index`
        // column.
        let result = sqlx::query!(
            r#"
            INSERT INTO metrics_bakers (
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
            )
            "#,
            self.block_height,
            inc_added,
            inc_removed
        )
        .execute(tx.as_mut())
        .await?;
        if result.rows_affected() == 0 {
            sqlx::query!(
                r#"
            INSERT INTO metrics_bakers (
              block_height,
              total_bakers_added,
              total_bakers_removed
            ) VALUES (
              $1,
              $2,
              $3
            )
            "#,
                self.block_height,
                inc_added,
                inc_removed,
            )
            .execute(tx.as_mut())
            .await?;
        }

        Ok(())
    }
}
