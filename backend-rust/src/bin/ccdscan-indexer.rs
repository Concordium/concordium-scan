use anyhow::Context;
use clap::Parser;
use concordium_rust_sdk::v2;
use concordium_scan::{
    indexer::{self, IndexerServiceConfig},
    migrations, router,
};
use prometheus_client::{
    metrics::{family::Family, gauge::Gauge},
    registry::Registry,
};
use sqlx::postgres::PgPoolOptions;
use std::net::SocketAddr;
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{error, info};

#[derive(Parser)]
#[command(version, author, about)]
struct Cli {
    /// The URL used for the database, something of the form
    /// "postgres://postgres:example@localhost/ccd-scan".
    /// Use an environment variable when the connection contains a password, as
    /// command line arguments are visible across OS processes.
    #[arg(long, env = "CCDSCAN_INDEXER_DATABASE_URL")]
    database_url:      String,
    /// Minimum number of connections in the pool.
    #[arg(long, env = "CCDSCAN_INDEXER_DATABASE_MIN_CONNECTIONS", default_value_t = 5)]
    min_connections:   u32,
    /// Maximum number of connections in the pool.
    #[arg(long, env = "CCDSCAN_INDEXER_DATABASE_MAX_CONNECTIONS", default_value_t = 10)]
    max_connections:   u32,
    /// gRPC interface of the node. Several can be provided.
    #[arg(
        long,
        env = "CCDSCAN_INDEXER_GRPC_ENDPOINTS",
        value_delimiter = ',',
        num_args = 1..
    )]
    node:              Vec<v2::Endpoint>,
    /// Address to listen for monitoring related requests
    #[arg(long, env = "CCDSCAN_INDEXER_MONITORING_ADDRESS", default_value = "127.0.0.1:8001")]
    monitoring_listen: SocketAddr,
    #[command(flatten, next_help_heading = "Performance tuning")]
    indexer_config:    IndexerServiceConfig,
    /// The maximum log level. Possible values are: `trace`, `debug`, `info`,
    /// `warn`, and `error`.
    #[arg(long = "log-level", default_value = "info", env = "LOG_LEVEL")]
    log_level:         tracing_subscriber::filter::LevelFilter,
    /// Run database schema migrations before the processing of blocks.
    #[arg(long, env = "CCDSCAN_INDEXER_MIGRATE")]
    migrate:           bool,
    /// Run database schema migrations only and then exit.
    /// In production it is recommended to use this for first running the
    /// migrations with elevated privileges.
    #[arg(long, env = "CCDSCAN_INDEXER_MIGRATE_ONLY")]
    migrate_only:      bool,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let _ = dotenvy::dotenv();
    let cli = Cli::parse();
    tracing_subscriber::fmt().with_max_level(cli.log_level).init();
    let pool = PgPoolOptions::new()
        .min_connections(cli.min_connections)
        .max_connections(cli.max_connections)
        .connect(&cli.database_url)
        .await
        .context("Failed constructing database connection pool")?;
    let cancel_token = CancellationToken::new();

    if cli.migrate || cli.migrate_only {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        let mut migration_task = tokio::spawn(migrations::run_migrations(pool, stop_signal));
        tokio::select! {
            _ = tokio::signal::ctrl_c() => {
                info!("Migrations aborted, shutting down");
                cancel_token.cancel();
                let _ = migration_task.await?;
                return Ok(())
            },
            result = &mut migration_task => {
                if let Err(err) = result? {
                    error!("Migration error: {}", err);
                    return Ok(())
                }
            }
        };
        if cli.migrate_only {
            return Ok(());
        }
    }
    migrations::ensure_latest_schema_version(&pool).await?;

    let mut registry = Registry::with_prefix("indexer");
    let service_info_family = Family::<Vec<(&str, String)>, Gauge>::default();
    let gauge =
        service_info_family.get_or_create(&vec![("version", clap::crate_version!().to_string())]);
    gauge.set(1);
    registry.register(
        "service_info",
        "Information about the software",
        service_info_family.clone(),
    );
    registry.register(
        "service_startup_timestamp_millis",
        "Timestamp of starting up the Indexer service (Unix time in milliseconds)",
        prometheus_client::metrics::gauge::ConstGauge::new(chrono::Utc::now().timestamp_millis()),
    );

    let mut indexer_task = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        let indexer =
            indexer::IndexerService::new(cli.node, pool, &mut registry, cli.indexer_config).await?;
        tokio::spawn(indexer.run(stop_signal))
    };
    let mut monitoring_task = {
        let tcp_listener = TcpListener::bind(cli.monitoring_listen)
            .await
            .context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Monitoring server is running at {:?}", cli.monitoring_listen);
        tokio::spawn(router::serve(registry, tcp_listener, pool, stop_signal))
    };
    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
            cancel_token.cancel();
        },
        result = &mut indexer_task => {
            error!("Indexer task stopped.");
            if let Err(err) = result? {
                error!("Indexer error: {}", err);
            }
            cancel_token.cancel();
        }
        result = &mut monitoring_task => {
            error!("Monitoring task stopped.");
            if let Err(err) = result? {
                error!("Monitoring error: {}", err);
            }
            cancel_token.cancel();
        }
    };
    let _ = tokio::join!(monitoring_task, indexer_task);
    Ok(())
}
