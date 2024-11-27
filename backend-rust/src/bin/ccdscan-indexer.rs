use anyhow::Context;
use clap::Parser;
use concordium_rust_sdk::v2;
use concordium_scan::{
    indexer::{self, IndexerServiceConfig},
    router,
};
use prometheus_client::registry::Registry;
use sqlx::PgPool;
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
    #[arg(long, env = "DATABASE_URL")]
    database_url:   String,
    /// gRPC interface of the node. Several can be provided.
    #[arg(
        long,
        env = "CCDSCAN_INDEXER_GRPC_ENDPOINTS",
        value_delimiter = ',',
        num_args = 1..,
        default_value = "http://localhost:20000"
    )]
    node:           Vec<v2::Endpoint>,
    /// Address to listen for metrics requests
    #[arg(long, env = "CCDSCAN_INDEXER_METRICS_ADDRESS", default_value = "127.0.0.1:8001")]
    metrics_listen: SocketAddr,
    #[command(flatten, next_help_heading = "Performance tuning")]
    indexer_config: IndexerServiceConfig,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let _ = dotenvy::dotenv();
    let cli = Cli::parse();
    tracing_subscriber::fmt().with_max_level(tracing::Level::INFO).init();
    let pool = PgPool::connect(&cli.database_url)
        .await
        .context("Failed constructing database connection pool")?;
    let cancel_token = CancellationToken::new();

    let mut registry = Registry::with_prefix("indexer");
    registry.register(
        "service",
        "Information about the software",
        prometheus_client::metrics::info::Info::new(vec![("version", clap::crate_version!())]),
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
        tokio::spawn(async move { indexer.run(stop_signal).await })
    };
    let mut metrics_task = {
        let pool = pool.clone();
        let tcp_listener = TcpListener::bind(cli.metrics_listen)
            .await
            .context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Metrics server is running at {:?}", cli.metrics_listen);
        tokio::spawn(router::serve(registry, tcp_listener, pool, stop_signal))
    };
    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
            cancel_token.cancel();
            let _ = indexer_task.await?;
            let _ = metrics_task.await?;
        },
        result = &mut indexer_task => {
            error!("Indexer task stopped.");
            if let Err(err) = result? {
                error!("Indexer error: {}", err);
            }
            cancel_token.cancel();
            let _ = metrics_task.await?;
        }
        result = &mut metrics_task => {
            error!("Metrics task stopped.");
            if let Err(err) = result? {
                error!("Metrics error: {}", err);
            }
            cancel_token.cancel();
            let _ = indexer_task.await?;
        }
    };
    Ok(())
}
