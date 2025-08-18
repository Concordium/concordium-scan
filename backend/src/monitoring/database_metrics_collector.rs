//! Contains `DatabaseMetricsCollector` an implementation of
//! prometheus_client::Collector for gathering metrics related to the database
//! pool used.
use prometheus_client::{
    collector::Collector,
    encoding::{DescriptorEncoder, EncodeMetric},
    metrics::gauge::ConstGauge,
};
use sqlx::PgPool;
use tracing::debug;

const ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_active_gauge";
const IDLE_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_idle_gauge";
const MAX_DATABASE_CONNECTIONS_METRIC_NAME: &str = "db_connections_max_gauge";

/// Abstract Database pool definition, with functions to get the database
/// connections. Defined as a Trait to facilitate mocking for unit tests.
trait DatabasePool {
    fn get_max_connections(&self) -> u32;
    fn size(&self) -> u32;
    fn num_idle(&self) -> usize;
}

/// implementation of Database pool for the Pg pool which allows to swap out the
/// implementation for testing.
impl DatabasePool for PgPool {
    fn get_max_connections(&self) -> u32 { self.options().get_max_connections() }

    fn size(&self) -> u32 { self.size() }

    fn num_idle(&self) -> usize { self.num_idle() }
}

/// Database metrics collector defines our DatabasePool trait so that the
/// implementation can be swapped for testing
#[derive(Debug)]
pub struct DatabaseMetricsCollector<DatabasePool> {
    pool: DatabasePool,
}

/// Implementation of database metrics collector defines a generic type, and
/// where this is of type DatabasePool we construct and return a reference to
/// self
impl<Pool> DatabaseMetricsCollector<Pool> {
    pub fn new(pool: Pool) -> Self {
        Self {
            pool,
        }
    }
}

/// Implements the Collector where its function `encode` is called each time the
/// data is scraped for prometheus. Our pool reference is used to get the max db
/// connections, the current pool size which is active + idle connections and
/// finally the idle connections. Gauges are defined for metrics collection
/// which are: max, active and idle connections.
/// send, sync and std::fmt::debug are required by bounds by the Collector and
/// the 'static keyword is required because the of the Pools lifetime
impl<Pool: DatabasePool + 'static + Send + Sync + std::fmt::Debug> Collector
    for DatabaseMetricsCollector<Pool>
{
    fn encode(&self, mut encoder: DescriptorEncoder) -> Result<(), std::fmt::Error> {
        let max_db_connections_count: u64 = self.pool.get_max_connections().into();
        let size_db_connections_count: u64 = self.pool.size().into();
        let idle_db_connections_count: u64 = u64::try_from(self.pool.num_idle())
            .expect("expected to convert idle connections to u64");
        let active_db_connections_count = size_db_connections_count - idle_db_connections_count;

        debug!(
            "db connection metrics now for pool. max: {}, active: {}, idle: {}",
            &max_db_connections_count, &active_db_connections_count, &idle_db_connections_count
        );

        // Gauges
        let max_connections_gauge = ConstGauge::new(max_db_connections_count);
        let active_connections_gauge = ConstGauge::new(active_db_connections_count);
        let idle_connections_gauge = ConstGauge::new(idle_db_connections_count);

        max_connections_gauge.encode(encoder.encode_descriptor(
            MAX_DATABASE_CONNECTIONS_METRIC_NAME,
            "max database connections available",
            None,
            max_connections_gauge.metric_type(),
        )?)?;

        active_connections_gauge.encode(encoder.encode_descriptor(
            ACTIVE_DATABASE_CONNECTIONS_METRIC_NAME,
            "active database connections",
            None,
            active_connections_gauge.metric_type(),
        )?)?;

        idle_connections_gauge.encode(encoder.encode_descriptor(
            IDLE_DATABASE_CONNECTIONS_METRIC_NAME,
            "idle database connections",
            None,
            idle_connections_gauge.metric_type(),
        )?)?;

        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    /// dummy pool definition for testing
    #[derive(Debug)]
    struct DummyPool {
        max:  u32,
        size: u32,
        idle: usize,
    }

    // implementation of dummy connection pool
    impl DatabasePool for DummyPool {
        fn get_max_connections(&self) -> u32 { self.max }

        fn size(&self) -> u32 { self.size }

        fn num_idle(&self) -> usize { self.idle }
    }

    /// confirms that the expected metrics are encoded when we set up a dummy
    /// connection pool.
    #[test]
    fn encodes_expected_metrics() {
        let pool = DummyPool {
            max:  10,
            size: 8,
            idle: 3,
        };

        let collector = DatabaseMetricsCollector {
            pool,
        };

        let mut registry = prometheus_client::registry::Registry::default();
        registry.register_collector(Box::new(collector));

        let mut buf = String::new();
        prometheus_client::encoding::text::encode(&mut buf, &registry).unwrap();

        assert!(buf.contains("db_connections_active_gauge 5"));
        assert!(buf.contains("db_connections_idle_gauge 3"));
        assert!(buf.contains("db_connections_max_gauge 10"));
    }
}
