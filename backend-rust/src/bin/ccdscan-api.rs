use anyhow::Context;
use axum::{http::StatusCode, Json};
use clap::Parser;
use concordium_scan::{
    graphql_api::{self, node_status::NodeInfoReceiver},
    migrations,
    migrations::SchemaVersion,
    rest_api, router,
};
use prometheus_client::{
    metrics::{family::Family, gauge::Gauge},
    registry::Registry,
};
use reqwest::Client;
use serde_json::json;
use sqlx::postgres::{PgConnectOptions, PgPoolOptions};
use std::{net::SocketAddr, path::PathBuf, sync::Arc, time::Duration};
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{error, info};
use tracing_subscriber::{layer::SubscriberExt as _, util::SubscriberInitExt as _};

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
    /// Database statement timeout. Abort any statement that takes more than the
    /// specified amount of time. Set to 0 to disable.
    #[arg(long, env = "CCDSCAN_API_DATABASE_STATEMENT_TIMEOUT_SECS", default_value_t = 30)]
    statement_timeout_secs: u64,
    /// Outputs the GraphQL Schema for the API and then exits. The output is
    /// stored as a file at the provided path or to stdout when '-' is
    /// provided.
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
    /// Origin to the node collector backend. This URL is used to fetch node
    /// status data.
    #[arg(long, env = "CCDSCAN_API_NODE_COLLECTOR_BACKEND_ORIGIN")]
    node_collector_backend_origin: String,
    /// Frequency in seconds in between each poll from the node collector
    /// backend
    #[arg(long, env = "CCDSCAN_API_NODE_COLLECTOR_PULL_FREQUENCY_SECS", default_value_t = 5)]
    node_collector_backend_pull_frequency_secs: u64,
    /// Request timeout when awaiting response from the node collector backend
    /// in seconds.
    #[arg(long, env = "CCDSCAN_API_NODE_COLLECTOR_CLIENT_TIMEOUT_SECS", default_value_t = 30)]
    node_collector_timeout_secs: u64,
    /// Request connection timeout to the node collector backend in seconds.
    #[arg(
        long,
        env = "CCDSCAN_API_NODE_COLLECTOR_CLIENT_CONNECTION_TIMEOUT_SECS",
        default_value_t = 5
    )]
    node_collector_connection_timeout_secs: u64,
    /// Defines the maximum allowed content length (in bytes) for responses from
    /// the node collector backend
    #[arg(
        long,
        env = "CCDSCAN_API_NODE_COLLECTOR_CONNECTION_MAX_CONTENT_LENGTH",
        default_value_t = 4000000
    )]
    node_collector_connection_max_content_length: u64,
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
    let filter = if std::env::var("RUST_LOG").is_ok() {
        // If RUST_LOG env is defined we fallback to the default behavior of the env
        // filter.
        tracing_subscriber::EnvFilter::builder().from_env_lossy()
    } else {
        // If RUST_LOG env is not defined, set the --log-level only for this project and
        // leave dependencies filter to info level.
        let pkg_name = env!("CARGO_PKG_NAME").replace('-', "_");
        let crate_name = env!("CARGO_CRATE_NAME");
        format!("info,{pkg_name}={0},{crate_name}={0}", cli.log_level).parse()?
    };
    tracing_subscriber::registry().with(tracing_subscriber::fmt::layer()).with(filter).init();
    if let Some(schema_file) = cli.schema_out {
        let sdl = graphql_api::Service::sdl();
        if schema_file.as_path() == std::path::Path::new("-") {
            eprintln!("Writing schema to stdout");
            print!("{}", sdl);
        } else {
            eprintln!("Writing schema to {}", schema_file.to_string_lossy());
            std::fs::write(schema_file, sdl).context("Failed to write schema")?;
        }
        return Ok(());
    }

    let connection_options: PgConnectOptions = cli.database_url.parse()?;
    let pool = PgPoolOptions::new()
        .min_connections(cli.min_connections)
        .max_connections(cli.max_connections)
        .connect_with(
            connection_options
                .options([("statement_timeout", format!("{}s", cli.statement_timeout_secs))]),
        )
        .await
        .context("Failed constructing database connection pool")?;
    // Ensure the database schema is compatible with supported schema version.
    migrations::ensure_compatible_schema_version(
        &pool,
        SchemaVersion::API_SUPPORTED_SCHEMA_VERSION,
    )
    .await?;
    if cli.check_database_compatibility_only {
        // Exit if we only care about the compatibility.
        return Ok(());
    }

    let client = Client::builder()
        .connect_timeout(Duration::from_secs(cli.node_collector_connection_timeout_secs))
        .timeout(Duration::from_secs(cli.node_collector_timeout_secs))
        .build()?;

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
    let (nodes_status_sender, nodes_status_receiver) = tokio::sync::watch::channel(None);
    let node_status_receiver = nodes_status_receiver.clone();
    let mut pgnotify_listener = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        tokio::spawn(async move { subscription_listener.listen(pool, stop_signal).await })
    };

    let mut queries_task = {
        let config = Arc::new(cli.api_config);
        let graphql_service = graphql_api::Service::new(
            subscription,
            &mut registry,
            pool.clone(),
            config.clone(),
            nodes_status_receiver,
        );
        let rest_service = rest_api::Service::new(pool.clone(), config);
        let tcp_listener =
            TcpListener::bind(cli.listen).await.context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Server is running at {:?}", cli.listen);
        tokio::spawn(async move {
            axum::serve(
                tcp_listener,
                axum::Router::new()
                    .merge(graphql_service.as_router())
                    .merge(rest_service.as_router()),
            )
            .with_graceful_shutdown(stop_signal.cancelled_owned())
            .await
        })
    };
    let mut node_collector_task = {
        let stop_signal = cancel_token.child_token();
        let service = graphql_api::node_status::Service::new(
            nodes_status_sender,
            &cli.node_collector_backend_origin,
            Duration::from_secs(cli.node_collector_backend_pull_frequency_secs),
            client,
            cli.node_collector_connection_max_content_length,
            stop_signal,
            &mut registry,
        );
        tokio::spawn(service.serve())
    };
    let mut monitoring_task = {
        let state = HealthState {
            pool,
            node_status_receiver,
        };
        let health_routes =
            axum::Router::new().route("/", axum::routing::get(health)).with_state(state);
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
        result = &mut node_collector_task => {
            error!("Node collector task stopped.");
            if let Err(err) = result? {
                error!("Node collector error: {}", err);
            }
            cancel_token.cancel();
        }
    }
    info!("Shutting down");
    // Ensure all tasks have stopped
    let _ = tokio::join!(monitoring_task, queries_task, pgnotify_listener, node_collector_task);
    Ok(())
}

/// Represents the state required by the health endpoint router.
///
/// This struct provides access to essential resources needed to determine
/// system health and readiness.
#[derive(Clone)]
struct HealthState {
    pool:                 sqlx::PgPool,
    node_status_receiver: NodeInfoReceiver,
}

/// GET Handler for route `/health`.
/// Verifying the API service state is as expected.
async fn health(
    axum::extract::State(state): axum::extract::State<HealthState>,
) -> (StatusCode, Json<serde_json::Value>) {
    let node_status_connected = state.node_status_receiver.borrow().is_some();
    let database_connected = migrations::ensure_compatible_schema_version(
        &state.pool,
        SchemaVersion::API_SUPPORTED_SCHEMA_VERSION,
    )
    .await
    .is_ok();

    let is_healthy = node_status_connected && database_connected;

    let status_code = if is_healthy {
        StatusCode::OK
    } else {
        StatusCode::INTERNAL_SERVER_ERROR
    };
    (
        status_code,
        Json(json!({
            "node_status": if node_status_connected {"connected"} else {"not connected"},
            "database_status": if database_connected {"connected"} else {"not connected"},
        })),
    )
}
