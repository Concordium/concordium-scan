use anyhow::Context;
use clap::Parser;
use concordium_rust_sdk::v2;
use dotenv::dotenv;
use futures::future::OptionFuture;
use sqlx::PgPool;
use std::{
    net::SocketAddr,
    path::PathBuf,
};
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;
use tracing::{
    error,
    info,
    warn,
};

mod graphql_api;
mod indexer;

// TODO add env for remaining args.
#[derive(Parser)]
struct Cli {
    /// The URL used for the database, something of the form
    /// "postgres://postgres:example@localhost/ccd-scan"
    #[arg(long, env = "DATABASE_URL")]
    database_url: String,
    /// gRPC interface of the node. Several can be provided.
    #[arg(long, default_value = "http://localhost:20000")]
    node: Vec<v2::Endpoint>,
    /// Whether to run the indexer.
    #[arg(long)]
    indexer: bool,
    /// Output the GraphQL Schema for the API to this path.
    #[arg(long)]
    schema_out: Option<PathBuf>,
    /// Address to listen for API requests
    #[arg(long, default_value = "127.0.0.1:8000")]
    listen: SocketAddr,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    dotenv().ok();
    let cli = Cli::parse();
    tracing_subscriber::fmt()
        .with_max_level(tracing::Level::INFO)
        .init();
    let pool = PgPool::connect(&cli.database_url)
        .await
        .context("Failed constructing database connection pool")?;
    let cancel_token = CancellationToken::new();

    let mut indexer_task = if cli.indexer {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        let indexer = indexer::CcdScanIndexer::new(cli.node, pool).await?;
        let task = tokio::spawn(async move { indexer.run(stop_signal).await });
        OptionFuture::from(Some(task))
    } else {
        OptionFuture::from(None)
    };

    let (subscription, subscription_listener) = graphql_api::Subscription::new();
    let mut subscription_task = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        tokio::spawn(async move { subscription_listener.listen(pool, stop_signal).await })
    };

    let mut queries_task = {
        let service = graphql_api::Service::new(subscription, pool);
        if let Some(schema_file) = cli.schema_out {
            info!("Writing schema to {}", schema_file.to_string_lossy());
            std::fs::write(schema_file, service.schema.sdl()).context("Failed to write schema")?;
        }
        let tcp_listener = TcpListener::bind(cli.listen)
            .await
            .context("Parsing TCP listener address failed")?;
        let stop_signal = cancel_token.child_token();
        info!("Server is running at {:?}", cli.listen);
        tokio::spawn(async move { service.serve(tcp_listener, stop_signal).await })
    };

    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
        },
        Some(result) = &mut indexer_task => {
            error!("Indexer task stopped.");
            if let Err(err) = result? {
                error!("Indexer error: {}", err);
            }
        },
        result = &mut queries_task => {
            error!("Queries task stopped.");
            if let Err(err) = result? {
                error!("Queries error: {}", err);
            }
        },
        result = &mut subscription_task => {
            error!("Subscription task stopped.");
            if let Err(err) = result? {
                error!("Subscription error: {}", err);
            }
        },
    }
    info!("Shutting down");
    cancel_token.cancel();
    let _ = indexer_task.await.transpose()?;
    let _ = queries_task.await?;
    let _ = subscription_task.await?;
    Ok(())
}
