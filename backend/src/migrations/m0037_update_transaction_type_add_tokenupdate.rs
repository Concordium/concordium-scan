//! - `transactions.type_account` due to deprecated `TokenHolder` and
//!   `TokenGovernance` enum values being present. These are not part of the
//!   intended devnet account_transaction_type enum set.

use tracing::info;

use super::SchemaVersion;
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::UpdateTransactionTypeAddTokenUpdate;

const BATCH_SIZE: i64 = 10_000;

pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    _endpoints: &[concordium_rust_sdk::v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    sqlx::query(
        r#"
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_type WHERE typname = 'account_transaction_type_new'
            ) THEN
                CREATE TYPE account_transaction_type_new AS ENUM (
                    'InitializeSmartContractInstance',
                    'UpdateSmartContractInstance',
                    'SimpleTransfer',
                    'EncryptedTransfer',
                    'SimpleTransferWithMemo',
                    'EncryptedTransferWithMemo',
                    'TransferWithScheduleWithMemo',
                    'DeployModule',
                    'AddBaker',
                    'RemoveBaker',
                    'UpdateBakerStake',
                    'UpdateBakerRestakeEarnings',
                    'UpdateBakerKeys',
                    'UpdateCredentialKeys',
                    'TransferToEncrypted',
                    'TransferToPublic',
                    'TransferWithSchedule',
                    'UpdateCredentials',
                    'RegisterData',
                    'ConfigureBaker',
                    'ConfigureDelegation',
                    'TokenUpdate'
                );
            END IF;
        END$$;
        "#,
    )
    .execute(tx.as_mut())
    .await?;
    info!("Created new enum: account_transaction_type_new");

    sqlx::query(
        r#"
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'transactions' AND column_name = 'type_account_new'
            ) THEN
                ALTER TABLE transactions
                ADD COLUMN type_account_new account_transaction_type_new;
            END IF;
        END$$;
        "#,
    )
    .execute(tx.as_mut())
    .await?;
    info!("Added new column: type_account_new");

    let mut total_updated = 0;

    loop {
        let rows_updated = sqlx::query(
            r#"
            WITH batch AS (
                SELECT index
                FROM transactions
                WHERE type_account IN ('TokenHolder', 'TokenGovernance')
                LIMIT $1
            )
            UPDATE transactions t
            SET type_account_new = 'TokenUpdate'
            FROM batch
            WHERE t.index = batch.index
            "#,
        )
        .bind(BATCH_SIZE)
        .execute(tx.as_mut())
        .await?
        .rows_affected();

        total_updated += rows_updated;
        println!(
            "Updating to TokenUpdate in transactions table. Running total: {} rows updated",
            total_updated
        );

        if rows_updated == 0 {
            break;
        }
    }
    info!("Finished replacing deprecated values.");

    let mut total_migrated = 0;

    loop {
        let rows_updated = sqlx::query(
            r#"
            WITH batch AS (
                SELECT index, type_account::TEXT AS type_text
                FROM transactions
                WHERE type_account IS NOT NULL
                  AND type_account_new IS NULL
                LIMIT $1
            )
            UPDATE transactions t
            SET type_account_new = batch.type_text::account_transaction_type_new
            FROM batch
            WHERE t.index = batch.index
            "#,
        )
        .bind(BATCH_SIZE)
        .execute(tx.as_mut())
        .await?
        .rows_affected();

        total_migrated += rows_updated;
        info!(
            "Migrating type_account to type_account_new in transactions table. Running total: {} \
             rows migrated",
            total_migrated
        );

        if rows_updated == 0 {
            break;
        }
    }
    info!("Finished copying all `type_account` values into new column.");

    sqlx::query(
        r#"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'transactions' AND column_name = 'type_account'
            ) THEN
                ALTER TABLE transactions DROP COLUMN type_account;
            END IF;
        END$$;
        "#,
    )
    .execute(tx.as_mut())
    .await?;
    info!("Dropped old column: type_account");

    sqlx::query(
        r#"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'transactions' AND column_name = 'type_account_new'
            ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'transactions' AND column_name = 'type_account'
            ) THEN
                ALTER TABLE transactions RENAME COLUMN type_account_new TO type_account;
            END IF;
        END$$;
        "#,
    )
    .execute(tx.as_mut())
    .await?;
    info!("Renamed type_account_new → type_account");

    sqlx::query(
        r#"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM pg_type WHERE typname = 'account_transaction_type'
            ) THEN
                DROP TYPE account_transaction_type;
            END IF;
        END$$;
        "#,
    )
    .execute(tx.as_mut())
    .await?;
    info!("Dropped old enum: account_transaction_type");

    sqlx::query(
        r#"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM pg_type WHERE typname = 'account_transaction_type_new'
            ) THEN
                ALTER TYPE account_transaction_type_new RENAME TO account_transaction_type;
            END IF;
        END$$;
        "#,
    )
    .execute(tx.as_mut())
    .await?;
    info!("Renamed account_transaction_type_new → account_transaction_type");

    info!("Migration complete. Schema version: {NEXT_SCHEMA_VERSION:?}");

    Ok(NEXT_SCHEMA_VERSION)
}
