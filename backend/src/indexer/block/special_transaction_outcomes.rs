//! Data collected for special transaction outcomes for in each block during the
//! concurrent preprocessing and the logic for how to do the sequential
//! processing into the database.
//! Special transaction outcomes are events in a block, which are not an
//! immediate outcome of a block item (transaction) on chain, such as rewards
//! and validator suspension.

use anyhow::Context;
use concordium_rust_sdk::{
    base::contracts_common::CanonicalAccountAddress,
    id::types::AccountAddress,
    types::{queries::BlockInfo, AbsoluteBlockHeight, ProtocolVersion, SpecialTransactionOutcome},
    v2,
};

use crate::{
    block_special_event::{SpecialEvent, SpecialEventTypeFilter},
    graphql_api::AccountStatementEntryType,
    indexer::{
        db::update_account_balance::PreparedUpdateAccountBalance,
        ensure_affected_rows::EnsureAffectedRows, statistics::Statistics,
    },
};

pub mod payday;
pub mod validator_suspension;

/// Represents changes in the database from special transaction outcomes from a
/// block.
pub struct PreparedSpecialTransactionOutcomes {
    /// Insert the special transaction outcomes for this block.
    insert_special_transaction_outcomes: PreparedInsertBlockSpecialTransactionOutcomes,
    /// Updates to various tables depending on the type of special transaction
    /// outcome.
    updates: Vec<PreparedSpecialTransactionOutcomeUpdate>,
    /// Present if block is a payday block with its associated updates.
    payday_updates: Option<payday::PreparedPayDayBlock>,
}

impl PreparedSpecialTransactionOutcomes {
    pub async fn prepare(
        node_client: &mut v2::Client,
        block_info: &BlockInfo,
        events: &[SpecialTransactionOutcome],
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        // Return whether the block is a payday block. This is always false for
        // protocol versions before P4. In protocol version 4 and later this is the
        // block where all the rewards are paid out.
        let is_payday_block = events.iter().any(|ev| {
            matches!(
                ev,
                SpecialTransactionOutcome::PaydayFoundationReward { .. }
                    | SpecialTransactionOutcome::PaydayAccountReward { .. }
                    | SpecialTransactionOutcome::PaydayPoolReward { .. }
            )
        });

        let payday_updates = if is_payday_block {
            Some(payday::PreparedPayDayBlock::prepare(node_client, block_info).await?)
        } else {
            None
        };

        Ok(Self {
            insert_special_transaction_outcomes:
                PreparedInsertBlockSpecialTransactionOutcomes::prepare(
                    block_info.block_height,
                    events,
                )?,
            updates: events
                .iter()
                .map(|event| {
                    PreparedSpecialTransactionOutcomeUpdate::prepare(event, block_info, statistics)
                })
                .collect::<Result<_, _>>()?,
            payday_updates,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        self.insert_special_transaction_outcomes.save(tx).await?;
        if let Some(payday_updates) = &self.payday_updates {
            payday_updates.save(tx).await?;
        }
        for update in self.updates.iter() {
            update.save(tx).await?;
        }
        Ok(())
    }
}

/// The `SpecialEvents` of a payday block in the order they
/// occur in the block.
struct PreparedPaydaySpecialTransactionOutcomes {
    /// Height of the payday block containing the events.
    block_height: i64,
    has_reward_events: bool,
    // Total rewards
    total_rewards_pool_owners: Vec<Option<i64>>,
    total_transaction_rewards: Vec<i64>,
    total_baking_rewards: Vec<i64>,
    total_finalization_rewards: Vec<i64>,
    // Delegator rewards
    delegators_rewards_pool_owners: Vec<Option<i64>>,
    delegators_rewards_canonical_addresses: Vec<Vec<u8>>,
    delegators_transaction_rewards: Vec<i64>,
    delegators_baking_rewards: Vec<i64>,
    delegators_finalization_rewards: Vec<i64>,
}

impl PreparedPaydaySpecialTransactionOutcomes {
    fn prepare(block_height: i64, events: &[SpecialTransactionOutcome]) -> anyhow::Result<Self> {
        // Extract the rewards from the `SpecialEvents` in each payday block
        // and associate it with the `pool_owner`.
        // The `pool_owner` can be either a `baker_id` or `None`.
        // The `pool_owner` is `None` if the pool rewards are for the passive
        // delegators which can happen at most once per payday block.
        //
        // https://docs.rs/concordium-rust-sdk/6.0.0/concordium_rust_sdk/types/enum.SpecialTransactionOutcome.html#variant.PaydayAccountReward
        // The order of `SpecialEvents` in each payday block has a meaning to
        // determine which rewards go to the baker of a baker pool and which
        // rewards go to the pool's delegators.
        //
        // For example:
        // PaydayPoolReward to pool 1
        // PaydayAccountReward to account 5
        // PaydayAccountReward to account 6
        // PaydayAccountReward to account 1
        // PaydayPoolReward to pool 8
        // PaydayAccountReward to account 8
        // PaydayAccountReward to account 2
        // PaydayPoolReward to `None`
        // PaydayAccountReward to account 10
        // PaydayAccountReward to account 3
        // Means 5, 6 are receiving rewards from delegating to 1 and 2 receiving rewards
        // from delegating to 8, and 10, 3 are receiving rewards from passive
        // delegation.
        //
        // `PaydayPoolReward` and `PaydayAccountReward` events only occur in payday
        // blocks starting in protocol 4.
        let mut total_rewards_pool_owners: Vec<Option<i64>> = vec![];
        let mut total_transaction_rewards: Vec<i64> = vec![];
        let mut total_baking_rewards: Vec<i64> = vec![];
        let mut total_finalization_rewards: Vec<i64> = vec![];

        let mut delegators_rewards_pool_owners: Vec<Option<i64>> = vec![];
        let mut delegators_rewards_canonical_addresses: Vec<Vec<u8>> = vec![];
        let mut delegators_transaction_rewards: Vec<i64> = vec![];
        let mut delegators_baking_rewards: Vec<i64> = vec![];
        let mut delegators_finalization_rewards: Vec<i64> = vec![];

        let mut last_pool_owner: Option<Option<i64>> = None;
        let has_reward_events = !events.is_empty();

        for event in events {
            match event {
                SpecialTransactionOutcome::PaydayPoolReward {
                    pool_owner,
                    transaction_fees,
                    baker_reward,
                    finalization_reward,
                } => {
                    let pool_owner =
                        pool_owner.map(|baker_id| baker_id.id.index.try_into()).transpose()?;

                    total_rewards_pool_owners.push(pool_owner);
                    total_transaction_rewards.push(transaction_fees.micro_ccd.try_into()?);
                    total_baking_rewards.push(baker_reward.micro_ccd.try_into()?);
                    total_finalization_rewards.push(finalization_reward.micro_ccd.try_into()?);

                    last_pool_owner = Some(pool_owner);
                }
                SpecialTransactionOutcome::PaydayAccountReward {
                    transaction_fees,
                    baker_reward,
                    finalization_reward,
                    account,
                } => {
                    // Collect all rewards from the delegators and associate the rewards with their
                    // baker pools.
                    if let Some(last_pool_owner) = last_pool_owner {
                        delegators_rewards_pool_owners.push(last_pool_owner);
                        delegators_rewards_canonical_addresses
                            .push(account.get_canonical_address().0.to_vec());
                        delegators_transaction_rewards.push(transaction_fees.micro_ccd.try_into()?);
                        delegators_baking_rewards.push(baker_reward.micro_ccd.try_into()?);
                        delegators_finalization_rewards
                            .push(finalization_reward.micro_ccd.try_into()?);
                    }
                }
                _ => {}
            }
        }

        Ok(Self {
            block_height,
            has_reward_events,
            total_rewards_pool_owners,
            total_transaction_rewards,
            total_baking_rewards,
            total_finalization_rewards,
            delegators_rewards_pool_owners,
            delegators_rewards_canonical_addresses,
            delegators_transaction_rewards,
            delegators_baking_rewards,
            delegators_finalization_rewards,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        if !self.has_reward_events {
            return Ok(());
        }
        // Calculate and insert the delegators' rewards.
        // Don't record the rewards if they are associated with the baker itself
        // (not a delegator) hence we check that `pool_owner IS DISTINCT FROM
        // account_index`.
        sqlx::query!(
            "
            INSERT INTO bakers_payday_pool_rewards (
                payday_block_height,
                pool_owner,
                payday_delegators_transaction_rewards,
                payday_delegators_baking_rewards,
                payday_delegators_finalization_rewards
            )
            SELECT
                $1 AS payday_block_height,
                pool_owner,
                SUM(
                    CASE WHEN 
                        pool_owner IS DISTINCT FROM account_index
                            THEN payday_delegators_transaction_rewards 
                            ELSE 0 
                    END
                ) AS payday_delegators_transaction_rewards,
                SUM(
                    CASE WHEN 
                        pool_owner IS DISTINCT FROM account_index 
                            THEN payday_delegators_baking_rewards 
                            ELSE 0 
                    END
                ) AS payday_delegators_baking_rewards,
                SUM(
                    CASE 
                    WHEN pool_owner IS DISTINCT FROM account_index
                        THEN payday_delegators_finalization_rewards 
                        ELSE 0 
                    END
                ) AS payday_delegators_finalization_rewards
            FROM (
                SELECT 
                    pool_owner_data.pool_owner, 
                    accounts.index AS account_index,
                    tx_rewards.payday_delegators_transaction_rewards,
                    baker_rewards.payday_delegators_baking_rewards,
                    final_rewards.payday_delegators_finalization_rewards
                FROM 
                    UNNEST($2::BIGINT[]) WITH ORDINALITY AS pool_owner_data(pool_owner, idx)
                    JOIN UNNEST($3::BYTEA[]) WITH ORDINALITY AS addresses(canonical_address, \
             idx_addr) ON idx = idx_addr
                    LEFT JOIN accounts ON accounts.canonical_address = addresses.canonical_address
                    JOIN UNNEST($4::BIGINT[]) WITH ORDINALITY AS \
             tx_rewards(payday_delegators_transaction_rewards, idx_tx) ON idx = idx_tx
                    JOIN UNNEST($5::BIGINT[]) WITH ORDINALITY AS \
             baker_rewards(payday_delegators_baking_rewards, idx_baker) ON idx = idx_baker
                    JOIN UNNEST($6::BIGINT[]) WITH ORDINALITY AS \
             final_rewards(payday_delegators_finalization_rewards, idx_final) ON idx = idx_final
            )
            GROUP BY pool_owner;
            ",
            &self.block_height,
            &self.delegators_rewards_pool_owners as &[Option<i64>],
            &self.delegators_rewards_canonical_addresses,
            &self.delegators_transaction_rewards,
            &self.delegators_baking_rewards,
            &self.delegators_finalization_rewards
        )
        .execute(tx.as_mut())
        .await
        .context("Failed inserting delegator rewards at payday block")?;

        // Insert the total rewards.
        sqlx::query!(
            "
            UPDATE bakers_payday_pool_rewards AS rewards
            SET 
                payday_total_transaction_rewards = data.payday_total_transaction_rewards,
                payday_total_baking_rewards = data.payday_total_baking_rewards,
                payday_total_finalization_rewards = data.payday_total_finalization_rewards
            FROM (
                SELECT
                    UNNEST($1::BIGINT[]) AS pool_owner,
                    UNNEST($2::BIGINT[]) AS payday_total_transaction_rewards,
                    UNNEST($3::BIGINT[]) AS payday_total_baking_rewards,
                    UNNEST($4::BIGINT[]) AS payday_total_finalization_rewards
            ) AS data
            WHERE rewards.pool_owner IS NOT DISTINCT FROM data.pool_owner
            AND rewards.payday_block_height = $5
            ",
            &self.total_rewards_pool_owners as &[Option<i64>],
            &self.total_transaction_rewards,
            &self.total_baking_rewards,
            &self.total_finalization_rewards,
            &self.block_height,
        )
        .execute(tx.as_mut())
        .await
        .context("Failed inserting total rewards at payday block")?;

        Ok(())
    }
}

/// Insert special transaction outcomes for a particular block.
struct PreparedInsertBlockSpecialTransactionOutcomes {
    /// Height of the block containing these special events.
    block_height: i64,
    /// Index of the outcome within this block in the order they
    /// occur in the block.
    block_outcome_index: Vec<i64>,
    /// The types of the special transaction outcomes in the order they
    /// occur in the block.
    outcome_type: Vec<SpecialEventTypeFilter>,
    /// JSON serializations of `SpecialTransactionOutcome` in the order they
    /// occur in the block.
    outcomes: Vec<serde_json::Value>,
    /// The `SpecialEvents` of a payday block in the order they
    /// occur in the block.
    payday_special_transaction_outcomes: PreparedPaydaySpecialTransactionOutcomes,
}

impl PreparedInsertBlockSpecialTransactionOutcomes {
    fn prepare(
        block_height: AbsoluteBlockHeight,
        events: &[SpecialTransactionOutcome],
    ) -> anyhow::Result<Self> {
        let block_height = block_height.height.try_into()?;
        let mut block_outcome_index = Vec::with_capacity(events.len());
        let mut outcome_type = Vec::with_capacity(events.len());
        let mut outcomes = Vec::with_capacity(events.len());

        let payday_special_transaction_outcomes =
            PreparedPaydaySpecialTransactionOutcomes::prepare(block_height, events)?;

        for (block_index, event) in events.iter().enumerate() {
            let outcome_index = block_index.try_into()?;
            let special_event = SpecialEvent::from_special_transaction_outcome(
                block_height,
                outcome_index,
                event.clone(),
            )?;
            block_outcome_index.push(outcome_index);
            outcome_type.push(event.into());
            outcomes.push(serde_json::to_value(special_event)?);
        }
        Ok(Self {
            block_height,
            block_outcome_index,
            outcome_type,
            outcomes,
            payday_special_transaction_outcomes,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO block_special_transaction_outcomes
                 (block_height, block_outcome_index, outcome_type, outcome)
             SELECT $1, block_outcome_index, outcome_type, outcome
             FROM
                 UNNEST(
                     $2::BIGINT[],
                     $3::special_transaction_outcome_type[],
                     $4::JSONB[]
                 ) AS outcomes(
                     block_outcome_index,
                     outcome_type,
                     outcome
                 )",
            self.block_height,
            &self.block_outcome_index,
            &self.outcome_type as &[SpecialEventTypeFilter],
            &self.outcomes
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_rows(self.outcomes.len().try_into()?)?;

        self.payday_special_transaction_outcomes.save(tx).await?;

        Ok(())
    }
}

/// Represents updates in the database caused by a single special transaction
/// outcome in a block.
enum PreparedSpecialTransactionOutcomeUpdate {
    /// Distribution of various CCD rewards.
    Rewards(Vec<AccountReceivedReward>),
    /// Validator is primed for suspension.
    ValidatorPrimedForSuspension(validator_suspension::PreparedValidatorPrimedForSuspension),
    /// Validator is suspended.
    ValidatorSuspended(validator_suspension::PreparedValidatorSuspension),
}

impl PreparedSpecialTransactionOutcomeUpdate {
    fn prepare(
        event: &SpecialTransactionOutcome,
        block_info: &BlockInfo,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        let results = match &event {
            SpecialTransactionOutcome::BakingRewards {
                baker_rewards,
                ..
            } => {
                let rewards = baker_rewards
                    .iter()
                    .map(|(account_address, amount)| {
                        AccountReceivedReward::prepare(
                            account_address,
                            amount.micro_ccd.try_into()?,
                            block_info.block_height,
                            AccountStatementEntryType::BakerReward,
                            block_info.protocol_version,
                            statistics,
                        )
                    })
                    .collect::<Result<Vec<_>, _>>()?;
                Self::Rewards(rewards)
            }
            SpecialTransactionOutcome::Mint {
                foundation_account,
                mint_platform_development_charge,
                ..
            } => {
                let rewards = vec![AccountReceivedReward::prepare(
                    foundation_account,
                    mint_platform_development_charge.micro_ccd.try_into()?,
                    block_info.block_height,
                    AccountStatementEntryType::FoundationReward,
                    block_info.protocol_version,
                    statistics,
                )?];
                Self::Rewards(rewards)
            }
            SpecialTransactionOutcome::FinalizationRewards {
                finalization_rewards,
                ..
            } => {
                let rewards = finalization_rewards
                    .iter()
                    .map(|(account_address, amount)| {
                        AccountReceivedReward::prepare(
                            account_address,
                            amount.micro_ccd.try_into()?,
                            block_info.block_height,
                            AccountStatementEntryType::FinalizationReward,
                            block_info.protocol_version,
                            statistics,
                        )
                    })
                    .collect::<Result<Vec<_>, _>>()?;
                Self::Rewards(rewards)
            }
            SpecialTransactionOutcome::BlockReward {
                baker,
                foundation_account,
                baker_reward,
                foundation_charge,
                ..
            } => Self::Rewards(vec![
                AccountReceivedReward::prepare(
                    foundation_account,
                    foundation_charge.micro_ccd.try_into()?,
                    block_info.block_height,
                    AccountStatementEntryType::FoundationReward,
                    block_info.protocol_version,
                    statistics,
                )?,
                AccountReceivedReward::prepare(
                    baker,
                    baker_reward.micro_ccd.try_into()?,
                    block_info.block_height,
                    AccountStatementEntryType::BakerReward,
                    block_info.protocol_version,
                    statistics,
                )?,
            ]),
            SpecialTransactionOutcome::PaydayFoundationReward {
                foundation_account,
                development_charge,
            } => Self::Rewards(vec![AccountReceivedReward::prepare(
                foundation_account,
                development_charge.micro_ccd.try_into()?,
                block_info.block_height,
                AccountStatementEntryType::FoundationReward,
                block_info.protocol_version,
                statistics,
            )?]),
            SpecialTransactionOutcome::PaydayAccountReward {
                account,
                transaction_fees,
                baker_reward,
                finalization_reward,
            } => Self::Rewards(vec![
                AccountReceivedReward::prepare(
                    account,
                    transaction_fees.micro_ccd.try_into()?,
                    block_info.block_height,
                    AccountStatementEntryType::TransactionFeeReward,
                    block_info.protocol_version,
                    statistics,
                )?,
                AccountReceivedReward::prepare(
                    account,
                    baker_reward.micro_ccd.try_into()?,
                    block_info.block_height,
                    AccountStatementEntryType::BakerReward,
                    block_info.protocol_version,
                    statistics,
                )?,
                AccountReceivedReward::prepare(
                    account,
                    finalization_reward.micro_ccd.try_into()?,
                    block_info.block_height,
                    AccountStatementEntryType::FinalizationReward,
                    block_info.protocol_version,
                    statistics,
                )?,
            ]),
            // TODO: Support these two types. (Deviates from Old CCDScan)
            SpecialTransactionOutcome::BlockAccrueReward {
                ..
            }
            | SpecialTransactionOutcome::PaydayPoolReward {
                ..
            } => Self::Rewards(Vec::new()),
            SpecialTransactionOutcome::ValidatorSuspended {
                baker_id,
                ..
            } => Self::ValidatorSuspended(
                validator_suspension::PreparedValidatorSuspension::prepare(
                    baker_id,
                    block_info.block_height,
                )?,
            ),
            SpecialTransactionOutcome::ValidatorPrimedForSuspension {
                baker_id,
                ..
            } => Self::ValidatorPrimedForSuspension(
                validator_suspension::PreparedValidatorPrimedForSuspension::prepare(
                    baker_id,
                    block_info.block_height,
                )?,
            ),
        };
        Ok(results)
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        match self {
            Self::Rewards(events) => {
                for event in events {
                    event.save(tx).await?
                }
                Ok(())
            }
            Self::ValidatorPrimedForSuspension(event) => event.save(tx).await,
            Self::ValidatorSuspended(event) => event.save(tx).await,
        }
    }
}

/// Represents the event of an account receiving a reward.
struct AccountReceivedReward {
    /// Update the balance of the account.
    update_account_balance: PreparedUpdateAccountBalance,
    /// Update the stake if restake earnings.
    update_stake:           RestakeEarnings,
}

impl AccountReceivedReward {
    fn prepare(
        account_address: &AccountAddress,
        amount: i64,
        block_height: AbsoluteBlockHeight,
        transaction_type: AccountStatementEntryType,
        protocol_version: ProtocolVersion,
        statistics: &mut Statistics,
    ) -> anyhow::Result<Self> {
        statistics.reward_stats.increment(account_address.get_canonical_address(), amount);
        Ok(Self {
            update_account_balance: PreparedUpdateAccountBalance::prepare(
                account_address,
                amount,
                block_height,
                transaction_type,
            )?,
            update_stake:           RestakeEarnings::prepare(
                account_address,
                amount,
                protocol_version,
            ),
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        self.update_account_balance.save(tx, None).await?;
        self.update_stake.save(tx).await?;
        Ok(())
    }
}

/// Represents the database operation of updating stake for a reward if restake
/// earnings are enabled.
struct RestakeEarnings {
    /// The account address of the receiver of the reward.
    canonical_account_address: CanonicalAccountAddress,
    /// Amount of CCD received as reward.
    amount:                    i64,
    /// Protocol version belonging to the block being processed
    protocol_version:          ProtocolVersion,
}

impl RestakeEarnings {
    fn prepare(
        account_address: &AccountAddress,
        amount: i64,
        protocol_version: ProtocolVersion,
    ) -> Self {
        Self {
            canonical_account_address: account_address.get_canonical_address(),
            amount,
            protocol_version,
        }
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        // Update the account if delegated_restake_earnings is set and is true, meaning
        // the account is delegating.
        sqlx::query!(
            "UPDATE accounts
                SET
                    delegated_stake = CASE
                            WHEN delegated_restake_earnings THEN delegated_stake + $2
                            ELSE delegated_stake
                        END
                WHERE canonical_address = $1
                RETURNING index, delegated_restake_earnings, delegated_target_baker_id",
            self.canonical_account_address.0.as_slice(),
            self.amount
        )
        .fetch_one(tx.as_mut())
        .await?;

        Ok(())
    }
}
