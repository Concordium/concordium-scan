//! Migration fixing corrupt data in table `bakers` column `staked` and
//! `accounts` column `delegated_stake`.

use super::SchemaVersion;
use anyhow::Context;
use concordium_rust_sdk::{types::AbsoluteBlockHeight, v2};
use tokio_stream::StreamExt;

/// Resulting database schema version from running this migration.
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::FixStakedAmounts;

/// Run database migration and returns the new database schema version when
/// successful.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    // Get the last processed block height.
    let block_height: Option<i64> =
        sqlx::query_scalar("SELECT height FROM blocks ORDER BY height DESC LIMIT 1")
            .fetch_optional(tx.as_mut())
            .await?;
    let Some(block_height) = block_height else {
        // If result is None, we did not index any block yet and we are done migrating.
        return Ok(NEXT_SCHEMA_VERSION);
    };
    let block_height = AbsoluteBlockHeight::from(u64::try_from(block_height)?);
    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        NEXT_SCHEMA_VERSION
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;
    let mut bakers = client.get_baker_list(block_height).await?.response;

    let mut baker_ids = Vec::new();
    let mut baker_stakes = Vec::new();
    let mut delegator_addresses = Vec::new();
    let mut delegator_stakes = Vec::new();
    while let Some(baker) = bakers.try_next().await? {
        let account_info = client
            .get_account_info(&v2::AccountIdentifier::Index(baker.id), block_height)
            .await?
            .response;

        let baker_id: i64 = baker.id.index.try_into()?;
        let staked = account_info
            .account_stake
            .context("Expected account to be baker")?
            .staked_amount();
        baker_ids.push(baker_id);
        baker_stakes.push(i64::try_from(staked.micro_ccd())?);

        let mut pool_delegators = client
            .get_pool_delegators(block_height, baker)
            .await?
            .response;
        while let Some(delegator) = pool_delegators.try_next().await? {
            delegator_addresses.push(delegator.account.to_string());
            delegator_stakes.push(i64::try_from(delegator.stake.micro_ccd())?)
        }
    }

    sqlx::query(
        "UPDATE bakers
                 SET staked = input.staked
             FROM (SELECT
                 UNNEST($1::BIGINT[]) AS id,
                 UNNEST($2::BIGINT[]) AS staked) AS input
             WHERE bakers.id = input.id",
    )
    .bind(baker_ids.as_slice())
    .bind(baker_stakes.as_slice())
    .execute(tx.as_mut())
    .await?;

    sqlx::query(
        "UPDATE accounts
                 SET delegated_stake = input.staked
             FROM (SELECT
                 UNNEST($1::VARCHAR[]) AS address,
                 UNNEST($2::BIGINT[]) AS staked) AS input
             WHERE accounts.address = input.address",
    )
    .bind(delegator_addresses.as_slice())
    .bind(delegator_stakes.as_slice())
    .execute(tx.as_mut())
    .await?;

    Ok(NEXT_SCHEMA_VERSION)
}
