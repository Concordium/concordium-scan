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
mod m0010_fill_capital_bound_and_leverage_bound;
mod m0014_baker_metrics;
mod m0015_pool_rewards;
mod m0019_payday_stake_information;
mod m0020_chain_update_events;
mod m0021_amounts_schedule;
mod m0023_add_init_parameter;
mod m0025_fix_passive_delegator_stake;
mod m0026_update_genesis_validator_info;
mod m0027_reindex_credential_deployments;

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
                if new_version.is_partial() {
                    info!(
                        "Committing partial migration to schema version {}",
                        new_version.as_i64()
                    );
                } else {
                    info!(
                        "Migrated database schema to version {} successfully",
                        new_version.as_i64()
                    );
                }
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
    #[display("0010:Add delegated stake cap")]
    DelegatedStakeCap,
    #[display("0011:RankingByLotteryPower")]
    RankingByLotteryPower,
    #[display("0012:Add removed bakers table")]
    TrackRemovedBakers,
    #[display("0013:Fix delegated_restake_earnings data in accounts")]
    FixDelegatedStakeEarnings,
    #[display("0014:Add baker metrics")]
    BakerMetrics,
    #[display("0015:Add tracking of rewards paid out to bakers and delegators in payday blocks")]
    PaydayPoolRewards,
    #[display("0016:Passive delegation")]
    PassiveDelegation,
    #[display("0017:Reward metrics")]
    RewardMetrics,
    #[display("0018:Add tracking of stake to bakers and delagators in payday blocks (Partial)")]
    PaydayPoolStakePartial,
    #[display("0019:Add tracking of stake to bakers and delagators in payday blocks")]
    PaydayPoolStake,
    #[display("0020:Chain updates events")]
    ChainUpdateEvents,
    #[display("0021:Amount schedule")]
    AmountSchedule,
    #[display("0022:Fix corrupted passive delegators")]
    FixCorruptedPassiveDelegators,
    #[display("0023:Add input parameter to init transactions")]
    AddInputParameterToInitTransactions,
    #[display("0024:Precompute validators APYs")]
    BakerPeriodApyViews,
    #[display("0025:Fix passive delegators stake")]
    FixPassiveDelegatorsStake,
    #[display("0026:Update information for genesis validators")]
    UpdateGenesisValidatorInfo,
    #[display("0027:Reindex credential deployments, adjusting cost and missing information")]
    ReindexCredentialDeployment,
    #[display("0028:Reindex reward metrics, using time as leading column")]
    ReindexRewardMetrics,
}
impl SchemaVersion {
    /// The minimum supported database schema version for the API.
    /// Fails at startup if any breaking (destructive) database schema versions
    /// have been introduced since this version.
    pub const API_SUPPORTED_SCHEMA_VERSION: SchemaVersion = SchemaVersion::BakerPeriodApyViews;
    /// The latest known version of the schema.
    const LATEST: SchemaVersion = SchemaVersion::ReindexRewardMetrics;

    /// Parse version number into a database schema version.
    /// None if the version is unknown.
    fn from_version(version: i64) -> Option<SchemaVersion> {
        num_traits::FromPrimitive::from_i64(version)
    }

    /// Convert to the integer representation used as version in the database.
    fn as_i64(self) -> i64 { self as i64 }

    /// Whether introducing the database schema version is destructive, meaning
    /// not backwards compatible.
    /// Note: We use match statements here to catch missing variants at
    /// compile-time. This enforces explicit evaluation when adding a new
    /// database schema, ensuring awareness of whether the change is
    /// destructive.
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
            SchemaVersion::DelegatedStakeCap => false,
            SchemaVersion::RankingByLotteryPower => false,
            SchemaVersion::TrackRemovedBakers => false,
            SchemaVersion::FixDelegatedStakeEarnings => false,
            SchemaVersion::BakerMetrics => false,
            SchemaVersion::PaydayPoolRewards => false,
            SchemaVersion::PassiveDelegation => false,
            SchemaVersion::RewardMetrics => false,
            SchemaVersion::PaydayPoolStakePartial => false,
            SchemaVersion::PaydayPoolStake => false,
            SchemaVersion::ChainUpdateEvents => false,
            SchemaVersion::AmountSchedule => false,
            SchemaVersion::FixCorruptedPassiveDelegators => false,
            SchemaVersion::AddInputParameterToInitTransactions => false,
            SchemaVersion::BakerPeriodApyViews => false,
            SchemaVersion::FixPassiveDelegatorsStake => false,
            SchemaVersion::UpdateGenesisValidatorInfo => false,
            SchemaVersion::ReindexCredentialDeployment => false,
            SchemaVersion::ReindexRewardMetrics => false,
        }
    }

    /// Whether the database schema version is a partial migration.
    /// Note: We use match statements here to catch missing variants at
    /// compile-time. This enforces explicit evaluation when adding a new
    /// database schema, ensuring awareness of whether the change is partial.
    fn is_partial(self) -> bool {
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
            SchemaVersion::DelegatedStakeCap => false,
            SchemaVersion::RankingByLotteryPower => false,
            SchemaVersion::TrackRemovedBakers => false,
            SchemaVersion::FixDelegatedStakeEarnings => false,
            SchemaVersion::BakerMetrics => false,
            SchemaVersion::PaydayPoolRewards => false,
            SchemaVersion::PassiveDelegation => false,
            SchemaVersion::RewardMetrics => false,
            SchemaVersion::PaydayPoolStakePartial => true,
            SchemaVersion::PaydayPoolStake => false,
            SchemaVersion::ChainUpdateEvents => false,
            SchemaVersion::AmountSchedule => false,
            SchemaVersion::FixCorruptedPassiveDelegators => false,
            SchemaVersion::AddInputParameterToInitTransactions => false,
            SchemaVersion::BakerPeriodApyViews => false,
            SchemaVersion::FixPassiveDelegatorsStake => false,
            SchemaVersion::UpdateGenesisValidatorInfo => false,
            SchemaVersion::ReindexCredentialDeployment => false,
            SchemaVersion::ReindexRewardMetrics => false,
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
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0009_pool_info_constraint.sql"
                    )))
                    .await?;
                SchemaVersion::StakedPoolSizeConstraint
            }
            SchemaVersion::StakedPoolSizeConstraint => {
                let next_schema_version = SchemaVersion::DelegatedStakeCap;
                m0010_fill_capital_bound_and_leverage_bound::run(
                    &mut tx,
                    endpoints,
                    next_schema_version,
                )
                .await?
            }
            SchemaVersion::DelegatedStakeCap => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0011-ranking-by-lottery-power.sql"
                    )))
                    .await?;
                SchemaVersion::RankingByLotteryPower
            }
            SchemaVersion::RankingByLotteryPower => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!("./migrations/m0012-removed-bakers.sql")))
                    .await?;
                SchemaVersion::TrackRemovedBakers
            }
            SchemaVersion::TrackRemovedBakers => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0013-fix-removed-delegators-restake.sql"
                    )))
                    .await?;
                SchemaVersion::FixDelegatedStakeEarnings
            }
            SchemaVersion::FixDelegatedStakeEarnings => {
                let next_schema_version = SchemaVersion::BakerMetrics;
                m0014_baker_metrics::run(&mut tx, endpoints, next_schema_version).await?
            }
            SchemaVersion::BakerMetrics => {
                let next_schema_version = SchemaVersion::PaydayPoolRewards;
                m0015_pool_rewards::run(&mut tx, endpoints, next_schema_version).await?
            }
            SchemaVersion::PaydayPoolRewards => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0016-passive-delegation.sql"
                    )))
                    .await?;
                SchemaVersion::PassiveDelegation
            }
            SchemaVersion::PassiveDelegation => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!("./migrations/m0017-reward-metrics.sql")))
                    .await?;
                SchemaVersion::RewardMetrics
            }
            SchemaVersion::RewardMetrics => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0018-payday-stake-information.sql"
                    )))
                    .await?;
                SchemaVersion::PaydayPoolStakePartial
            }
            SchemaVersion::PaydayPoolStakePartial => {
                m0019_payday_stake_information::run(&mut tx, endpoints).await?
            }
            SchemaVersion::PaydayPoolStake => {
                m0020_chain_update_events::run(&mut tx, endpoints, SchemaVersion::ChainUpdateEvents)
                    .await?
            }
            SchemaVersion::ChainUpdateEvents => {
                m0021_amounts_schedule::run(&mut tx, endpoints, SchemaVersion::AmountSchedule)
                    .await?
            }
            SchemaVersion::AmountSchedule => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0022-fix-corrupted-passive-delegators.sql"
                    )))
                    .await?;
                SchemaVersion::FixCorruptedPassiveDelegators
            }
            SchemaVersion::FixCorruptedPassiveDelegators => {
                m0023_add_init_parameter::run(
                    &mut tx,
                    endpoints,
                    SchemaVersion::AddInputParameterToInitTransactions,
                )
                .await?
            }
            SchemaVersion::AddInputParameterToInitTransactions => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0024-baker-apy-materialized-view.sql"
                    )))
                    .await?;
                SchemaVersion::BakerPeriodApyViews
            }
            SchemaVersion::BakerPeriodApyViews => {
                m0025_fix_passive_delegator_stake::run(&mut tx, endpoints).await?
            }
            SchemaVersion::FixPassiveDelegatorsStake => {
                m0026_update_genesis_validator_info::run(&mut tx, endpoints).await?
            }
            SchemaVersion::UpdateGenesisValidatorInfo => {
                m0027_reindex_credential_deployments::run(&mut tx, endpoints).await?
            }
            SchemaVersion::ReindexCredentialDeployment => {
                tx.as_mut()
                    .execute(sqlx::raw_sql(include_str!(
                        "./migrations/m0028_reindex_reward_metrics.sql"
                    )))
                    .await?;
                SchemaVersion::ReindexRewardMetrics
            }
            SchemaVersion::ReindexRewardMetrics => unimplemented!(
                "No migration implemented for database schema version {}",
                self.as_i64()
            ),
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
        "INSERT INTO migrations (version, description, destructive, start_time, end_time)
         VALUES ($1, $2, $3, $4, $5)
         ON CONFLICT (version) DO UPDATE SET
             end_time = EXCLUDED.end_time",
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
