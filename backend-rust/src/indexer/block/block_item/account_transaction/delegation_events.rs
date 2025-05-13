use super::baker_events::BakerRemoved;
use crate::indexer::{ensure_affected_rows::EnsureAffectedRows, statistics::Statistics};
use anyhow::Context;
use concordium_rust_sdk::types::ProtocolVersion;

/// Represents the events from an account configuring a delegator.
#[derive(Debug)]
pub struct PreparedAccountDelegationEvents {
    /// Update the state of the delegator.
    pub events: Vec<PreparedAccountDelegationEvent>,
}

impl PreparedAccountDelegationEvents {
    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        protocol_version: ProtocolVersion,
    ) -> anyhow::Result<()> {
        for event in &self.events {
            event.save(tx, transaction_index, protocol_version).await?;
        }
        Ok(())
    }
}

#[derive(Debug)]
pub enum PreparedAccountDelegationEvent {
    StakeIncrease {
        account_id: i64,
        staked:     i64,
    },
    StakeDecrease {
        account_id: i64,
        staked:     i64,
    },
    SetRestakeEarnings {
        account_id:       i64,
        restake_earnings: bool,
    },
    Added {
        account_id: i64,
    },
    Removed {
        account_id: i64,
    },
    SetDelegationTarget {
        account_id: i64,
        target_id:  Option<i64>,
    },
    RemoveBaker(BakerRemoved),
}

impl PreparedAccountDelegationEvent {
    pub fn prepare(
        event: &concordium_rust_sdk::types::DelegationEvent,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        use concordium_rust_sdk::types::DelegationEvent;
        let prepared = match event {
            DelegationEvent::DelegationStakeIncreased {
                delegator_id,
                new_stake,
            } => PreparedAccountDelegationEvent::StakeIncrease {
                account_id: delegator_id.id.index.try_into()?,
                staked:     new_stake.micro_ccd.try_into()?,
            },
            DelegationEvent::DelegationStakeDecreased {
                delegator_id,
                new_stake,
            } => PreparedAccountDelegationEvent::StakeDecrease {
                account_id: delegator_id.id.index.try_into()?,
                staked:     new_stake.micro_ccd.try_into()?,
            },
            DelegationEvent::DelegationSetRestakeEarnings {
                delegator_id,
                restake_earnings,
            } => PreparedAccountDelegationEvent::SetRestakeEarnings {
                account_id:       delegator_id.id.index.try_into()?,
                restake_earnings: *restake_earnings,
            },
            DelegationEvent::DelegationSetDelegationTarget {
                delegator_id,
                delegation_target,
            } => PreparedAccountDelegationEvent::SetDelegationTarget {
                account_id: delegator_id.id.index.try_into()?,
                target_id:  if let concordium_rust_sdk::types::DelegationTarget::Baker {
                    baker_id,
                } = delegation_target
                {
                    Some(baker_id.id.index.try_into()?)
                } else {
                    None
                },
            },
            DelegationEvent::DelegationAdded {
                delegator_id,
            } => PreparedAccountDelegationEvent::Added {
                account_id: delegator_id.id.index.try_into()?,
            },
            DelegationEvent::DelegationRemoved {
                delegator_id,
            } => PreparedAccountDelegationEvent::Removed {
                account_id: delegator_id.id.index.try_into()?,
            },
            DelegationEvent::BakerRemoved {
                baker_id,
            } => PreparedAccountDelegationEvent::RemoveBaker(BakerRemoved::prepare(
                baker_id, statistics,
            )?),
        };
        Ok(prepared)
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        protocol_version: ProtocolVersion,
    ) -> anyhow::Result<()> {
        let bakers_expected_affected_range = if protocol_version > ProtocolVersion::P6 {
            1..=1
        } else {
            0..=1
        };
        match self {
            PreparedAccountDelegationEvent::StakeIncrease {
                account_id,
                staked,
            }
            | PreparedAccountDelegationEvent::StakeDecrease {
                account_id,
                staked,
            } => {
                // Update total stake of the pool first  (if not the passive pool).
                // Note that `DelegationEvent::Added` event is always accommodated by a
                // `DelegationEvent::StakeIncrease` event, in this case the current
                // `delegated_stake` will be zero.
                sqlx::query!(
                    "UPDATE bakers
                     SET pool_total_staked = pool_total_staked + $1 - accounts.delegated_stake
                     FROM accounts
                     WHERE bakers.id = accounts.delegated_target_baker_id AND accounts.index = $2",
                    staked,
                    account_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(0..=1) // Targeting the passive pool would result in no affected rows.
                .context("Failed update baker pool stake")?;
                // Then the stake of the delegator.
                sqlx::query!(
                    "UPDATE accounts SET delegated_stake = $1 WHERE index = $2",
                    staked,
                    account_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_one_row()
                .context("Failed update delegator stake")?;
            }
            PreparedAccountDelegationEvent::Added {
                account_id,
            } => {
                sqlx::query!(
                    "UPDATE accounts
                     SET delegated_stake = 0,
                         delegated_restake_earnings = FALSE,
                         delegated_target_baker_id = NULL
                     WHERE index = $1",
                    account_id,
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_one_row()
                .context("Failed updating delegator state to be added")?;
            }
            PreparedAccountDelegationEvent::Removed {
                account_id,
            } => {
                // Update the total pool stake when removed.
                // Note that `DelegationEvent::Added` event is always accommodated by a
                // `DelegationEvent::StakeIncrease` event and
                // `DelegationEvent::SetDelegationTarget` event, meaning we don't have to handle
                // updating the pool state here.
                sqlx::query!(
                        "UPDATE bakers
                         SET pool_total_staked = pool_total_staked - accounts.delegated_stake,
                             pool_delegator_count = pool_delegator_count - 1
                         FROM accounts
                         WHERE bakers.id = accounts.delegated_target_baker_id
                             AND accounts.index = $1",
                        account_id
                    )
                    .execute(tx.as_mut())
                    .await?
                    .ensure_affected_rows_in_range(0..=1) // No row affected when target was the passive pool.
                    .context("Failed updating pool state with removed delegator")?;

                sqlx::query!(
                    "UPDATE accounts
                     SET delegated_stake = 0,
                         delegated_restake_earnings = NULL,
                         delegated_target_baker_id = NULL
                     WHERE index = $1",
                    account_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_one_row()
                .context("Failed updating delegator state to be removed")?;
            }

            PreparedAccountDelegationEvent::SetRestakeEarnings {
                account_id,
                restake_earnings,
            } => {
                sqlx::query!(
                    "UPDATE accounts
                        SET delegated_restake_earnings = $1
                    WHERE
                        index = $2
                        -- Ensure we don't update removed delegators
                        -- (prior to P7 this was not immediate)
                        AND delegated_restake_earnings IS NOT NULL",
                    *restake_earnings,
                    account_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(bakers_expected_affected_range)
                .context("Failed update restake earnings for delegator")?;
            }
            PreparedAccountDelegationEvent::SetDelegationTarget {
                account_id,
                target_id,
            } => {
                // Update total pool stake and delegator count for the old target (if old pool
                // was the passive pool or the account just started delegating nothing happens).
                sqlx::query!(
                    "UPDATE bakers
                     SET
                         pool_total_staked = pool_total_staked - accounts.delegated_stake,
                         pool_delegator_count = pool_delegator_count - 1
                     FROM accounts
                     WHERE
                         -- Only consider delegators which are not removed,
                         -- prior to P7 this was not immediate.
                         accounts.delegated_restake_earnings IS NOT NULL
                         AND bakers.id = accounts.delegated_target_baker_id
                         AND accounts.index = $1",
                    account_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_rows_in_range(0..=1) // Affected rows will be 0 for the passive pool
                .context("Failed update pool stake removing delegator")?;
                // Update total pool stake and delegator count for new target.
                if let Some(target) = target_id {
                    sqlx::query!(
                        "UPDATE bakers
                         SET pool_total_staked = pool_total_staked + accounts.delegated_stake,
                             pool_delegator_count = pool_delegator_count + 1
                         FROM accounts
                         WHERE
                             -- Only consider delegators which are not removed,
                             -- prior to P7 this was not immediate.
                             accounts.delegated_restake_earnings IS NOT NULL
                             AND bakers.id = $2
                             AND accounts.index = $1",
                        account_id,
                        target
                    )
                    .execute(tx.as_mut())
                    .await?
                    .ensure_affected_rows_in_range(bakers_expected_affected_range.clone())
                    .context("Failed update pool stake adding delegator")?;
                }
                // Set the new target on the delegator.
                // Prior to Protocol version 7, removing a baker was not immediate, but after
                // some cooldown period, allowing delegators to still target the pool after
                // removal. Since we remove the baker immediate even for older blocks there
                // might not be a baker to target, so we check for existence as part of the
                // query, unless the new target is the passive delegation pool.
                sqlx::query!(
                    "UPDATE accounts
                        SET delegated_target_baker_id = CASE
                                WHEN
                                    $1::BIGINT IS NOT NULL
                                    AND EXISTS(SELECT TRUE FROM bakers WHERE id = $1)
                                THEN $1
                                ELSE NULL
                            END
                    WHERE index = $2",
                    *target_id,
                    account_id
                )
                .execute(tx.as_mut())
                .await?
                .ensure_affected_one_row()
                .context("Failed update delegator target")?;
            }
            PreparedAccountDelegationEvent::RemoveBaker(baker_removed) => {
                baker_removed.save(tx, transaction_index).await?;
            }
        }
        Ok(())
    }
}
