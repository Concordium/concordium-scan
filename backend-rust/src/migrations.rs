use anyhow::Context;
use concordium_rust_sdk::v2;
use sqlx::{Executor, PgPool};
use std::cmp::Ordering;
use tokio_util::sync::CancellationToken;
use tracing::info;

type Transaction = sqlx::Transaction<'static, sqlx::Postgres>;

mod m0005_fix_dangling_delegators;
mod m0006_fix_stake;
mod m0008_canonical_address_and_transaction_search_index;

/// Ensure the current database schema version is compatible with the supported
/// schema version.
/// When the current database schema version is older than supported, ensure no
/// additive versions since then, as the API might depend on information
/// introduced. When the current database schema is newer, ensure no destructive
/// versions have been introduced since then, as the API might depend on
/// information now removed.
pub async fn ensure_compatible_schema_version(
    pool: &PgPool,
    supported: SchemaVersion,
) -> anyhow::Result<()> {
    if !has_migration_table(pool).await? {
        anyhow::bail!(
            "Failed to find database schema version.

Use `ccdscan-indexer --migrate` to initialize the database schema."
        )
    }
    let current = current_schema_version(pool).await?;
    match current.cmp(&supported) {
        // If the database schema version is exactly the supported one, we are done.
        Ordering::Equal => (),
        // If current database schema version is older than supported version, we check
        // if any version between the current database schema and the
        // supported are additive (non-destructive), if so the API most likely
        // depend on this new information and we produce an error.
        Ordering::Less => {
            // Iterate versions between current and supported schema version.
            for version_between in current.as_i64()..supported.as_i64() {
                let version_between = SchemaVersion::from_version(version_between)
                    .context("Unexpected gap between schema versions.")?;
                if !version_between.is_destructive() {
                    anyhow::bail!(
                        "Database is using an older schema version not supported by this version \
                         of `ccdscan-api`.

Use `ccdscan-indexer --migrate` to migrate the database schema."
                    )
                }
            }
        }
        // If the database schema version is newer than the supported schema version, we check if
        // any of the migrations were destructive, since the API would most likely still depend on
        // the information we produce an error in this case.
        Ordering::Greater => {
            let destructive_migrations = destructive_schema_version_since(pool, supported).await?;
            if !destructive_migrations.is_empty() {
                anyhow::bail!(
                    "Database is using a newer schema version, which is not compatible with the \
                     supported version of this service.
    Support {}

The following breaking schema migrations have happened:
  - {}",
                    Migration::from(supported),
                    destructive_migrations
                        .iter()
                        .map(|m| m.to_string())
                        .collect::<Vec<_>>()
                        .join("\n  - ")
                );
            }
        }
    }
    Ok(())
}

/// Migrate the database schema to the latest version.
pub async fn run_migrations(
    pool: PgPool,
    endpoints: Vec<v2::Endpoint>,
    cancel_token: CancellationToken,
) -> anyhow::Result<()> {
    cancel_token
        .run_until_cancelled(async move {
            ensure_migrations_table(&pool).await?;
            let mut current = current_schema_version(&pool).await?;
            info!("Current database schema version {}", current.as_i64());
            info!("Latest database schema version {}", SchemaVersion::LATEST.as_i64());
            while current < SchemaVersion::LATEST {
                info!("Running migration from database schema version {}", current.as_i64());
                let new_version = current.migration_to_next(&pool, endpoints.as_slice()).await?;
                info!("Migrated database schema to version {} successfully", new_version.as_i64());
                current = new_version
            }
            Ok::<_, anyhow::Error>(())
        })
        .await
        .transpose()?;
    Ok(())
}

/// Check whether the current database schema version matches the latest known
/// database schema version.
pub async fn ensure_latest_schema_version(pool: &PgPool) -> anyhow::Result<()> {
    if !has_migration_table(pool).await? {
        anyhow::bail!(
            "Failed to find the database schema version.

To allow running database migrations provide the `--migrate` flag.
Use `--help` for more information."
        )
    }
    let current = current_schema_version(pool).await?;
    if current != SchemaVersion::LATEST {
        anyhow::bail!(
            "Current database schema version is not the latest
    Current: {}
     Latest: {}

To allow running database migrations provide the `--migrate` flag.
Use `--help` for more information.",
            current.as_i64(),
            SchemaVersion::LATEST.as_i64()
        )
    }
    Ok(())
}

#[derive(Debug, derive_more::Display)]
#[display("Migration {version}:{description}")]
struct Migration {
    /// Version number for the database schema.
    version:     i64,
    /// Short description of the point of the migration.
    description: String,
    /// Whether the migration does a breaking change to the database schema.
    /// This can be used for the API to ensure no breaking change has happened
    /// since the supported schema version.
    destructive: bool,
}

impl From<SchemaVersion> for Migration {
    fn from(value: SchemaVersion) -> Self {
        Migration {
            version:     value.as_i64(),
            description: value.to_string(),
            destructive: value.is_destructive(),
        }
    }
}

/// Represents database schema versions.
/// Whenever migrating the database a new variant should be introduced.
#[derive(
    Debug,
    PartialEq,
    Eq,
    PartialOrd,
    Ord,
    Clone,
    Copy,
    derive_more::Display,
    num_derive::FromPrimitive,
)]
#[repr(i64)]
pub enum SchemaVersion {
    #[display("0000:Empty database with no tables yet.")]
    Empty,
    #[display(
        "0001:Initial schema version with tables for blocks, transactions, accounts, contracts, \
         tokens and more."
    )]
    InitialFirstHalf,
    #[display(
        "0002:Adds index over blocks without cumulative finalization time, improving indexer \
         performance."
    )]
    IndexBlocksWithNoCumulativeFinTime,
    #[display("0003:PayDayPoolCommissionRates")]
    PayDayPoolCommissionRates,
    #[display("0004:PayDayLotteryPowers")]
    PayDayLotteryPowers,
    #[display("0005:Fix invalid data of dangling delegators.")]
    FixDanglingDelegators,
    #[display("0006:Fix staked amounts")]
    FixStakedAmounts,
    #[display("0007:Accumulated pool state columns.")]
    AddAccumulatedPoolState,
    #[display("0008:AccountBaseAddress")]
    AccountBaseAddress,
    #[display("0009:StakedPoolSizeConstraint")]
    StakedPoolSizeConstraint,
}
impl SchemaVersion {
    /// The minimum supported database schema version for the API.
    /// Fails at startup if any breaking database schema versions have been
    /// introduced since this version.
    pub const API_SUPPORTED_SCHEMA_VERSION: SchemaVersion = SchemaVersion::AddAccumulatedPoolState;
    /// The latest known version of the schema.
    const LATEST: SchemaVersion = SchemaVersion::StakedPoolSizeConstraint;

    /// Parse version number into a database schema version.
    /// None if the version is unknown.
    fn from_version(version: i64) -> Option<SchemaVersion> {
        num_traits::FromPrimitive::from_i64(version)
    }

    /// Convert to the integer representation used as version in the database.
    fn as_i64(self) -> i64 { self as i64 }

    /// Whether introducing the database schema version is destructive, meaning
    /// not backwards compatible.
    fn is_destructive(self) -> bool {
        match self {
            SchemaVersion::Empty => false,
            SchemaVersion::InitialFirstHalf => false,
            SchemaVersion::IndexBlocksWithNoCumulativeFinTime => false,
            SchemaVersion::PayDayPoolCommissionRates => false,
            SchemaVersion::PayDayLotteryPowers => false,
            SchemaVersion::FixDanglingDelegators => false,
            SchemaVersion::FixStakedAmounts => false,
            SchemaVersion::AddAccumulatedPoolState => false,
            SchemaVersion::AccountBaseAddress => false,
            SchemaVersion::StakedPoolSizeConstraint => false,
        }
    }

    /// Run migrations for this schema version to the next.
    async fn migration_to_next(
        &self,
        pool: &PgPool,
        endpoints: &[v2::Endpoint],
    ) -> anyhow::Result<SchemaVersion> {
        let mut tx = pool.begin().await?;
        let start_time = chrono::Utc::now();
        let new_version = match self {
            SchemaVersion::Empty => {
                // Set up the initial database schema.
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!("./migrations/m0001-initialize.sql")))
                    .await?;
                SchemaVersion::InitialFirstHalf
            }
            SchemaVersion::InitialFirstHalf => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0002-block-cumulative-fin-time-index.sql"
                    )))
                    .await?;
                SchemaVersion::IndexBlocksWithNoCumulativeFinTime
            }
            SchemaVersion::IndexBlocksWithNoCumulativeFinTime => {
                tx.as_mut().execute(sqlx::raw_sql(include_str!("./migrations/m0003.sql"))).await?;
                SchemaVersion::PayDayPoolCommissionRates
            }
            SchemaVersion::PayDayPoolCommissionRates => {
                tx.as_mut().execute(sqlx::raw_sql(include_str!("./migrations/m0004.sql"))).await?;
                SchemaVersion::PayDayLotteryPowers
            }
            SchemaVersion::PayDayLotteryPowers => {
                m0005_fix_dangling_delegators::run(&mut tx, endpoints).await?
            }
            SchemaVersion::FixDanglingDelegators => {
                m0006_fix_stake::run(&mut tx, endpoints).await?
            }
            SchemaVersion::FixStakedAmounts => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0007-cumulate-pool-info.sql"
                    )))
                    .await?;
                SchemaVersion::AddAccumulatedPoolState
            }
            SchemaVersion::AddAccumulatedPoolState => {
                m0008_canonical_address_and_transaction_search_index::run(&mut tx).await?
            }
            SchemaVersion::AccountBaseAddress => {
                tx.as_mut().execute(sqlx::raw_sql(include_str!("./migrations/m0009_pool_info_constraint.sql"))).await?;
                SchemaVersion::StakedPoolSizeConstraint
            },
            SchemaVersion::StakedPoolSizeConstraint => unimplemented!(
                "No migration implemented for database schema version {}",
                self.as_i64()
            )
        };
        let end_time = chrono::Utc::now();
        insert_migration(&mut tx, &new_version.into(), start_time, end_time).await?;
        tx.commit().await?;
        Ok(new_version)
    }
}

/// Set up the migrations tables if not already there.
async fn ensure_migrations_table(pool: &PgPool) -> anyhow::Result<()> {
    sqlx::query!(
        r#"CREATE TABLE IF NOT EXISTS migrations (
            version BIGINT PRIMARY KEY,
            description TEXT NOT NULL,
            destructive BOOL NOT NULL,
            start_time TIMESTAMPTZ NOT NULL,
            end_time TIMESTAMPTZ NOT NULL
        )"#
    )
    .execute(pool)
    .await?;
    Ok(())
}

/// Check the existence of the 'migration' table.
async fn has_migration_table(pool: &PgPool) -> anyhow::Result<bool> {
    let has_migration_table = sqlx::query_scalar!(
        "SELECT EXISTS (
            SELECT FROM information_schema.tables
            WHERE  table_schema = 'public'
            AND    table_name   = 'migrations'
        )"
    )
    .fetch_one(pool)
    .await?
    .unwrap_or(false);
    Ok(has_migration_table)
}

/// Query the migrations table for the current database schema version.
/// Results in an error if not migrations table found.
pub async fn current_schema_version(pool: &PgPool) -> anyhow::Result<SchemaVersion> {
    let version = sqlx::query_scalar!("SELECT MAX(version) FROM migrations")
        .fetch_one(pool)
        .await?
        .unwrap_or(0);
    SchemaVersion::from_version(version).context("Unknown database schema version")
}

/// Update the migrations table with a new migration.
async fn insert_migration(
    connection: &mut Transaction,
    migration: &Migration,
    start_time: chrono::DateTime<chrono::Utc>,
    end_time: chrono::DateTime<chrono::Utc>,
) -> anyhow::Result<()> {
    sqlx::query!(
        "INSERT INTO migrations (version, description, destructive, start_time, end_time) VALUES \
         ($1, $2, $3, $4, $5)",
        migration.version,
        migration.description,
        migration.destructive,
        start_time,
        end_time
    )
    .execute(connection.as_mut())
    .await?;
    Ok(())
}

/// Query the migrations table for any migrations which have been destructive
/// since the provided version.
async fn destructive_schema_version_since(
    pool: &PgPool,
    version: SchemaVersion,
) -> anyhow::Result<Vec<Migration>> {
    let rows = sqlx::query_as!(
        Migration,
        "SELECT
            version, description, destructive
        FROM migrations
        WHERE version > $1
            AND destructive IS TRUE
        ORDER BY version ASC",
        version.as_i64()
    )
    .fetch_all(pool)
    .await?;
    Ok(rows)
}
