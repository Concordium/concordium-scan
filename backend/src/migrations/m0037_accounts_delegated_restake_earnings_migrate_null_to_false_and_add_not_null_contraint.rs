use super::SchemaVersion;
use std::time::Duration;
use tokio::time::Instant;

/// Database schema version representing the partial migration.
const PARTIAL_MIGRATION_SCHEMA_VERSION: SchemaVersion = SchemaVersion::AccountsDelegatedEarningsNullToFalseAndNotNullConstraint;


/// Run database migration and returns the new database schema version when
/// successful.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
) -> anyhow::Result<SchemaVersion> {


    // find all accounts with restake earnings currently set to null
    let accounts_restake_earnings_null: Vec<i64> =
        sqlx::query_scalar(
            "SELECT index FROM ACCOUNTS 
            WHERE delegated_restake_earnings IS NULL"
        )
        .fetch_all(tx.as_mut())
        .await?;

    let mut process = accounts_restake_earnings_null.len();
    tracing::debug!("About to process {} accounts", process);
    
    // if we have accounts to update, we will perform a partial update for each account 
    if !accounts_restake_earnings_null.is_empty() {

        // for each account that currently has null set as their 'delegated_restake_earnings' we will update them to the new default false
        for account_index in accounts_restake_earnings_null{
            let start = Instant::now();
            if start.elapsed() > Duration::from_secs(60) {
               break;
            }

            sqlx::query(
            "UPDATE ACCOUNTS
	                SET delegated_restake_earnings = false
                WHERE index = $1",
            )
            .bind(account_index)
            .execute(tx.as_mut())
            .await?;

            process -= 1;
            tracing::debug!("{} accounts remaining", process);
        }
    }

    // finally set not null constraint on the ACCOUNTS table 'delegated_restake_earnings' column
    tracing::debug!("Adding NOT NULL constraint now on ACCOUNTS table 'delegated_restake_earnings' column");

    sqlx::query(
    "ALTER TABLE ACCOUNTS ALTER COLUMN delegated_restake_earnings SET NOT NULL",
    )
    .execute(tx.as_mut())
    .await?;

    Ok(PARTIAL_MIGRATION_SCHEMA_VERSION)
}
