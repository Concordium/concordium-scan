use anyhow::Context;
use axum::{http::StatusCode, Json};
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
use serde_json::json;
use sqlx::{postgres::PgConnectOptions, Connection as _};
use std::{net::SocketAddr, path::PathBuf, time::Duration};
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{error, info};
use tracing_subscriber::{layer::SubscriberExt as _, util::SubscriberInitExt as _};

#[derive(Parser)]
#[command(version, author, about)]
struct Cli {
    /// The URL used for the database, something of the form
    /// "postgres://postgres:example@localhost/ccd-scan".
    /// Use an environment variable when the connection contains a password, as
    /// command line arguments are visible across OS processes.
    #[arg(long, env = "CCDSCAN_INDEXER_DATABASE_URL")]
    database_url: PgConnectOptions,
    /// gRPC interface of the node. Several can be provided.
    #[arg(
        long,
        env = "CCDSCAN_INDEXER_GRPC_ENDPOINTS",
        value_delimiter = ',',
        num_args = 1..
    )]
    node: Vec<v2::Endpoint>,
    /// Address to listen for monitoring related requests
    #[arg(long, env = "CCDSCAN_INDEXER_MONITORING_ADDRESS", default_value = "127.0.0.1:8001")]
    monitoring_listen: SocketAddr,
    #[command(flatten)]
    indexer_config: IndexerServiceConfig,
    /// The maximum log level. Possible values are: `trace`, `debug`, `info`,
    /// `warn`, and `error`.
    #[arg(long = "log-level", default_value = "info", env = "LOG_LEVEL")]
    log_level: tracing_subscriber::filter::LevelFilter,
    /// Run database schema migrations before the processing of blocks.
    #[arg(long, env = "CCDSCAN_INDEXER_MIGRATE")]
    migrate: bool,
    /// Run database schema migrations only and then exit.
    /// In production it is recommended to use this for first running the
    /// migrations with elevated privileges.
    #[arg(long, env = "CCDSCAN_INDEXER_MIGRATE_ONLY")]
    migrate_only: bool,
    /// Provide file to load environment variables from, instead of the default
    /// `.env`.
    // This is only part of this struct in order to generate help information.
    // This argument is actually handled before hand using `DotenvCli`.
    #[arg(long)]
    dotenv: Option<PathBuf>,
    /// How often to recompute staked amounts for validators with the node.
    /// Denotes the number of blocks to allow pass before updating stakes.
    #[arg(long, env = "STAKE_RECOMPUTE_INTERVAL_IN_BLOCKS", default_value = "500")]
    stake_recompute_every_x_blocks: u64,
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
    // Parse CLI args and env variables
    if let Some(dotenv) = DotenvCli::parse().dotenv {
        dotenvy::from_filename(dotenv)?;
    } else {
        let _ = dotenvy::dotenv();
    }
    let cli = Cli::parse();
    // Setup filter for the logging
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
    // Handle TLS configuration and set timeouts according to the configuration for
    // every endpoint.
    let endpoints: Vec<v2::Endpoint> = cli
        .node
        .into_iter()
        .map(|mut endpoint| {
            // Enable TLS when using HTTPS
            if endpoint.uri().scheme().is_some_and(|x| x == &concordium_rust_sdk::v2::Scheme::HTTPS)
            {
                endpoint = endpoint
                    .tls_config(tonic::transport::ClientTlsConfig::new())
                    .context("Unable to construct TLS configuration for the Concordium node.")?
            }
            // Enable rate limit per second.
            if let Some(limit) = cli.indexer_config.node_request_rate_limit {
                endpoint = endpoint.rate_limit(limit, Duration::from_secs(1))
            }
            // Enable concurrency limit per connection.
            if let Some(concurrency) = cli.indexer_config.node_request_concurrency_limit {
                endpoint = endpoint.concurrency_limit(concurrency)
            }
            Ok(endpoint
                .timeout(Duration::from_secs(cli.indexer_config.node_request_timeout))
                .connect_timeout(Duration::from_secs(cli.indexer_config.node_connect_timeout)))
        })
        .collect::<anyhow::Result<_>>()?;
    // Open database connection
    let mut db_connection = sqlx::PgConnection::connect_with(&cli.database_url)
        .await
        .context("Failed establishing the database connection")?;
    // Acquire the indexer lock
    let database_indexer_lock_timeout =
        Duration::from_secs(cli.indexer_config.database_indexer_lock_timeout);
    tokio::time::timeout(
        database_indexer_lock_timeout,
        indexer::acquire_indexer_lock(db_connection.as_mut()),
    )
    .await
    .context(
        "Acquire indexer lock timed out, another instance of ccdscan-indexer might already be \
         running",
    )??;
    // Run migrations if allowed
    let cancel_token = CancellationToken::new();
    if cli.migrate || cli.migrate_only {
        let endpoints = endpoints.clone();
        let migration_task = cancel_token
            .run_until_cancelled(migrations::run_migrations(&mut db_connection, endpoints));
        tokio::select! {
            _ = tokio::signal::ctrl_c() => {
                info!("Migrations aborted, shutting down");
                cancel_token.cancel();
                return Ok(())
            },
            result = migration_task => {
                if let Err(err) = result.transpose() {
                    return Err(anyhow::format_err!("Migration error: {}", err))
                }
            }
        };
        if cli.migrate_only {
            return Ok(());
        }
    }
    migrations::ensure_latest_schema_version(&mut db_connection).await?;
    // Setup information in the metric registry
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
    // Setup and run the services
    let mut indexer_task = {
        let stop_signal = cancel_token.child_token();
        let indexer = indexer::IndexerService::new(
            endpoints,
            cli.database_url.clone(),
            db_connection,
            &mut registry,
            cli.indexer_config,
        )
        .await?;
        tokio::spawn(indexer.run(stop_signal))
    };
    let mut monitoring_task = {
        let health_routes =
            axum::Router::new().route("/", axum::routing::get(health)).with_state(cli.database_url);
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

/// GET Handler for route `/health`.
/// Verifying the indexer service state is as expected.
async fn health(
    axum::extract::State(db_connect_options): axum::extract::State<PgConnectOptions>,
) -> (StatusCode, Json<serde_json::Value>) {
    if check_health(&db_connect_options).await.is_ok() {
        (
            StatusCode::OK,
            Json(json!({
                "database_status": "connected",
            })),
        )
    } else {
        (
            StatusCode::INTERNAL_SERVER_ERROR,
            Json(json!({
                "database_status": "not connected",
            })),
        )
    }
}

/// Function verifying the indexer health, returns Ok if healthy otherwise an
/// Err.
async fn check_health(db_connect_options: &PgConnectOptions) -> anyhow::Result<()> {
    let mut db_connection = sqlx::PgConnection::connect_with(db_connect_options).await?;
    migrations::ensure_latest_schema_version(&mut db_connection).await?;
    Ok(())
}
