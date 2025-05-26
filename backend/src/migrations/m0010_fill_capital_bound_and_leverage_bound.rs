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
    v2::{self, BlockIdentifier, ChainParameters},
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
        .execute(sqlx::raw_sql(include_str!("./m0010_fill_capital_bound_and_leverage_bound.sql")))
        .await?;

    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        next_schema_version
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;

    // Get the current `epoch_duration` value.
    let current_epoch_duration =
        client.get_consensus_info().await?.epoch_duration.num_milliseconds();

    // Get the current `reward_period_length`, `capital_bound` and `leverage_bound`
    // value.
    let current_chain_parmeters =
        client.get_block_chain_parameters(BlockIdentifier::LastFinal).await?.response;
    let (current_reward_period_length, capital_bound, leverage_bound) =
        match current_chain_parmeters {
            ChainParameters::V3(chain_parameters_v3) => (
                chain_parameters_v3.time_parameters.reward_period_length,
                chain_parameters_v3.pool_parameters.capital_bound,
                chain_parameters_v3.pool_parameters.leverage_bound,
            ),
            ChainParameters::V2(chain_parameters_v2) => (
                chain_parameters_v2.time_parameters.reward_period_length,
                chain_parameters_v2.pool_parameters.capital_bound,
                chain_parameters_v2.pool_parameters.leverage_bound,
            ),
            ChainParameters::V1(chain_parameters_v1) => (
                chain_parameters_v1.time_parameters.reward_period_length,
                chain_parameters_v1.pool_parameters.capital_bound,
                chain_parameters_v1.pool_parameters.leverage_bound,
            ),
            ChainParameters::V0(_) => unimplemented!(
                "Expect the node to have caught up enough for the `reward_period_length`, \
                 `capital_bound` and `leverage_bound` values to be available."
            ),
        };

    let capital_bound = i64::from(u32::from(PartsPerHundredThousands::from(capital_bound.bound)));

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
    .bind(i64::try_from(current_reward_period_length.reward_period_epochs().epoch)?)
    .bind(capital_bound)
    .bind(i64::try_from(leverage_bound.numerator)?)
    .bind(i64::try_from(leverage_bound.denominator)?)
    .execute(tx.as_mut())
    .await?;

    Ok(next_schema_version)
}
