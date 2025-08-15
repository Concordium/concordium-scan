use anyhow::Result;
use prometheus_client::{metrics::gauge::Gauge, registry::Registry};
use sqlx::PgPool;
use std::sync::Arc;
use tracing::{debug, info};

/// Represent the metric name that will be registered with prometheus
const ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_active_count";
const IDLE_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_idle_count";
const MAX_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_max_count";

/// Database connection stats. Holds all the details about active connections,
/// idle connections and max connections
#[derive(sqlx::FromRow)]
pub struct DatabaseConnectionStats {
    /// Active database connections
    active: i64,
    /// Idle database connections
    idle:   i64,
    /// Max database connections
    max:    i64,
}

/// Abstract trait definition of the Database statistics provider
pub trait DatabaseStatisticsProvider: Send + Sync {
    fn query_db_pool_connections_stats(&self) -> Result<DatabaseConnectionStats>;
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
/// to check the database connections and return the active, idle and max
/// connections in the result
impl DatabaseStatisticsProvider for RealDatabaseStatisticsProvider {
    fn query_db_pool_connections_stats(&self) -> Result<DatabaseConnectionStats> {
        let pool = self.pool.clone();

        let max: i64 = pool.options().get_max_connections().into();
        let size: i64 = pool.size().into();
        let idle: i64 =
            i64::try_from(pool.num_idle()).expect("expected to convert idle connections to i64");
        let active: i64 = size - idle;

        let result_database_statistics = DatabaseConnectionStats {
            active,
            idle,
            max,
        };

        Ok(result_database_statistics)
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
    /// prometheus gauge representing the max database connections
    max_db_connections:    Gauge<i64>,
}

/// Implementation for Database Metrics. It specifies a Generic Provider Type,
/// so that it can easily be mocked for testing later
impl<P: DatabaseStatisticsProvider> DatabaseMetrics<P> {
    pub fn new(provider: P, registry: &mut Registry) -> Self {
        info!("Creating Database Metrics collector now for database statistics gathering");
        let active_db_connections = Gauge::default();
        let idle_db_connections = Gauge::default();
        let max_db_connections = Gauge::default();

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
            MAX_DATABASE_CONNECTIONS_METRIC_NAME,
            "Max DB connections",
            max_db_connections.clone(),
        );

        info!(
            "The following metrics are registered successfully for prometheus: {}, {}, {}",
            ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME,
            IDLE_DATABASE_CONNECTIONS_METRIC_NAME,
            MAX_DATABASE_CONNECTIONS_METRIC_NAME
        );

        Self {
            provider,
            active_db_connections,
            idle_db_connections,
            max_db_connections,
        }
    }

    /// Update database metrics. Uses the dedicated provider to query the
    /// database connection stats, so that they can be updated in the registry
    /// for Prometheus
    pub fn update(&self) -> Result<()> {
        let db_connection_stats = self
            .provider
            .query_db_pool_connections_stats()
            .expect("Database connection statistics could not be resolved");

        debug!(
            "db connection statistics: max: {}, active: {}, idle: {}, ",
            &db_connection_stats.max, &db_connection_stats.active, &db_connection_stats.idle
        );

        self.active_db_connections.set(db_connection_stats.active);
        self.idle_db_connections.set(db_connection_stats.idle);
        self.max_db_connections.set(db_connection_stats.max);
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use anyhow::Result;
    use mockall::{mock, predicate::*};
    use prometheus_client::registry::Registry;

    // Create a mock for the trait
    mock! {
       pub DatabaseStatisticsProvider {}
        impl DatabaseStatisticsProvider for DatabaseStatisticsProvider {
            fn query_db_pool_connections_stats(&self) -> Result<DatabaseConnectionStats>;
        }
    }

    #[tokio::test]
    async fn test_update_sets_gauges_correctly() {
        // setup mocks and dependencies
        let mut mock_provider = MockDatabaseStatisticsProvider::new();
        let mut registry = Registry::default();

        // Mock return call from querying the database connection stats
        mock_provider.expect_query_db_pool_connections_stats().returning(|| {
            Ok(DatabaseConnectionStats {
                active: 5,
                idle:   3,
                max:    8,
            })
        });

        // invoke real update function
        let metrics = DatabaseMetrics::new(mock_provider, &mut registry);
        metrics.update().unwrap();

        // assert all the database connections are set correctly
        assert_eq!(5, metrics.active_db_connections.get());
        assert_eq!(3, metrics.idle_db_connections.get());
        assert_eq!(8, metrics.max_db_connections.get());

        // Gte the encoded metrics from the prometheus registry
        let mut encoded = String::new();
        prometheus_client::encoding::text::encode(&mut encoded, &registry).unwrap();

        // assert that the encoded contains the correct metrics now too
        assert!(encoded.contains("db_connections_active_count 5"));
        assert!(encoded.contains("db_connections_idle_count 3"));
        assert!(encoded.contains("db_connections_max_count 8"));
    }
}
