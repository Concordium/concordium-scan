use anyhow::Context;
use async_graphql::SDLExportOptions;
use clap::Parser;
use concordium_scan::{graphql_api, migrations, router};
use prometheus_client::{
    metrics::{family::Family, gauge::Gauge},
    registry::Registry,
};
use serde_json::json;
use sqlx::postgres::PgPoolOptions;
use std::{net::SocketAddr, path::PathBuf};
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{error, info};

/// The known supported database schema version for the API.
const SUPPORTED_SCHEMA_VERSION: migrations::SchemaVersion =
    migrations::SchemaVersion::InitialFirstHalf;

#[derive(Parser)]
struct Cli {
    /// The URL used for the database, something of the form:
    /// "postgres://postgres:example@localhost/ccd-scan".
    /// Use an environment variable when the connection contains a password, as
    /// command line arguments are visible across OS processes.
    #[arg(long, env = "CCDSCAN_API_DATABASE_URL")]
    database_url: String,
    #[arg(long, env = "CCDSCAN_API_DATABASE_RETRY_DELAY_SECS", default_value_t = 5)]
    database_retry_delay_secs: u64,
    /// Minimum number of connections in the pool.
    #[arg(long, env = "CCDSCAN_API_DATABASE_MIN_CONNECTIONS", default_value_t = 5)]
    min_connections: u32,
    /// Maximum number of connections in the pool.
    #[arg(long, env = "CCDSCAN_API_DATABASE_MAX_CONNECTIONS", default_value_t = 10)]
    max_connections: u32,
    /// Output the GraphQL Schema for the API to this path.
    #[arg(long)]
    schema_out: Option<PathBuf>,
    /// Address to listen to for API requests.
    #[arg(long, env = "CCDSCAN_API_ADDRESS", default_value = "127.0.0.1:8000")]
    listen: SocketAddr,
    /// Address to listen for monitoring related requests
    #[arg(long, env = "CCDSCAN_API_MONITORING_ADDRESS", default_value = "127.0.0.1:8003")]
    monitoring_listen: SocketAddr,
    #[command(flatten, next_help_heading = "Configuration")]
    api_config: graphql_api::ApiServiceConfig,
    /// The maximum log level. Possible values are: `trace`, `debug`, `info`,
    /// `warn`, and `error`.
    #[arg(long, default_value = "info", env = "LOG_LEVEL")]
    log_level: tracing_subscriber::filter::LevelFilter,
    /// Check whether the database schema version is compatible with this
    /// version of the service and then exit the service immediately.
    /// Non-zero exit code is returned when incompatible.
    #[arg(long, env = "CCDSCAN_API_CHECK_DATABASE_COMPATIBILITY_ONLY")]
    check_database_compatibility_only: bool,
    /// Provide file to load environment variables from, instead of the default
    /// `.env`.
    // This is only part of this struct in order to generate help information.
    // This argument is actually handled before hand using `DotenvCli`.
    #[arg(long)]
    dotenv: Option<PathBuf>,
}

/// CLI argument parser first used for parsing only the --dotenv option.
/// Allowing loading the provided file before parsing the remaining arguments
/// and producing errors
#[derive(Parser)]
#[command(ignore_errors = true, disable_help_flag = true, disable_version_flag = true)]
struct DotenvCli {
    #[arg(long)]
    dotenv: Option<PathBuf>,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    if let Some(dotenv) = DotenvCli::parse().dotenv {
        dotenvy::from_filename(dotenv)?;
    } else {
        let _ = dotenvy::dotenv();
    }
    let cli = Cli::parse();
    tracing_subscriber::fmt().with_max_level(cli.log_level).init();
    let pool = PgPoolOptions::new()
        .min_connections(cli.min_connections)
        .max_connections(cli.max_connections)
        .connect(&cli.database_url)
        .await
        .context("Failed constructing database connection pool")?;
    // Ensure the database schema is compatible with supported schema version.
    migrations::ensure_compatible_schema_version(&pool, SUPPORTED_SCHEMA_VERSION).await?;
    if cli.check_database_compatibility_only {
        // Exit if we only care about the compatibility.
        return Ok(());
    }

    let cancel_token = CancellationToken::new();
    let service_info_family = Family::<Vec<(&str, String)>, Gauge>::default();
    let gauge =
        service_info_family.get_or_create(&vec![("version", clap::crate_version!().to_string())]);
    gauge.set(1);
    let mut registry = Registry::with_prefix("api");
    registry.register(
        "service_info",
        "Information about the software",
        service_info_family.clone(),
    );
    registry.register(
        "service_startup_timestamp_millis",
        "Timestamp of starting up the API service (Unix time in milliseconds)",
        prometheus_client::metrics::gauge::ConstGauge::new(chrono::Utc::now().timestamp_millis()),
    );

    let (subscription, subscription_listener) =
        graphql_api::Subscription::new(cli.database_retry_delay_secs);
    let mut pgnotify_listener = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        tokio::spawn(async move { subscription_listener.listen(pool, stop_signal).await })
    };

    let mut queries_task = {
        let pool = pool.clone();
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
    let mut monitoring_task = {
        let health_routes =
            axum::Router::new().route("/", axum::routing::get(health)).with_state(pool);
        let tcp_listener = TcpListener::bind(cli.monitoring_listen)
            .await
            .context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Monitoring server is running at {:?}", cli.monitoring_listen);
        tokio::spawn(router::serve(registry, tcp_listener, stop_signal, health_routes))
    };

    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
            cancel_token.cancel();
        },
        result = &mut queries_task => {
            error!("Queries task stopped.");
            if let Err(err) = result? {
                error!("Queries error: {}", err);
            }
            cancel_token.cancel();
        }
        result = &mut pgnotify_listener => {
            error!("Pgnotify listener task stopped.");
            if let Err(err) = result? {
                error!("Pgnotify listener task error: {}", err);
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
    }
    info!("Shutting down");
    // Ensure all tasks have stopped
    let _ = tokio::join!(monitoring_task, queries_task, pgnotify_listener);
    Ok(())
}

/// GET Handler for route `/health`.
/// Verifying the API service state is as expected.
async fn health(
    axum::extract::State(pool): axum::extract::State<sqlx::PgPool>,
) -> axum::Json<serde_json::Value> {
    match migrations::ensure_compatible_schema_version(&pool, SUPPORTED_SCHEMA_VERSION).await {
        Ok(_) => axum::extract::Json(json!({
            "status": "ok",
            "database": "connected"
        })),
        Err(err) => axum::extract::Json(json!({
            "status": "error",
            "database": format!("not connected: {}", err)
        })),
    }
}
