use anyhow::Context;
use clap::Parser;
use concordium_rust_sdk::v2;
use concordium_scan::{
    indexer::{self, IndexerServiceConfig},
    router,
};
use sqlx::postgres::PgPoolOptions;
use std::net::SocketAddr;
use axum_prometheus::metrics::gauge;
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
    #[arg(long, env = "DATABASE_URL")]
    database_url:      String,
    /// Minimum number of connections in the pool.
    #[arg(long, env = "DATABASE_MIN_CONNECTIONS", default_value_t = 5)]
    min_connections:   u32,
    /// Maximum number of connections in the pool.
    #[arg(long, env = "DATABASE_MAX_CONNECTIONS", default_value_t = 10)]
    max_connections:   u32,
    /// gRPC interface of the node. Several can be provided.
    #[arg(
        long,
        env = "CCDSCAN_INDEXER_GRPC_ENDPOINTS",
        value_delimiter = ',',
        num_args = 1..,
        default_value = "http://localhost:20000"
    )]
    node:              Vec<v2::Endpoint>,
    /// Address to listen for monitoring related requests
    #[arg(long, env = "CCDSCAN_INDEXER_MONITORING_ADDRESS", default_value = "127.0.0.1:8001")]
    monitoring_listen: SocketAddr,
    #[command(flatten, next_help_heading = "Performance tuning")]
    indexer_config:    IndexerServiceConfig,
    #[arg(
        long = "log-level",
        default_value = "info",
        help = "The maximum log level. Possible values are: `trace`, `debug`, `info`, `warn`, and \
                `error`.",
        env = "LOG_LEVEL"
    )]
    log_level:         tracing_subscriber::filter::LevelFilter,
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

    let mut indexer_task = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        let indexer =
            indexer::IndexerService::new(cli.node, pool, cli.indexer_config).await?;
        tokio::spawn(async move { indexer.run(stop_signal).await })
    };
    let mut monitoring_task = {
        let pool = pool.clone();
        let tcp_listener = TcpListener::bind(cli.monitoring_listen)
            .await
            .context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Monitoring server is running at {:?}", cli.monitoring_listen);
        tokio::spawn(router::serve(tcp_listener, pool, stop_signal, "indexer".to_string()))
    };
    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
            cancel_token.cancel();
            let _ = indexer_task.await?;
            let _ = monitoring_task.await?;
        },
        result = &mut indexer_task => {
            error!("Indexer task stopped.");
            if let Err(err) = result? {
                error!("Indexer error: {}", err);
            }
            cancel_token.cancel();
            let _ = monitoring_task.await?;
        }
        result = &mut monitoring_task => {
            error!("Monitoring task stopped.");
            if let Err(err) = result? {
                error!("Monitoring error: {}", err);
            }
            cancel_token.cancel();
            let _ = indexer_task.await?;
        }
    };
    Ok(())
}
