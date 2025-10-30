//! Migration adding (or updating) the values to the database:
//! - `epoch_duration`
//! - `reward_period_length`
//! - `capital_bound`
//! - `leverage_bound_numerator`
//! - `leverage_bound_denominator`

use super::SchemaVersion;
use anyhow::Context;
use concordium_rust_sdk::{
    types::PartsPerHundredThousands,
    v2::{self, BlockIdentifier},
};
use sqlx::Executor;

/// Run database migration to fill the new columns `capital_bound` and
/// `leverage_bound`.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
    next_schema_version: SchemaVersion,
) -> anyhow::Result<SchemaVersion> {
    // Run database migration first to add the new columns.
    tx.as_mut()
        .execute(sqlx::raw_sql(include_str!(
            "./m0010_fill_capital_bound_and_leverage_bound.sql"
        )))
        .await?;

    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        next_schema_version
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;

    // Get the current `epoch_duration` value.
    let current_epoch_duration = client
        .get_consensus_info()
        .await?
        .epoch_duration
        .num_milliseconds();

    // Get the current `reward_period_length`, `capital_bound` and `leverage_bound`
    // value.
    let current_chain_parmeters = client
        .get_block_chain_parameters(BlockIdentifier::LastFinal)
        .await?
        .response;
    let current_reward_period_length = current_chain_parmeters
        .reward_period_length
        .context(
            "Expect the node to have caught up enough for the `reward_period_length` value to be available.",
        )?;
    let staking_parameters = current_chain_parmeters.staking_parameters;
    let capital_bound = staking_parameters.capital_bound.context(
        "Expect the node to have caught up enough for the `capital_bound` value to be available.",
    )?;
    let leverage_bound = staking_parameters.leverage_bound.context(
        "Expect the node to have caught up enough for the `leverage_bound` value to be available.",
    )?;

    let capital_bound = i64::from(u32::from(PartsPerHundredThousands::from(
        capital_bound.bound,
    )));

    sqlx::query(
        "INSERT INTO current_chain_parameters (
                id, 
                epoch_duration, 
                reward_period_length, 
                capital_bound, 
                leverage_bound_numerator, 
                leverage_bound_denominator
            ) VALUES (true, $1, $2, $3, $4, $5)
            ON CONFLICT (id) 
            DO UPDATE SET 
                epoch_duration = EXCLUDED.epoch_duration,
                reward_period_length = EXCLUDED.reward_period_length,
                capital_bound = EXCLUDED.capital_bound,
                leverage_bound_numerator = EXCLUDED.leverage_bound_numerator,
                leverage_bound_denominator = EXCLUDED.leverage_bound_denominator;
            ",
    )
    .bind(current_epoch_duration)
    .bind(i64::try_from(
        current_reward_period_length.reward_period_epochs().epoch,
    )?)
    .bind(capital_bound)
    .bind(i64::try_from(leverage_bound.numerator)?)
    .bind(i64::try_from(leverage_bound.denominator)?)
    .execute(tx.as_mut())
    .await?;

    Ok(next_schema_version)
}
