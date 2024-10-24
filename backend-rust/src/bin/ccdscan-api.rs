use anyhow::Context;
use async_graphql::SDLExportOptions;
use clap::Parser;
use concordium_scan::{graphql_api, metrics};
use prometheus_client::registry::Registry;
use sqlx::PgPool;
use std::{net::SocketAddr, path::PathBuf};
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{error, info};

#[derive(Parser)]
struct Cli {
    /// The URL used for the database, something of the form:
    /// "postgres://postgres:example@localhost/ccd-scan".
    /// Use environmental variable when the connection contains a password, as
    /// command line arguments are visible across OS processes.
    #[arg(long, env = "DATABASE_URL")]
    database_url:   String,
    /// Output the GraphQL Schema for the API to this path.
    #[arg(long)]
    schema_out:     Option<PathBuf>,
    /// Address to listen for API requests
    #[arg(long, env = "CCDSCAN_API_ADDRESS", default_value = "127.0.0.1:8000")]
    listen:         SocketAddr,
    /// Address to listen for API requests
    #[arg(long, env = "CCDSCAN_API_METRICS_ADDRESS", default_value = "127.0.0.1:8003")]
    metrics_listen: SocketAddr,
    #[command(flatten, next_help_heading = "Configuration")]
    api_config:     graphql_api::ApiServiceConfig,
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

    let mut registry = Registry::with_prefix("api");
    registry.register(
        "service",
        "Information about the software",
        prometheus_client::metrics::info::Info::new(vec![("version", clap::crate_version!())]),
    );
    registry.register(
        "service_startup_timestamp_millis",
        "Timestamp of starting up the API service (Unix time in milliseconds)",
        prometheus_client::metrics::gauge::ConstGauge::new(chrono::Utc::now().timestamp_millis()),
    );

    let (subscription, subscription_listener) = graphql_api::Subscription::new();
    let mut pgnotify_listener = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        tokio::spawn(async move { subscription_listener.listen(pool, stop_signal).await })
    };

    let mut queries_task = {
        let service = graphql_api::Service::new(subscription, &mut registry, pool, cli.api_config);
        if let Some(schema_file) = cli.schema_out {
            info!("Writing schema to {}", schema_file.to_string_lossy());
            std::fs::write(
                schema_file,
                service
                    .schema
                    .sdl_with_options(SDLExportOptions::new().prefer_single_line_descriptions()),
            )
            .context("Failed to write schema")?;
        }
        let tcp_listener =
            TcpListener::bind(cli.listen).await.context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Server is running at {:?}", cli.listen);
        tokio::spawn(async move { service.serve(tcp_listener, stop_signal).await })
    };
    let mut metrics_task = {
        let tcp_listener = TcpListener::bind(cli.metrics_listen)
            .await
            .context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Metrics server is running at {:?}", cli.metrics_listen);
        tokio::spawn(metrics::serve(registry, tcp_listener, stop_signal))
    };

    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
            cancel_token.cancel();
            let _ = queries_task.await?;
            let _ = pgnotify_listener.await?;
            let _ = metrics_task.await?;
        },
        result = &mut queries_task => {
            error!("Queries task stopped.");
            if let Err(err) = result? {
                error!("Queries error: {}", err);
            }
            info!("Shutting down");
            cancel_token.cancel();
            let _ = pgnotify_listener.await?;
            let _ = metrics_task.await?;
        },
        result = &mut pgnotify_listener => {
            error!("Pgnotify listener task stopped.");
            if let Err(err) = result? {
                error!("Pgnotify listener task error: {}", err);
            }
            info!("Shutting down");
            cancel_token.cancel();
            let _ = queries_task.await?;
            let _ = metrics_task.await?;
        },
        result = &mut metrics_task => {
            error!("Metrics task stopped.");
            if let Err(err) = result? {
                error!("Metrics error: {}", err);
            }
            cancel_token.cancel();
            let _ = queries_task.await?;
            let _ = pgnotify_listener.await?;
        }
    }
    Ok(())
}
