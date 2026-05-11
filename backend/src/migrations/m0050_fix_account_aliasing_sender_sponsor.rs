//! Fix NULL sender_index / sponsor_index caused by the account aliasing bug.
//!
//! The indexer looked up the sender by matching the address string directly,
//! which breaks when the transaction was submitted using an account alias.
//! Aliases share the same canonical account but differ in the last 3 bytes of
//! the address, so the lookup missed and left the columns NULL.
//!
//! affected_accounts stores account_index directly (not the address string),
//! so we use that to backfill the missing values.
//!
//! Runs in batches to avoid one giant transaction holding locks and flooding
//! WAL. Each batch commits independently, so the migration is safe to restart.

use sqlx::{Acquire, PgConnection};
use tracing::info;

// Rows updated per commit. 10k keeps each transaction short without making
// the loop run too many iterations on a large dataset.
const BATCH_SIZE: i64 = 10_000;

/// Backfill NULL sender_index / sponsor_index in batches.
///
/// Must be called on a bare connection outside any transaction — each batch
/// opens and commits its own transaction.
pub async fn run(conn: &mut PgConnection) -> anyhow::Result<()> {
    // The PK on affected_accounts was flipped to (account_index,
    // transaction_index) in m0031, so filtering by transaction_index alone
    // needs a separate index. Create it now if it wasn't added before.
    info!("m0050: ensuring index on affected_accounts(transaction_index)");
    sqlx::query(
        "CREATE INDEX IF NOT EXISTS idx_affected_accounts_transaction_index \
         ON affected_accounts (transaction_index)",
    )
    .execute(&mut *conn)
    .await?;

    // Fix sender_index. Pick the lowest account_index from affected_accounts
    // for the transaction — that is the canonical sender.
    info!("m0050: fixing NULL sender_index in batches of {BATCH_SIZE}");
    let mut total_sender: u64 = 0;
    loop {
        let mut tx = conn.begin().await?;
        let rows = sqlx::query(
            r"
            WITH candidates AS (
                SELECT t.index          AS tx_index,
                       MIN(aa.account_index) AS acc_index
                FROM transactions t
                JOIN affected_accounts aa ON aa.transaction_index = t.index
                WHERE t.sender_index IS NULL
                  AND t.type = 'Account'
                GROUP BY t.index
                LIMIT $1
            )
            UPDATE transactions t
               SET sender_index = c.acc_index
              FROM candidates c
             WHERE t.index = c.tx_index
            ",
        )
        .bind(BATCH_SIZE)
        .execute(tx.as_mut())
        .await?
        .rows_affected();

        tx.commit().await?;
        total_sender += rows;

        if rows == 0 {
            break;
        }
        info!("m0050: sender_index: {total_sender} rows fixed so far");
    }
    info!("m0050: sender_index done — {total_sender} rows total");

    // Fix sponsor_index. Same idea but exclude the sender; the remaining
    // affected account is the sponsor.
    info!("m0050: fixing NULL sponsor_index in batches of {BATCH_SIZE}");
    let mut total_sponsor: u64 = 0;
    loop {
        let mut tx = conn.begin().await?;
        let rows = sqlx::query(
            r"
            WITH candidates AS (
                SELECT t.index          AS tx_index,
                       MIN(aa.account_index) AS acc_index
                FROM transactions t
                JOIN affected_accounts aa ON aa.transaction_index = t.index
                WHERE t.sponsor_index IS NULL
                  AND t.type = 'Account'
                  AND t.sponsored_ccd_cost IS NOT NULL
                  AND t.sponsored_ccd_cost > 0
                  AND aa.account_index != t.sender_index
                GROUP BY t.index
                LIMIT $1
            )
            UPDATE transactions t
               SET sponsor_index = c.acc_index
              FROM candidates c
             WHERE t.index = c.tx_index
            ",
        )
        .bind(BATCH_SIZE)
        .execute(tx.as_mut())
        .await?
        .rows_affected();

        tx.commit().await?;
        total_sponsor += rows;

        if rows == 0 {
            break;
        }
        info!("m0050: sponsor_index: {total_sponsor} rows fixed so far");
    }
    info!("m0050: sponsor_index done — {total_sponsor} rows total");

    Ok(())
}
