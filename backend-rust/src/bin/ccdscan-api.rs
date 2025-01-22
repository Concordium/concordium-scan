use anyhow::Context;
use async_graphql::SDLExportOptions;
use clap::Parser;
use concordium_rust_sdk::{
    types::ProtocolVersion,
    v2::{self, BlockIdentifier, ChainParameters},
};
use concordium_scan::{
    graphql_api::{self, MemoryState, ServiceConfig},
    router,
};
use prometheus_client::{
    metrics::{family::Family, gauge::Gauge},
    registry::Registry,
};
use sqlx::postgres::PgPoolOptions;
use std::{net::SocketAddr, path::PathBuf, time::Duration};
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{error, info};

#[derive(Parser)]
struct Cli {
    /// The URL used for the database, something of the form:
    /// "postgres://postgres:example@localhost/ccd-scan".
    /// Use an environment variable when the connection contains a password, as
    /// command line arguments are visible across OS processes.
    #[arg(long, env = "DATABASE_URL")]
    database_url: String,
    #[arg(long, env = "DATABASE_RETRY_DELAY_SECS", default_value_t = 5)]
    database_retry_delay_secs: u64,
    /// Minimum number of connections in the pool.
    #[arg(long, env = "DATABASE_MIN_CONNECTIONS", default_value_t = 5)]
    min_connections: u32,
    /// Maximum number of connections in the pool.
    #[arg(long, env = "DATABASE_MAX_CONNECTIONS", default_value_t = 10)]
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
    #[arg(
        long = "log-level",
        default_value = "info",
        help = "The maximum log level. Possible values are: `trace`, `debug`, `info`, `warn`, and \
                `error`.",
        env = "LOG_LEVEL"
    )]
    log_level: tracing_subscriber::filter::LevelFilter,
    /// Concordium node URL to a caught-up node.
    #[arg(long, env = "CCDSCAN_API_GRPC_ENDPOINT", default_value = "http://localhost:20000")]
    node: v2::Endpoint,
    /// Request timeout in seconds when querying a Concordium Node.
    #[arg(long, env = "CCDSCAN_API_NODE_REQUEST_TIMEOUT", default_value = "60")]
    node_request_timeout: u64,
    /// Connection timeout in seconds when connecting a Concordium Node.
    #[arg(long, env = "CCDSCAN_API_NODE_CONNECT_TIMEOUT", default_value = "10")]
    node_connect_timeout: u64,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let _ = dotenvy::dotenv();
    let cli = Cli::parse();

    let endpoint = if cli
        .node
        .uri()
        .scheme()
        .map_or(false, |x| x == &concordium_rust_sdk::v2::Scheme::HTTPS)
    {
        cli.node
            .clone()
            .tls_config(tonic::transport::ClientTlsConfig::new())
            .context("Unable to construct TLS configuration for the Concordium node.")?
    } else {
        cli.node.clone()
    };
    let endpoint: v2::Endpoint = endpoint
        .timeout(Duration::from_secs(cli.node_request_timeout))
        .connect_timeout(Duration::from_secs(cli.node_connect_timeout));

    let mut client = v2::Client::new(endpoint).await?;
    // Get the current block.
    let current_block = client.get_block_info(BlockIdentifier::LastFinal).await?.response;
    // We ensure that the connected node has caught up with the current protocol
    // version 8. This ensures that the parameters `current_epoch_duration` and
    // `current_reward_period_length` are available.
    if current_block.protocol_version < ProtocolVersion::P8 {
        anyhow::bail!(
            "Ensure the connected node has caught up with the current protocol version 8. 
            This ensures that the `current_epoch_duration` and `current_reward_period_length` are \
             available to be queried"
        );
    }

    // Get the current `epoch_duration` value.
    let current_epoch_duration = client.get_consensus_info().await?.epoch_duration;

    // Get the current `reward_period_length` value.
    let current_chain_parmeters =
        client.get_block_chain_parameters(BlockIdentifier::LastFinal).await?.response;
    let current_reward_period_length = match current_chain_parmeters {
        ChainParameters::V3(chain_parameters_v3) => {
            chain_parameters_v3.time_parameters.reward_period_length
        }
        ChainParameters::V2(chain_parameters_v2) => {
            chain_parameters_v2.time_parameters.reward_period_length
        }
        ChainParameters::V1(chain_parameters_v1) => {
            chain_parameters_v1.time_parameters.reward_period_length
        }
        _ => todo!(
            "Expect the chain to have caught up enought for the `reward_period_length` value \
             being available."
        ),
    };

    let memory_state = MemoryState {
        current_epoch_duration,
        current_reward_period_length,
    };

    tracing_subscriber::fmt().with_max_level(cli.log_level).init();
    let pool = PgPoolOptions::new()
        .min_connections(cli.min_connections)
        .max_connections(cli.max_connections)
        .connect(&cli.database_url)
        .await
        .context("Failed constructing database connection pool")?;
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
        let service = graphql_api::Service::new(subscription, &mut registry, pool, ServiceConfig {
            api_config: cli.api_config,
            memory_state,
        });
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
