//! This module contains information computed for baker related events in an
//! account transaction during the concurrent preprocessing and the logic for
//! how to do the sequential processing into the database.

use crate::{
    indexer::{
        ensure_affected_rows::EnsureAffectedRows,
        statistics::{BakerField, Statistics},
    },
    transaction_event::baker::BakerPoolOpenStatus,
};
use anyhow::Context;
use concordium_rust_sdk::types::{
    self as sdk_types, queries::ProtocolVersionInt, PartsPerHundredThousands, ProtocolVersion,
};
use tracing::debug;

/// Represents the event of a baker being removed, resulting in the delegators
/// targeting the pool are moved to the passive pool.
#[derive(Debug)]
pub struct BakerRemoved {
    /// Move delegators to the passive pool.
    move_delegators: MovePoolDelegatorsToPassivePool,
    /// Remove the baker from the bakers table.
    remove_baker: RemoveBaker,
    /// Add the baker to the bakers_removed table.
    insert_removed: InsertRemovedBaker,
}
impl BakerRemoved {
    pub fn prepare(
        baker_id: &sdk_types::BakerId,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        statistics.baker_stats.increment(BakerField::Removed, 1);
        Ok(Self {
            move_delegators: MovePoolDelegatorsToPassivePool::prepare(baker_id)?,
            remove_baker: RemoveBaker::prepare(baker_id)?,
            insert_removed: InsertRemovedBaker::prepare(baker_id)?,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.move_delegators
            .save(tx)
            .await
            .context("Failed moving delegators to the passive pool")?;
        self.remove_baker
            .save(tx)
            .await
            .context("Failed removing the validator/baker from the bakers table")?;
        self.insert_removed
            .save(tx, transaction_index)
            .await
            .context("Failed inserting validator/baker to removed bakers table")?;
        Ok(())
    }
}

/// Represent the events from configuring a baker.
#[derive(Debug)]
pub struct PreparedBakerEvents {
    /// Update the status of the baker.
    pub events: Vec<PreparedBakerEvent>,
}

impl PreparedBakerEvents {
    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        protocol_version: ProtocolVersionInt,
    ) -> anyhow::Result<()> {
        for event in &self.events {
            event
                .save(tx, transaction_index, protocol_version)
                .await
                .with_context(|| format!("Failed processing baker event {:?}", event))?;
        }
        Ok(())
    }
}

/// Event changing state related to validators (bakers).
#[derive(Debug)]
pub enum PreparedBakerEvent {
    Add {
        baker_id: i64,
        staked: i64,
        restake_earnings: bool,
        delete_removed_baker: DeleteRemovedBakerWhenPresent,
    },
    Remove(BakerRemoved),
    StakeIncrease {
        baker_id: i64,
        staked: i64,
    },
    StakeDecrease {
        baker_id: i64,
        staked: i64,
    },
    SetRestakeEarnings {
        baker_id: i64,
        restake_earnings: bool,
    },
    SetOpenStatus {
        baker_id: i64,
        open_status: BakerPoolOpenStatus,
        /// When set to ClosedForAll move delegators to passive pool.
        move_delegators: Option<MovePoolDelegatorsToPassivePool>,
    },
    SetMetadataUrl {
        baker_id: i64,
        metadata_url: String,
    },
    SetTransactionFeeCommission {
        baker_id: i64,
        commission: i64,
    },
    SetBakingRewardCommission {
        baker_id: i64,
        commission: i64,
    },
    SetFinalizationRewardCommission {
        baker_id: i64,
        commission: i64,
    },
    RemoveDelegation {
        delegator_id: i64,
    },
    Suspended {
        baker_id: i64,
    },
    Resumed {
        baker_id: i64,
    },
    NoOperation,
}
impl PreparedBakerEvent {
    pub fn prepare(
        event: &concordium_rust_sdk::types::BakerEvent,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        use concordium_rust_sdk::types::BakerEvent;
        let prepared = match event {
            BakerEvent::BakerAdded { data: details } => {
                statistics.baker_stats.increment(BakerField::Added, 1);
                PreparedBakerEvent::Add {
                    baker_id: details.keys_event.baker_id.id.index.try_into()?,
                    staked: details.stake.micro_ccd().try_into()?,
                    restake_earnings: details.restake_earnings,
                    delete_removed_baker: DeleteRemovedBakerWhenPresent::prepare(
                        &details.keys_event.baker_id,
                    )?,
                }
            }
            BakerEvent::BakerRemoved { baker_id } => {
                PreparedBakerEvent::Remove(BakerRemoved::prepare(baker_id, statistics)?)
            }
            BakerEvent::BakerStakeIncreased {
                baker_id,
                new_stake,
            } => PreparedBakerEvent::StakeIncrease {
                baker_id: baker_id.id.index.try_into()?,
                staked: new_stake.micro_ccd().try_into()?,
            },
            BakerEvent::BakerStakeDecreased {
                baker_id,
                new_stake,
            } => PreparedBakerEvent::StakeDecrease {
                baker_id: baker_id.id.index.try_into()?,
                staked: new_stake.micro_ccd().try_into()?,
            },
            BakerEvent::BakerRestakeEarningsUpdated {
                baker_id,
                restake_earnings,
            } => PreparedBakerEvent::SetRestakeEarnings {
                baker_id: baker_id.id.index.try_into()?,
                restake_earnings: *restake_earnings,
            },
            BakerEvent::BakerKeysUpdated { .. } => PreparedBakerEvent::NoOperation,
            BakerEvent::BakerSetOpenStatus {
                baker_id,
                open_status,
            } => {
                let open_status = open_status.known_or_err()?.to_owned().into();
                let move_delegators = if matches!(open_status, BakerPoolOpenStatus::ClosedForAll) {
                    Some(MovePoolDelegatorsToPassivePool::prepare(baker_id)?)
                } else {
                    None
                };
                PreparedBakerEvent::SetOpenStatus {
                    baker_id: baker_id.id.index.try_into()?,
                    open_status,
                    move_delegators,
                }
            }
            BakerEvent::BakerSetMetadataURL {
                baker_id,
                metadata_url,
            } => PreparedBakerEvent::SetMetadataUrl {
                baker_id: baker_id.id.index.try_into()?,
                metadata_url: metadata_url.to_string(),
            },
            BakerEvent::BakerSetTransactionFeeCommission {
                baker_id,
                transaction_fee_commission,
            } => PreparedBakerEvent::SetTransactionFeeCommission {
                baker_id: baker_id.id.index.try_into()?,
                commission: u32::from(PartsPerHundredThousands::from(*transaction_fee_commission))
                    .into(),
            },
            BakerEvent::BakerSetBakingRewardCommission {
                baker_id,
                baking_reward_commission,
            } => PreparedBakerEvent::SetBakingRewardCommission {
                baker_id: baker_id.id.index.try_into()?,
                commission: u32::from(PartsPerHundredThousands::from(*baking_reward_commission))
                    .into(),
            },
            BakerEvent::BakerSetFinalizationRewardCommission {
                baker_id,
                finalization_reward_commission,
            } => PreparedBakerEvent::SetFinalizationRewardCommission {
                baker_id: baker_id.id.index.try_into()?,
                commission: u32::from(PartsPerHundredThousands::from(
                    *finalization_reward_commission,
                ))
                .into(),
            },
            BakerEvent::DelegationRemoved { delegator_id } => {
                PreparedBakerEvent::RemoveDelegation {
                    delegator_id: delegator_id.id.index.try_into()?,
                }
            }
            BakerEvent::BakerSuspended { baker_id } => PreparedBakerEvent::Suspended {
                baker_id: baker_id.id.index.try_into()?,
            },
            BakerEvent::BakerResumed { baker_id } => PreparedBakerEvent::Resumed {
                baker_id: baker_id.id.index.try_into()?,
            },
        };
        Ok(prepared)
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        protocol_version: ProtocolVersionInt,
    ) -> anyhow::Result<()> {
        let bakers_expected_affected_range =
            if protocol_version > ProtocolVersionInt::from(ProtocolVersion::P6) {
                1..=1
            } else {
                0..=1
            };
        match self {
            PreparedBakerEvent::Add {
                baker_id,
                staked,
                restake_earnings,
                delete_removed_baker,
            } => {
                sqlx::query!(
                    "INSERT INTO bakers (id, staked, restake_earnings, pool_total_staked, \
                     pool_delegator_count) VALUES ($1, $2, $3, $4, $5)",
                    baker_id,
                    staked,
                    restake_earnings,
                    staked,
                    0
                )
                .execute(tx.as_mut())
                .await?;
                delete_removed_baker.save(tx).await?
            }
            PreparedBakerEvent::Remove(baker_removed) => {
                baker_removed.save(tx, transaction_index).await?;
            }
            PreparedBakerEvent::StakeIncrease { baker_id, staked } => {
                debug!(
                    "Stake increase event for baker: {:?}, staked amount: {:?}",
                    baker_id, staked
                );
            }
            PreparedBakerEvent::StakeDecrease { baker_id, staked } => {
                debug!(
                    "Stake Decrease event for baker: {:?}, staked amount: {:?}",
                    baker_id, staked
                );
            }
            PreparedBakerEvent::SetRestakeEarnings {
                baker_id,
                restake_earnings,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET restake_earnings = $2 WHERE id=$1",
                    baker_id,
                    restake_earnings,
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed updating validator restake earnings")?;
            }
            PreparedBakerEvent::SetOpenStatus {
                baker_id,
                open_status,
                move_delegators,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET open_status = $2 WHERE id=$1",
                    baker_id,
                    *open_status as BakerPoolOpenStatus,
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range.clone())
                .context("Failed updating open_status of validator")?;
                if let Some(move_operation) = move_delegators {
                    sqlx::query!(
                        "UPDATE bakers
                         SET pool_delegator_count = 0
                         WHERE id = $1",
                        baker_id
                    )
                    .execute(tx.as_mut())
                    .await?
                    .ensure_affected_rows_in_range(bakers_expected_affected_range)
                    .context("Failed updating pool stake when closing for all")?;
                    move_operation.save(tx).await?;
                }
            }
            PreparedBakerEvent::SetMetadataUrl {
                baker_id,
                metadata_url,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET metadata_url = $2 WHERE id=$1",
                    baker_id,
                    metadata_url
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed updating validator metadata url")?;
            }
            PreparedBakerEvent::SetTransactionFeeCommission {
                baker_id,
                commission,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET transaction_commission = $2 WHERE id=$1",
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed updating validator transaction fee commission")?;
            }
            PreparedBakerEvent::SetBakingRewardCommission {
                baker_id,
                commission,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET baking_commission = $2 WHERE id=$1",
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed updating validator transaction fee commission")?;
            }
            PreparedBakerEvent::SetFinalizationRewardCommission {
                baker_id,
                commission,
            } => {
                sqlx::query!(
                    "UPDATE bakers SET finalization_commission = $2 WHERE id=$1",
                    baker_id,
                    commission
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed updating validator transaction fee commission")?;
            }
            PreparedBakerEvent::RemoveDelegation { delegator_id } => {
                // Update pool_delegator_count when we have a Removed Delegation event
                sqlx::query!(
                    "UPDATE bakers
                     SET pool_delegator_count = pool_delegator_count - 1
                     FROM accounts
                     WHERE bakers.id = accounts.delegated_target_baker_id AND accounts.index = $1",
                    delegator_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(0..=1) // None affected when target was passive pool.
                .context("Failed update pool state as delegator is removed")?;
                // Set account information to not be delegating.
                sqlx::query!(
                    "UPDATE accounts
                        SET delegated_stake = 0,
                            delegated_restake_earnings = false,
                            delegated_target_baker_id = NULL
                       WHERE index = $1",
                    delegator_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_one_row()
                .context("Failed update account to remove delegation")?;
            }
            PreparedBakerEvent::Suspended { baker_id } => {
                sqlx::query!(
                    "UPDATE bakers
                     SET
                         self_suspended = $2,
                         inactive_suspended = NULL
                     WHERE id=$1",
                    baker_id,
                    transaction_index
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed update validator state to self-suspended")?;
            }
            PreparedBakerEvent::Resumed { baker_id } => {
                sqlx::query!(
                    "UPDATE bakers
                     SET
                         self_suspended = NULL,
                         inactive_suspended = NULL
                     WHERE id=$1",
                    baker_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed update validator state to resumed from suspension")?;
            }
            PreparedBakerEvent::NoOperation => (),
        }
        Ok(())
    }
}

/// Represents the database operation of removing baker from the baker
/// table.
#[derive(Debug)]
pub struct RemoveBaker {
    baker_id: i64,
}
impl RemoveBaker {
    pub fn prepare(baker_id: &sdk_types::BakerId) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id: baker_id.id.index.try_into()?,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!("DELETE FROM bakers WHERE id=$1", self.baker_id,)
            .execute(tx.as_mut())
            .await?
            .ensure_affected_one_row()
            .context("Failed removing validator")?;
        Ok(())
    }
}

/// Represents the database operation of moving delegators for a pool to the
/// passive pool.
#[derive(Debug)]
pub struct MovePoolDelegatorsToPassivePool {
    /// Baker ID of the pool to move delegators from.
    baker_id: i64,
}
impl MovePoolDelegatorsToPassivePool {
    fn prepare(baker_id: &sdk_types::BakerId) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id: baker_id.id.index.try_into()?,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE accounts
             SET delegated_target_baker_id = NULL
             WHERE delegated_target_baker_id = $1",
            self.baker_id
        )
        .execute(tx.as_mut())
        .await?;
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
    fn prepare(baker_id: &sdk_types::BakerId) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id: baker_id.id.index.try_into()?,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!("DELETE FROM bakers_removed WHERE id = $1", self.baker_id)
            .execute(tx.as_mut())
            .await?
            .ensure_affected_rows_in_range(0..=1)?;
        Ok(())
    }
}

/// Represents the database operation of adding a removed baker to the
/// bakers_removed table.
#[derive(Debug)]
pub struct InsertRemovedBaker {
    baker_id: i64,
}
impl InsertRemovedBaker {
    fn prepare(baker_id: &sdk_types::BakerId) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id: baker_id.id.index.try_into()?,
        })
    }

    async fn save(
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
