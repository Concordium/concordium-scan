//! Adding table tracking rewards paid to delegators, passive delegators, and
//! baker accounts at past payday blocks and populate the table.
use super::{SchemaVersion, Transaction};
use crate::transaction_event::baker::PaydayPoolRewards;
use anyhow::Context;
use concordium_rust_sdk::{
    types::{AbsoluteBlockHeight, SpecialTransactionOutcome},
    v2::{self, BlockIdentifier},
};
use futures::stream::TryStreamExt;
use sqlx::{Executor, Row};
use std::collections::HashMap;

/// Run database migration to fill in historical payday blocks into the new
/// table.
pub async fn run(
    tx: &mut Transaction,
    endpoints: &[v2::Endpoint],
    next_schema_version: SchemaVersion,
) -> anyhow::Result<SchemaVersion> {
    // Run database migration first to add the new table.
    tx.as_mut().execute(sqlx::raw_sql(include_str!("./m0014-pool-rewards.sql"))).await?;

    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        next_schema_version
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;

    let rows = sqlx::query(
        "
                SELECT block_height
                FROM block_special_transaction_outcomes
                WHERE outcome_type IN ('PaydayFoundationReward', 'PaydayAccountReward', 'PaydayPoolReward')
                GROUP BY block_height;
            ",
    )
    .fetch_all(tx.as_mut()) // Fetch all rows at once
    .await?;

    for row in rows {
        let payday_block_height: i64 = row.get("block_height");

        let special_items = client
            .get_block_special_events(BlockIdentifier::AbsoluteHeight(AbsoluteBlockHeight {
                height: payday_block_height.try_into()?,
            }))
            .await?
            .response
            .try_collect::<Vec<_>>()
            .await?;

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
        // `PaydayPoolReward` and `PaydayAccountReward` events only occure in payday
        // blocks starting in protocol 4.
        //
        // The mapping `acc_payday_pool_rewards` associates the `pool_owner` (a
        // `baker_id` or `None`) to its `PaydayPoolRewards`. The `None` value
        // tracks passive deleagation.
        let mut acc_payday_pool_rewards: HashMap<Option<u64>, PaydayPoolRewards> = HashMap::new();
        let mut last_pool_owner: Option<Option<u64>> = None;

        for event in &special_items {
            match event {
                SpecialTransactionOutcome::PaydayPoolReward {
                    pool_owner,
                    transaction_fees,
                    baker_reward,
                    finalization_reward,
                } => {
                    // The pool owner is `None` only if the pool rewards are for the passive
                    // delegators. There is only one event per
                    // payday block that rewards the passive delegators.
                    let last = pool_owner.as_ref().map(|baker_id| baker_id.id.into());

                    let acc_payday_pool_rewards =
                        acc_payday_pool_rewards.entry(last).or_insert(PaydayPoolRewards::new());

                    acc_payday_pool_rewards.transaction_fees.total_amount =
                        transaction_fees.micro_ccd;
                    acc_payday_pool_rewards.block_baking.total_amount = baker_reward.micro_ccd;
                    acc_payday_pool_rewards.block_finalization.total_amount =
                        finalization_reward.micro_ccd;

                    last_pool_owner = Some(last);
                }
                SpecialTransactionOutcome::PaydayAccountReward {
                    transaction_fees,
                    baker_reward,
                    finalization_reward,
                    account,
                } => {
                    // Collect all rewards from the delegators and associate the rewards to their
                    // baker pools.
                    if let Some(pool_owner) = last_pool_owner {
                        if let Some(baker_id) = pool_owner {
                            let account_index = sqlx::query_scalar!(
                                "SELECT index
                                    FROM accounts
                                    WHERE address = $1",
                                account.to_string()
                            )
                            .fetch_one(tx.as_mut())
                            .await?;

                            // Don't record the rewards if they are associated to the baker itself
                            // (not a delegator).
                            if account_index == baker_id as i64 {
                                continue;
                            }
                        }

                        let acc_payday_pool_rewards = acc_payday_pool_rewards
                            .entry(pool_owner)
                            .or_insert(PaydayPoolRewards::new());

                        acc_payday_pool_rewards.transaction_fees.delegators_amount +=
                            transaction_fees.micro_ccd;
                        acc_payday_pool_rewards.block_baking.delegators_amount +=
                            baker_reward.micro_ccd;
                        acc_payday_pool_rewards.block_finalization.delegators_amount +=
                            finalization_reward.micro_ccd;
                    }
                }
                _ => {}
            }
        }

        let pool_owners: Vec<Option<i64>> = acc_payday_pool_rewards
            .keys()
            .map(|&r| r.map(i64::try_from).transpose())
            .collect::<Result<_, _>>()?;

        let total_transaction_rewards: Vec<i64> = acc_payday_pool_rewards
            .values()
            .map(|r| r.transaction_fees.total_amount.try_into())
            .collect::<Result<_, _>>()?;

        let delegators_transaction_rewards: Vec<i64> = acc_payday_pool_rewards
            .values()
            .map(|r| r.transaction_fees.delegators_amount.try_into())
            .collect::<Result<_, _>>()?;
        let total_baking_rewards: Vec<i64> = acc_payday_pool_rewards
            .values()
            .map(|r| r.block_baking.total_amount.try_into())
            .collect::<Result<_, _>>()?;

        let delegators_baking_rewards: Vec<i64> = acc_payday_pool_rewards
            .values()
            .map(|r| r.block_baking.delegators_amount.try_into())
            .collect::<Result<_, _>>()?;

        let total_finalization_rewards: Vec<i64> = acc_payday_pool_rewards
            .values()
            .map(|r| r.block_finalization.total_amount.try_into())
            .collect::<Result<_, _>>()?;

        let delegators_finalization_rewards: Vec<i64> = acc_payday_pool_rewards
            .values()
            .map(|r| r.block_finalization.delegators_amount.try_into())
            .collect::<Result<_, _>>()?;

        sqlx::query!(
            "INSERT INTO bakers_payday_pool_rewards (
                    payday_block_height,
                    pool_owner,
                    payday_total_transaction_rewards,
                    payday_delegators_transaction_rewards,
                    payday_total_baking_rewards,
                    payday_delegators_baking_rewards,
                    payday_total_finalization_rewards,
                    payday_delegators_finalization_rewards
                )
                SELECT
                    $1 AS payday_block_height,
                    UNNEST($2::BIGINT[]) AS pool_owner,
                    UNNEST($3::BIGINT[]) AS payday_total_transaction_rewards,
                    UNNEST($4::BIGINT[]) AS payday_delegators_transaction_rewards,
                    UNNEST($5::BIGINT[]) AS payday_total_baking_rewards,
                    UNNEST($6::BIGINT[]) AS payday_delegators_baking_rewards,
                    UNNEST($7::BIGINT[]) AS payday_total_finalization_rewards,
                    UNNEST($8::BIGINT[]) AS payday_delegators_finalization_rewards",
            &payday_block_height,
            &pool_owners as &[Option<i64>],
            &total_transaction_rewards,
            &delegators_transaction_rewards,
            &total_baking_rewards,
            &delegators_baking_rewards,
            &total_finalization_rewards,
            &delegators_finalization_rewards,
        )
        .execute(tx.as_mut())
        .await?;
    }

    Ok(next_schema_version)
}
