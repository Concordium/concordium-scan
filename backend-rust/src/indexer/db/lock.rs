//! This module contains the indexer PostgreSQL advisory lock, used to ensure
//! only one instance of the indexer is active at any time.

use anyhow::Context;
use sqlx::{postgres::PgAdvisoryLock, PgConnection};
use tracing::info;

/// Identifier for the PostgreSQL advisory lock for indexing.
const ADVISORY_LOCK_INDEXER: &str = "ccdscan-indexing";

/// Acquire the indexer lock for the provided connection.
/// The lock will be released once the connection is shutdown.
pub async fn acquire_indexer_lock(db_connection: &mut PgConnection) -> anyhow::Result<()> {
    info!("Requesting the indexer advisory lock");
    PgAdvisoryLock::new(ADVISORY_LOCK_INDEXER)
        .acquire(db_connection.as_mut())
        .await
        .context("Failed to acquire the indexer advisory lock")?
        .leak();
    Ok(())
}
