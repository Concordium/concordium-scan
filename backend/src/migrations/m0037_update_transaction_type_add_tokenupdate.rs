//! - `transactions.type_account` due to deprecated `TokenHolder` and
//!   `TokenGovernance` enum values being present. These are not part of the
//!   intended devnet account_transaction_type enum set.

use super::SchemaVersion;
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::UpdateTransactionTypeAddTokenUpdate;

pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    _endpoints: &[concordium_rust_sdk::v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
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
        sqlx::query(
            r#"
            UPDATE transactions
            SET type_account = TokenUpdate
            WHERE type_account IN ('TokenHolder', 'TokenGovernance')
            "#,
        )
        .execute(tx.as_mut())
        .await?;
    }

    sqlx::query(
        r#"
        ALTER TABLE transactions
        ADD COLUMN type_account_new account_transaction_type_new
        "#,
    )
    .execute(tx.as_mut())
    .await?;

    sqlx::query(
        r#"
        UPDATE transactions
        SET type_account_new = type_account::TEXT::account_transaction_type_new
        WHERE type_account IS NOT NULL
        "#,
    )
    .execute(tx.as_mut())
    .await?;

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

    sqlx::query("DROP TYPE account_transaction_type").execute(tx.as_mut()).await?;

    sqlx::query("ALTER TYPE account_transaction_type_new RENAME TO account_transaction_type")
        .execute(tx.as_mut())
        .await?;

    Ok(NEXT_SCHEMA_VERSION)
}
