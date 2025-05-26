use crate::scalar_types::DateTime;
use anyhow::Result;
use concordium_rust_sdk::base::contracts_common::CanonicalAccountAddress;
use std::collections::HashMap;
use tracing::debug;

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub(crate) enum BakerField {
    Added,
    Removed,
}

/// BakerStatistics holds baker-related counters and tracks whether a change has
/// occurred.
pub(crate) struct BakerStatistics {
    baker_is_changed:    bool,
    baker_added_count:   i64,
    baker_removed_count: i64,
    block_height:        i64,
}

impl BakerStatistics {
    /// Creates a new BakerStatistics instance for the given block.
    pub(crate) fn new(block_height: i64) -> Self {
        Self {
            baker_is_changed: false,
            baker_added_count: 0,
            baker_removed_count: 0,
            block_height,
        }
    }

    /// Increments the counter for the given BakerField by `count`.
    pub(crate) fn increment(&mut self, field: BakerField, count: i64) {
        let counter = match field {
            BakerField::Added => &mut self.baker_added_count,
            BakerField::Removed => &mut self.baker_removed_count,
        };
        *counter += count;
        self.baker_is_changed = true;
    }

    /// Saves the baker metrics
    /// If changes have been recorded (baker_is_changed is true), it first
    /// attempts to update the latest row by adding the increments. If no
    /// row exists, it inserts a new row.
    pub(crate) async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> Result<()> {
        if !self.baker_is_changed {
            debug!("No change in baker count at block_height: {}", self.block_height);
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

        let previous_baker_metrics_exists = result.rows_affected() > 0;
        if !previous_baker_metrics_exists {
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

/// RewardStatistics holds rewards for individual accounts
pub(crate) struct RewardStatistics {
    account_rewards: HashMap<CanonicalAccountAddress, i64>,
    block_height:    i64,
    block_slot_time: DateTime,
}

impl RewardStatistics {
    pub(crate) fn new(block_height: i64, slot_time: DateTime) -> Self {
        Self {
            account_rewards: HashMap::new(),
            block_height,
            block_slot_time: slot_time,
        }
    }

    /// Increments the reward counter for the specified account by `count`.
    pub(crate) fn increment(&mut self, account_id: CanonicalAccountAddress, count: i64) {
        *self.account_rewards.entry(account_id).or_insert(0) += count;
    }

    /// Saves the reward statistics.
    /// For each account in the hashmap, a new row is inserted for the current
    /// block. Does no database operations given account rewards are empty
    pub(crate) async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> Result<()> {
        if self.account_rewards.is_empty() {
            debug!("No rewards at block_height: {}", self.block_height);
            return Ok(());
        }

        for (&accound_address, &reward) in self.account_rewards.iter() {
            sqlx::query!(
                "INSERT INTO metrics_rewards (
                  block_height,
                  block_slot_time,
                  account_index,
                  amount
                ) VALUES (
                  $1, $2, (SELECT index FROM accounts WHERE canonical_address = $3), $4
                )",
                self.block_height,
                self.block_slot_time,
                accound_address.0.as_slice(),
                reward,
            )
            .execute(tx.as_mut())
            .await?;
        }
        Ok(())
    }
}

/// Composite Statistics that holds different types of statistics
pub(crate) struct Statistics {
    pub baker_stats:  BakerStatistics,
    pub reward_stats: RewardStatistics,
}

impl Statistics {
    pub(crate) fn new(block_height: i64, slot_time: DateTime) -> Self {
        Self {
            baker_stats:  BakerStatistics::new(block_height),
            reward_stats: RewardStatistics::new(block_height, slot_time),
        }
    }

    /// Persists statistics changes if recorded
    pub(crate) async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> Result<()> {
        self.baker_stats.save(tx).await?;
        self.reward_stats.save(tx).await?;
        Ok(())
    }
}
