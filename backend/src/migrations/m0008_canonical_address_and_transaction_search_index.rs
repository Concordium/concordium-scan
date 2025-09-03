//! Deriving account canonical address from the account address and storing it
//! in the database

use super::SchemaVersion;
use sqlx::Executor;
use std::str::FromStr;
use tokio_stream::StreamExt;

/// Run database migration and returns the new database schema version when
/// successful.
pub async fn run(tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<SchemaVersion> {
    tx.as_mut()
        .execute(sqlx::raw_sql(include_str!(
            "m0008-pre-canonical-address-and-transaction-search-index.sql"
        )))
        .await?;

    let update_queries = {
        let mut accounts = sqlx::query("SELECT index, address FROM accounts").fetch(tx.as_mut());
        let mut update_queries = Vec::new();
        while let Some(row) = accounts.try_next().await? {
            let account_address: String = sqlx::Row::try_get(&row, "address")?;
            let index: i64 = sqlx::Row::try_get(&row, "index")?;
            let account_address =
                concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(
                    &account_address,
                )?;
            let canonical_address = account_address
                .get_canonical_address()
                .0
                .as_slice()
                .to_vec();
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

    tx.as_mut()
        .execute(sqlx::raw_sql(include_str!(
            "m0008-post-canonical-address-migration.sql"
        )))
        .await?;

    Ok(SchemaVersion::AccountBaseAddress)
}
