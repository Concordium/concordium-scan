use anyhow::Context;
use clap::Parser;
use concordium_rust_sdk::v2;
use concordium_scan::indexer;
use dotenv::dotenv;
use sqlx::PgPool;
use tokio_util::sync::CancellationToken;
use tracing::{
    error,
    info,
};

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

    let mut indexer_task = {
        let pool = pool.clone();
        let stop_signal = cancel_token.child_token();
        let indexer = indexer::CcdScanIndexer::new(cli.node, pool).await?;
        tokio::spawn(async move { indexer.run(stop_signal).await })
    };
    // Await for signal to shutdown or any of the tasks to stop.
    tokio::select! {
        _ = tokio::signal::ctrl_c() => {
            info!("Received signal to shutdown");
            cancel_token.cancel();
            let _ = indexer_task.await?;
        },
        result = &mut indexer_task => {
            error!("Indexer task stopped.");
            if let Err(err) = result? {
                error!("Indexer error: {}", err);
            }
        }
    }
    Ok(())
}
