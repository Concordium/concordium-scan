//! Migration reindexing credential deployments.
//!
//! Credential deployments only stored the event `AccountCreation` and were
//! missing the `CredentialDeployment` events. Additionally a non-zero CCD cost
//! are stored computed from the energy, where this transaction is to be treated
//! as a special case resulting in no fee costs.
//!
//! This migration iterates the relevant transactions in the database and
//! fetches a Concordium node for the Credential Registration ID to include in
//! the events and updates the stored CCD fee cost to `0`.

use super::SchemaVersion;
use crate::transaction_event::{credentials, Event};
use anyhow::Context;
use concordium_rust_sdk::{base::hashes::TransactionHash, types::BlockItemSummaryDetails, v2};

/// Resulting database schema version from running this migration.
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::ReindexCredentialDeployment;

/// Migration reindexing credential deployments.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    let deployments: Vec<String> = sqlx::query_scalar(
        "SELECT hash
         FROM transactions
         WHERE type = 'CredentialDeployment'
         ORDER BY index ASC",
    )
    .fetch_all(tx.as_mut())
    .await?;
    if deployments.is_empty() {
        // No credential deployments processed yet, meaning no data to migrate.
        return Ok(NEXT_SCHEMA_VERSION);
    }
    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        NEXT_SCHEMA_VERSION
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;
    for chunk in deployments.chunks(1000) {
        let mut hashes = Vec::with_capacity(chunk.len());
        let mut events = Vec::with_capacity(chunk.len());
        for hash_str in chunk {
            let hash: TransactionHash = hash_str.parse()?;
            let status = client.get_block_item_status(&hash).await?;

            let (_, summary) = status
                .is_finalized()
                .context("Unexpected non-finalized transaction in the database")?;
            let BlockItemSummaryDetails::AccountCreation(details) =
                &summary.details.as_ref().known_or_err()?
            else {
                anyhow::bail!(
                    "Unexpected transaction type during migration of credential deployments"
                );
            };
            let updated_events = serde_json::to_value(vec![
                Event::CredentialDeployed(credentials::CredentialDeployed {
                    reg_id: details.reg_id.to_string(),
                    account_address: details.address.into(),
                }),
                Event::AccountCreated(credentials::AccountCreated {
                    account_address: details.address.into(),
                }),
            ])?;
            hashes.push(hash_str.clone());
            events.push(updated_events);
        }
        sqlx::query(
            "UPDATE transactions SET
                 ccd_cost = 0,
                 events = updated.events
             FROM UNNEST(
                 $1::TEXT[],
                 $2::JSONB[]
             ) AS updated(hash, events)
             WHERE transactions.hash = updated.hash",
        )
        .bind(&hashes)
        .bind(&events)
        .execute(tx.as_mut())
        .await?;
    }
    Ok(NEXT_SCHEMA_VERSION)
}
