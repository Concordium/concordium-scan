use anyhow::Result;
use prometheus_client::{metrics::gauge::Gauge, registry::Registry};
use sqlx::PgPool;
use std::sync::Arc;
use tonic::async_trait;
use tracing::{debug, info};

/// Represent the metric name that will be registered with prometheus 
const ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_active_count";
const IDLE_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_idle_count";
const TOTAL_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_total_count";

/// Database connection stats. Holds all the details about active connections,
/// idle connections and total connections
#[derive(sqlx::FromRow)]
struct DatabaseConnectionStats {
    /// Active database connections
    active: i64,
    /// Idle database connections
    idle:   i64,
    /// Total database connections
    total:  i64,
}

/// Abstract trait definition of the Database statistics provider
#[async_trait]
pub trait DatabaseStatisticsProvider: Send + Sync {
    async fn query_database_connections_stats(&self) -> Result<(i64, i64, i64)>;
}

/// Real Database statistics provider. Takes an Arc of the pool so that we can
/// later clone it for querying
pub struct RealDatabaseStatisticsProvider {
    pool: Arc<PgPool>,
}

/// Implementation for the Real Database Statistics Provider, creates a new Arc
/// for the provided pool
impl RealDatabaseStatisticsProvider {
    pub fn new(pool: PgPool) -> Self {
        Self {
            pool: Arc::new(pool),
        }
    }
}

/// Database statistics provider implementation for the Real database statistics
/// provider. Here we will clone the arc of the pool and perform the SQL query
/// to check the database connections and return the active, idle and total
/// connections in the result
#[async_trait]
impl DatabaseStatisticsProvider for RealDatabaseStatisticsProvider {
    async fn query_database_connections_stats(&self) -> Result<(i64, i64, i64)> {
        let pool = self.pool.clone();
        let database_connection_statistics: DatabaseConnectionStats = sqlx::query_as!(
            DatabaseConnectionStats,
            r#"
            SELECT
                COUNT(*) FILTER (WHERE state = 'active') AS "active!",
                COUNT(*) FILTER (WHERE state = 'idle') AS "idle!",
                COUNT(*) AS "total!"
            FROM pg_stat_activity
            WHERE datname = current_database()
            "#
        )
        .fetch_one(&*pool)
        .await?;

        Ok((
            database_connection_statistics.active,
            database_connection_statistics.idle,
            database_connection_statistics.total,
        ))
    }
}

/// Use to capture statistics and metrics related to the database, specifically
/// database connection details
#[derive(Clone)]
pub struct DatabaseMetrics<P: DatabaseStatisticsProvider> {
    /// database connection statistics provider
    provider:              P,
    /// prometheus gauge representing the active database connections
    active_db_connections: Gauge<i64>,
    /// prometheus gauge representing the idle database connections
    idle_db_connections:   Gauge<i64>,
    /// prometheus gauge representing the total database connections
    total_db_connections:  Gauge<i64>,
}

/// Implementation for Database Metrics. It specifies a Generic Provider Type,
/// so that it can easily be mocked for testing later
impl<P: DatabaseStatisticsProvider> DatabaseMetrics<P> {
    pub fn new(provider: P, registry: &mut Registry) -> Self {
        info!("Creating Database Metrics collector now for database statistics gathering");
        let active_db_connections = Gauge::default();
        let idle_db_connections = Gauge::default();
        let total_db_connections = Gauge::default();

        registry.register(
            ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME,
            "Active DB connections",
            active_db_connections.clone(),
        );
        registry.register(
            IDLE_DATABASE_CONNECTIONS_METRIC_NAME,
            "Idle DB connections",
            idle_db_connections.clone(),
        );
        registry.register(
            TOTAL_DATABASE_CONNECTIONS_METRIC_NAME,
            "Total DB connections",
            total_db_connections.clone(),
        );

        info!(
            "The following metrics are registered successfully for prometheus: {}, {}, {}",
            ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME,
            IDLE_DATABASE_CONNECTIONS_METRIC_NAME,
            TOTAL_DATABASE_CONNECTIONS_METRIC_NAME
        );

        Self {
            provider,
            active_db_connections,
            idle_db_connections,
            total_db_connections,
        }
    }

    /// Update database metrics. Uses the dedicated provider to query the
    /// database connection stats, so that they can be updated in the registry
    /// for Prometheus
    pub async fn update(&self) -> Result<()> {
        let (active_db_connection_count, idle_db_connection_count, total_db_connection_count) =
            self.provider.query_database_connections_stats().await?;

        debug!(
            "Database connection statistics are as follows: active: {}, idle: {}, total: {}",
            &active_db_connection_count, &idle_db_connection_count, &total_db_connection_count
        );

        self.active_db_connections.set(active_db_connection_count);
        self.idle_db_connections.set(idle_db_connection_count);
        self.total_db_connections.set(total_db_connection_count);
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use anyhow::Result;
    use mockall::{mock, predicate::*};
    use prometheus_client::registry::Registry;
    use tonic::async_trait;

    // Create a mock for the trait
    mock! {
        pub DatabaseStatisticsProvider {}
        #[async_trait]
        impl DatabaseStatisticsProvider for DatabaseStatisticsProvider {
            async fn query_database_connections_stats(&self) -> Result<(i64, i64, i64)>;
        }
    }

    #[tokio::test]
    async fn test_update_sets_gauges_correctly() {
        // setup mocks and dependencies
        let mut mock_provider = MockDatabaseStatisticsProvider::new();
        let mut registry = Registry::default();

        // Mock return call from querying the database connection stats
        mock_provider.expect_query_database_connections_stats().returning(|| Ok((5, 3, 8)));

        // invoke real update function
        let metrics = DatabaseMetrics::new(mock_provider, &mut registry);
        metrics.update().await.unwrap();

        // assert all the database connections are set correctly
        assert_eq!(5, metrics.active_db_connections.get());
        assert_eq!(3, metrics.idle_db_connections.get());
        assert_eq!(8, metrics.total_db_connections.get());

        // Gte the encoded metrics from the prometheus registry
        let mut encoded = String::new();
        prometheus_client::encoding::text::encode(&mut encoded, &registry).unwrap();

        // assert that the encoded contains the correct metrics now too
        assert!(encoded.contains("db_connections_active_count 5"));
        assert!(encoded.contains("db_connections_idle_count 3"));
        assert!(encoded.contains("db_connections_total_count 8"));
    }
}
