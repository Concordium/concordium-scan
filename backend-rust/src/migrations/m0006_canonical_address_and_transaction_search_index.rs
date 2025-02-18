//! Migration fixing corrupt data in:
//! - `bakers` table due to `DelegationEvent::RemoveBaker` event not being
//!   handled.
//! - delegators in `accounts` table due to delegators not being moved to the
//!   passive pool as their target validator pool got closed or removed (found
//!   in the matching `.sql` file).

use super::{SchemaVersion, Transaction};
use sqlx::Executor;
use std::str::FromStr;
use tokio_stream::StreamExt;

/// Run database migration and returns the new database schema version when
/// successful.
pub async fn run(tx: &mut Transaction) -> anyhow::Result<SchemaVersion> {
    tx.as_mut()
        .execute(sqlx::raw_sql(include_str!(
            "m0006-canonical-address-and-transaction-search-index.sql"
        )))
        .await?;

    let mut update_queries = {
        let mut accounts = sqlx::query("SELECT index, address FROM accounts").fetch(tx.as_mut());
        let mut update_queries = Vec::new();
        while let Some(row) = accounts.try_next().await? {
            let account_address: String = sqlx::Row::try_get(&row, "address")?;
            let index: i64 = sqlx::Row::try_get(&row, "index")?;
            let account_address =
                concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(
                    &account_address,
                )?;
            let canonical_address = account_address.get_canonical_address().0.as_slice().to_vec();
            if canonical_address.len() != 29 {
                println!("size: {}", canonical_address.len());
            }
            update_queries.push(
                sqlx::query("UPDATE accounts SET canonical_address = $1 WHERE index = $2")
                    .bind(canonical_address)
                    .bind(index),
            )
        }
        update_queries
    };

    for query in update_queries {
        query.execute(tx.as_mut()).await?;
    }

    Ok(SchemaVersion::AccountBaseAddress)
}
