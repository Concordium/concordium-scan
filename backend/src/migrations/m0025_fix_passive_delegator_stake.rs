//! Migration reindexing the current stake for delegators to the passive pool.
//!
//! The stakes for these delegators got offset by a bug in migration 0013, which
//! did not update the passive delegators properly. This was fixed again in
//! migration m0022, but the passive delegators with restake_earnings enabled
//! missed their increase in stake due to this.
use super::SchemaVersion;
use anyhow::Context;
use concordium_rust_sdk::v2::{self, BlockIdentifier};
use futures::{StreamExt, TryStreamExt};

/// Resulting database schema version from running this migration.
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::FixPassiveDelegatorsStake;

/// Migration reindexing the current stake for delegators to the passive pool.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    let latest_height: Option<i64> =
        sqlx::query_scalar("SELECT height FROM blocks ORDER BY height DESC LIMIT 1")
            .fetch_optional(tx.as_mut())
            .await?;
    let Some(latest_height) = latest_height else {
        return Ok(NEXT_SCHEMA_VERSION);
    };
    let latest_block = BlockIdentifier::AbsoluteHeight(u64::try_from(latest_height)?.into());
    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        NEXT_SCHEMA_VERSION
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;
    let (addresses, stakes): (Vec<_>, Vec<_>) = client
        .get_passive_delegators(latest_block)
        .await?
        .response
        .map(|result| {
            let delegator_info = result?;
            anyhow::Ok((
                delegator_info.account.to_string(),
                i64::try_from(delegator_info.stake.micro_ccd())?,
            ))
        })
        .try_collect()
        .await?;
    sqlx::query(
        "UPDATE accounts
             SET delegated_stake = passive_delegator.stake
         FROM UNNEST(
             $1::TEXT[],
             $2::BIGINT[]
         ) AS passive_delegator(address, stake)
         WHERE
             delegated_restake_earnings IS NOT NULL
             AND delegated_target_baker_id IS NULL
             AND accounts.address = passive_delegator.address",
    )
    .bind(&addresses)
    .bind(&stakes)
    .execute(tx.as_mut())
    .await?;

    Ok(NEXT_SCHEMA_VERSION)
}
