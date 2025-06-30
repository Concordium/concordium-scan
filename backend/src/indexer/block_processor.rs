//! Contains the block processing logic.
//!
//! This step run sequential for batches of preprocessed blocks in same order as
//! found in the chain. This step has access to a database connection and is
//! responsible for updating the data in the database.

use super::block::PreparedBlock;
use crate::indexer::acquire_indexer_lock;
use anyhow::Context;
use chrono::{DateTime, Utc};
use concordium_rust_sdk::indexer::{async_trait, ProcessEvent};
use prometheus_client::{
    metrics::{
        counter::Counter,
        histogram::{self, Histogram},
    },
    registry::Registry,
};
use sqlx::{postgres::PgConnectOptions, Connection as _, PgConnection};
use std::{mem, time::Duration};
use tokio::time::Instant;
use tracing::info;

/// Type implementing the `ProcessEvent` handling the insertion of prepared
/// blocks.
pub struct BlockProcessor {
    /// Options for the database connection.
    db_connect_options:          PgConnectOptions,
    /// Timeout for acquiring the indexer advisory lock after connecting to the
    /// database.
    db_indexer_lock_timeout:     Duration,
    /// Current database connection.
    db_connection:               PgConnection,
    /// Histogram collecting batch size
    batch_size:                  Histogram,
    /// Metric counting the total number of failed attempts to process
    /// blocks.
    processing_failures:         Counter,
    /// Histogram collecting the time it took to process a block.
    processing_duration_seconds: Histogram,
    /// Max number of acceptable successive failures before shutting down the
    /// service.
    max_successive_failures:     u32,
    /// Starting context which is tracked across processing blocks.
    current_context:             BlockProcessingContext,
}
impl BlockProcessor {
    /// Construct the block processor by loading the initial state from the
    /// database. This assumes at least the genesis block is in the
    /// database.
    pub async fn new(
        db_connect_options: PgConnectOptions,
        mut db_connection: PgConnection,
        database_indexer_lock_timeout: Duration,
        max_successive_failures: u32,
        registry: &mut Registry,
    ) -> anyhow::Result<Self> {
        let last_finalized_block = sqlx::query!(
            "
            SELECT
                hash,
                cumulative_finalization_time
            FROM blocks
            WHERE finalization_time IS NOT NULL
            ORDER BY height DESC
            LIMIT 1
            "
        )
        .fetch_one(db_connection.as_mut())
        .await
        .context("Failed to query data for save context")?;

        let last_block = sqlx::query!(
            "
            SELECT
                slot_time,
                cumulative_num_txs
            FROM blocks
            ORDER BY height DESC
            LIMIT 1
            "
        )
        .fetch_one(db_connection.as_mut())
        .await
        .context("Failed to query data for save context")?;
        let starting_context = BlockProcessingContext {
            last_finalized_hash:               last_finalized_block.hash,
            last_block_slot_time:              last_block.slot_time,
            last_cumulative_num_txs:           last_block.cumulative_num_txs,
            last_cumulative_finalization_time: last_finalized_block
                .cumulative_finalization_time
                .unwrap_or(0),
        };

        let processing_failures = Counter::default();
        registry.register(
            "processing_failures",
            "Number of blocks save to the database",
            processing_failures.clone(),
        );
        let processing_duration_seconds =
            Histogram::new(histogram::exponential_buckets(0.01, 2.0, 10));
        registry.register(
            "processing_duration_seconds",
            "Time taken for processing a block",
            processing_duration_seconds.clone(),
        );
        let batch_size = Histogram::new(histogram::linear_buckets(1.0, 1.0, 10));
        registry.register("batch_size", "Batch sizes", batch_size.clone());

        Ok(Self {
            db_connect_options,
            db_connection,
            db_indexer_lock_timeout: database_indexer_lock_timeout,
            current_context: starting_context,
            batch_size,
            processing_failures,
            processing_duration_seconds,
            max_successive_failures,
        })
    }
}

#[async_trait]
impl ProcessEvent for BlockProcessor {
    /// The type of events that are to be processed. Typically this will be all
    /// of the transactions of interest for a single block."]
    type Data = Vec<PreparedBlock>;
    /// A description returned by the [`process`](ProcessEvent::process) method.
    /// This message is logged by the [`ProcessorConfig`] and is intended to
    /// describe the data that was just processed.
    type Description = String;
    /// An error that can be signalled.
    type Error = anyhow::Error;

    /// Process a single item. This should work atomically in the sense that
    /// either the entire `data` is processed or none of it is in case of an
    /// error. This property is relied upon by the [`ProcessorConfig`] to retry
    /// failed attempts.
    async fn process(&mut self, batch: &Self::Data) -> Result<Self::Description, Self::Error> {
        let start_time = Instant::now();
        let mut out = format!("Processed {} blocks:", batch.len());
        // Clone the context, to avoid mutating the current context until we are certain
        // nothing fails.
        let mut new_context = self.current_context.clone();

        let mut tx =
            self.db_connection.begin().await.context("Failed to create SQL transaction")?;
        PreparedBlock::batch_save(batch, &mut new_context, &mut tx).await?;
        for block in batch {
            block.process_block_content(&mut tx).await?;
            out.push_str(format!("\n- {}:{}", block.height, block.hash).as_str());
        }
        process_release_schedules(new_context.last_block_slot_time, &mut tx)
            .await
            .context("Processing scheduled releases")?;
        tx.commit().await.context("Failed to commit SQL transaction")?;
        self.batch_size.observe(batch.len() as f64);
        let duration = start_time.elapsed();
        self.processing_duration_seconds.observe(duration.as_secs_f64());
        self.current_context = new_context;
        Ok(out)
    }

    /// The `on_failure` method is invoked by the [`ProcessorConfig`] when it
    /// fails to process an event. It is meant to retry to recreate the
    /// resources, such as a database connection, that might have been
    /// dropped. The return value should signal if the handler process
    /// should continue (`true`) or not.
    ///
    /// The function takes the `error` that occurred at the latest
    /// [`process`](Self::process) call that just failed, and the number of
    /// attempts of calling `process` that failed.
    async fn on_failure(
        &mut self,
        error: Self::Error,
        successive_failures: u32,
    ) -> Result<bool, Self::Error> {
        info!("Failed processing {} times in row: \n{:?}", successive_failures, error);
        self.processing_failures.inc();

        // Create the new Database connection here
        let new_db_connection = PgConnection::connect_with(&self.db_connect_options)
            .await
            .context("Failed to establish new connection to the database")?;

        // replace the active db_connection with the new connection, then return the old
        // connection reference so that it can be closed gracefully
        let old_db_connection = std::mem::replace(&mut self.db_connection, new_db_connection);

        // Gracefully close the old connection to the database
        if let Err(e) = old_db_connection.close().await {
            tracing::warn!("Failed to close old database connection: {:?}", e);
        }

        // acquire the DB lock for our new connection
        tokio::time::timeout(
            self.db_indexer_lock_timeout,
            acquire_indexer_lock(self.db_connection.as_mut()),
        )
        .await
        .context(
            "Acquire indexer lock timed out, another instance of ccdscan-indexer might already be \
             running",
        )??;

        Ok(self.max_successive_failures >= successive_failures)
    }
}

/// Process schedule releases based on the slot time of the last processed
/// block.
async fn process_release_schedules(
    last_block_slot_time: DateTime<Utc>,
    tx: &mut sqlx::PgTransaction<'_>,
) -> anyhow::Result<()> {
    sqlx::query!(
        "DELETE FROM scheduled_releases
         WHERE release_time <= $1",
        last_block_slot_time
    )
    .execute(tx.as_mut())
    .await?;
    Ok(())
}

#[derive(Clone)]
pub struct BlockProcessingContext {
    /// The last finalized block hash according to the latest indexed block.
    /// This is used when computing the finalization time.
    pub last_finalized_hash:               String,
    /// The slot time of the last processed block.
    /// This is used when computing the block time.
    pub last_block_slot_time:              DateTime<Utc>,
    /// The value of cumulative_num_txs from the last block.
    /// This, along with the number of transactions in the current block,
    /// is used to calculate the next cumulative_num_txs.
    pub last_cumulative_num_txs:           i64,
    /// The cumulative_finalization_time in milliseconds of the last finalized
    /// block. This is used to efficiently update the
    /// cumulative_finalization_time of newly finalized blocks.
    pub last_cumulative_finalization_time: i64,
}
