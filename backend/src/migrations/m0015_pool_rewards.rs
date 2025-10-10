//! Adding table tracking rewards paid to delegators, passive delegators, and
//! baker accounts at past payday blocks and populate the table.
use super::SchemaVersion;
use anyhow::Context;
use concordium_rust_sdk::{
    types::{AbsoluteBlockHeight, SpecialTransactionOutcome},
    v2::{self, BlockIdentifier},
};
use futures::stream::TryStreamExt;
use sqlx::Executor;

/// Run database migration to fill in historical payday blocks into the new
/// table.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
    next_schema_version: SchemaVersion,
) -> anyhow::Result<SchemaVersion> {
    // Run database migration first to add the new table.
    tx.as_mut()
        .execute(sqlx::raw_sql(include_str!("./m0015-pool-rewards.sql")))
        .await?;

    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        next_schema_version
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;

    let payday_blocks: Vec<i64> = sqlx::query_scalar!(
        "
            SELECT block_height
            FROM block_special_transaction_outcomes
            WHERE outcome_type IN ('PaydayFoundationReward', 'PaydayAccountReward', \
         'PaydayPoolReward')
            GROUP BY block_height
        "
    )
    .fetch_all(tx.as_mut())
    .await?;

    for payday_block_height in payday_blocks {
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

        for event in &special_items {
            match event.as_ref().known_or_err()? {
                SpecialTransactionOutcome::PaydayPoolReward {
                    pool_owner,
                    transaction_fees,
                    baker_reward,
                    finalization_reward,
                } => {
                    let pool_owner = pool_owner
                        .map(|baker_id| baker_id.id.index.try_into())
                        .transpose()?;

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
            payday_block_height,
            &delegators_rewards_pool_owners as &[Option<i64>],
            &delegators_rewards_canonical_addresses,
            &delegators_transaction_rewards,
            &delegators_baking_rewards,
            &delegators_finalization_rewards
        )
        .execute(tx.as_mut())
        .await
        .context(format!(
            "Failed inserting delegator rewards at payday block {}",
            payday_block_height
        ))?;

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
            &total_rewards_pool_owners as &[Option<i64>],
            &total_transaction_rewards,
            &total_baking_rewards,
            &total_finalization_rewards,
            payday_block_height,
        )
        .execute(tx.as_mut())
        .await
        .context(format!(
            "Failed inserting total rewards at payday block {}",
            payday_block_height
        ))?;
    }

    Ok(next_schema_version)
}
