//! Migration fixing corrupt data in:
//! - `bakers` table due to `DelegationEvent::RemoveBaker` event not being
//!   handled.
//! - delegators in `accounts` table due to delegators not being moved to the
//!   passive pool as their target validator pool got closed or removed (found
//!   in the matching `.sql` file).

use super::SchemaVersion;
use anyhow::Context;
use concordium_rust_sdk::{types::AbsoluteBlockHeight, v2};
use sqlx::Executor;
use std::collections::BTreeSet;
use tokio_stream::StreamExt;
use tracing::debug;

/// Run database migration and returns the new database schema version when
/// successful.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    // Fix data from the unhandled `DelegationEvent::RemoveBaker` event, which is
    // when a baker switches to delegation directly.
    // For a partial fix we remove bakers/validators, which are also delegating
    // currently.
    sqlx::query(
        "DELETE FROM bakers
    WHERE EXISTS (SELECT FROM accounts WHERE bakers.id = accounts.index AND delegated_stake > 0)",
    )
    .execute(tx.as_mut())
    .await?;
    // This leaves the bakers, which switched directly to delegation and then
    // stopped delegation again. Here we have to rely on a Concordium node for the
    // state, so we fetch the current processed block height and query the
    // bakers at this point.
    let result: Option<i64> =
        sqlx::query_scalar("SELECT height FROM blocks ORDER BY height DESC LIMIT 1")
            .fetch_optional(tx.as_mut())
            .await?;
    // If result is None, we did not index any block yet and can skip this step.
    if let Some(current_block_height) = result {
        let current_block_height = AbsoluteBlockHeight::from(u64::try_from(current_block_height)?);
        // Collect the bakers currently stored.
        let mut stored_baker_set = {
            let mut stored_bakers = sqlx::query("SELECT id FROM bakers").fetch(tx.as_mut());
            let mut set = BTreeSet::new();
            while let Some(row) = stored_bakers.try_next().await? {
                let baker_id: i64 = sqlx::Row::try_get(&row, 0)?;
                set.insert(baker_id);
            }
            set
        };
        // Subtract the actual bakers at this block height according to the node.
        let endpoint = endpoints.first().context(format!(
            "Migration '{}' must be provided access to a Concordium node",
            SchemaVersion::FixDanglingDelegators
        ))?;
        let mut client = v2::Client::new(endpoint.clone()).await?;
        let mut actual_bakers = client.get_baker_list(current_block_height).await?.response;
        while let Some(id) = actual_bakers.try_next().await? {
            let baker_id: i64 = id.id.index.try_into()?;
            stored_baker_set.remove(&baker_id);
        }
        // Remaining bakers in the collection must be due to data corruption, so we
        // remove these.
        let invalid_bakers: Vec<_> = stored_baker_set.into_iter().collect();
        debug!("Removing the following invalid bakers {:?}", invalid_bakers);
        sqlx::query("DELETE FROM bakers WHERE id = ANY($1)")
            .bind(invalid_bakers.as_slice())
            .execute(tx.as_mut())
            .await?;
    };

    // Run remaining migration as SQL.
    tx.as_mut().execute(sqlx::raw_sql(include_str!("./m0005-fix-dangling-delegators.sql"))).await?;

    Ok(SchemaVersion::FixDanglingDelegators)
}
