//! - `transactions.type_account` due to deprecated `TokenHolder` and
//!   `TokenGovernance` enum values being present. These are not part of the
//!   intended devnet account_transaction_type enum set.

use super::SchemaVersion;
use tracing::debug;
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::UpdateTransactionTypeAddTokenUpdate;

/// Run database migration and return the new schema version when successful.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    _endpoints: &[concordium_rust_sdk::v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    // Step 3a: Create new enum without the deprecated values
    debug!("Creating new enum type 'account_transaction_type_new'...");
    sqlx::query(
        r#"
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
    )
    "#,
    )
    .execute(tx.as_mut())
    .await?;

    debug!("Checking for invalid enum values in 'transactions.type_account'...");

    // Step 1: Find invalid enum values (TokenHolder, TokenGovernance)
    let rows = sqlx::query(
        r#"
        SELECT index, type_account::TEXT
        FROM transactions
        WHERE type_account IN ('TokenHolder', 'TokenGovernance')
        "#,
    )
    .fetch_all(tx.as_mut())
    .await?;

    if !rows.is_empty() {
        debug!(
            "Found {} transactions with deprecated enum values. Setting them to NULL...",
            rows.len()
        );

        // Step 2: Set invalid values to NULL to allow casting
        sqlx::query(
            r#"
            UPDATE transactions
            SET type_account = TokenUpdate
            WHERE type_account IN ('TokenHolder', 'TokenGovernance')
            "#,
        )
        .execute(tx.as_mut())
        .await?;
    } else {
        debug!("No invalid enum values found.");
    }

    // Step 3: Add new column with updated enum type
    debug!("Adding temporary column 'type_account_new' with new enum type...");
    sqlx::query(
        r#"
        ALTER TABLE transactions
        ADD COLUMN type_account_new account_transaction_type_new
        "#,
    )
    .execute(tx.as_mut())
    .await?;

    // Step 4: Copy over values
    debug!("Copying valid enum values to the new column...");
    sqlx::query(
        r#"
        UPDATE transactions
        SET type_account_new = type_account::TEXT::account_transaction_type_new
        WHERE type_account IS NOT NULL
        "#,
    )
    .execute(tx.as_mut())
    .await?;

    // Step 5: Drop old column, rename new column
    debug!("Replacing old enum column with the new one...");
    sqlx::query(
        r#"
        ALTER TABLE transactions
        DROP COLUMN type_account
        "#,
    )
    .execute(tx.as_mut())
    .await?;

    sqlx::query(
        r#"
        ALTER TABLE transactions
        RENAME COLUMN type_account_new TO type_account
        "#,
    )
    .execute(tx.as_mut())
    .await?;

    // Step 6: Drop old enum and rename new enum type
    debug!("Replacing old enum type...");
    sqlx::query("DROP TYPE account_transaction_type").execute(tx.as_mut()).await?;

    sqlx::query("ALTER TYPE account_transaction_type_new RENAME TO account_transaction_type")
        .execute(tx.as_mut())
        .await?;

    Ok(NEXT_SCHEMA_VERSION)
}
