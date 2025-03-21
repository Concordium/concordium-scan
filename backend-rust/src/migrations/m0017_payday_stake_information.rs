//! Adding tables tracking the reward period stake of bakers and delegators for
//! each pool and passive pool.
//! This is then used to compute the APY.

use super::{SchemaVersion, Transaction};
use anyhow::Context;
use concordium_rust_sdk::{common::types::Amount, types::AbsoluteBlockHeight, v2};
use futures::stream::TryStreamExt;
use sqlx::Executor;

/// Resulting database schema version from running this migration.
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::PaydayPoolStake;

#[derive(Debug)]
struct Data {
    block:                   i64,
    bakers:                  Vec<i64>,
    baker_stake:             Vec<i64>,
    delegator_stake:         Vec<i64>,
    passive_delegator_stake: i64,
}
impl Data {
    fn new(block: i64) -> Self {
        Self {
            block,
            bakers: Vec::new(),
            baker_stake: Vec::new(),
            delegator_stake: Vec::new(),
            passive_delegator_stake: 0,
        }
    }
}

/// Run database migration and returns the new database schema version when
/// successful.
pub async fn run(
    tx: &mut Transaction,
    endpoints: &[v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    tx.as_mut()
        .execute(sqlx::raw_sql(include_str!("./m0017-payday-stake-information.sql")))
        .await?;

    let endpoint = endpoints.first().with_context(|| {
        format!("Migration '{}' must be provided access to a Concordium node", NEXT_SCHEMA_VERSION)
    })?;
    let mut client = v2::Client::new(endpoint.clone()).await?;

    let paydays: Vec<i64> = sqlx::query_scalar(
        "SELECT DISTINCT payday_block_height
         FROM bakers_payday_pool_rewards
         ORDER BY payday_block_height ASC",
    )
    .fetch_all(tx.as_mut())
    .await?;

    let mut process = paydays.len();
    tracing::debug!("About to process {} paydays", process);
    for payday_block_height in paydays {
        let block_height = AbsoluteBlockHeight::from(u64::try_from(payday_block_height)?);
        let mut baker_rewards = client.get_bakers_reward_period(block_height).await?.response;
        let mut data = Data::new(payday_block_height);
        // Iterate bakers
        while let Some(reward) = baker_rewards.try_next().await? {
            data.bakers.push(reward.baker.baker_id.id.index.try_into()?);
            data.baker_stake.push(reward.equity_capital.micro_ccd().try_into()?);
            data.delegator_stake.push(reward.delegated_capital.micro_ccd().try_into()?);
        }
        // Iterate delegators for the passive pool
        let mut passive_rewards =
            client.get_passive_delegators_reward_period(block_height).await?.response;
        let mut passive_stake = Amount::zero();
        while let Some(reward) = passive_rewards.try_next().await? {
            passive_stake += reward.stake;
        }
        data.passive_delegator_stake = passive_stake.micro_ccd().try_into()?;
        sqlx::query(
            "INSERT INTO payday_baker_pool_stakes (
                 payday_block,
                 baker,
                 baker_stake,
                 delegators_stake
             ) SELECT $1, * FROM UNNEST(
                     $2::BIGINT[],
                     $3::BIGINT[],
                     $4::BIGINT[]
             ) AS payday_baker(owner, baker_stake, delegators_stake)",
        )
        .bind(data.block)
        .bind(&data.bakers)
        .bind(&data.baker_stake)
        .bind(&data.delegator_stake)
        .execute(tx.as_mut())
        .await?;

        sqlx::query(
            "INSERT INTO payday_passive_pool_stakes (
                 payday_block,
                 delegators_stake
             ) VALUES ($1, $2)",
        )
        .bind(data.block)
        .bind(data.passive_delegator_stake)
        .execute(tx.as_mut())
        .await?;

        process -= 1;
        tracing::debug!("{} paydays remaining", process);
    }

    Ok(NEXT_SCHEMA_VERSION)
}
