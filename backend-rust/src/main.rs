use anyhow::Context;
use async_graphql::futures_util::lock::Mutex;
use async_graphql::http::GraphiQLSource;
use async_graphql::Schema;
use async_graphql_axum::GraphQL;
use axum::response::{self, IntoResponse};
use axum::{routing::get, Router};
use clap::Parser;
use concordium_rust_sdk::v2;
use dotenv::dotenv;
use sqlx::PgPool;
use std::sync::Arc;
use tokio::net::TcpListener;
use tokio::sync::mpsc;

mod graphql_api;
mod indexer;

pub async fn graphiql() -> impl IntoResponse {
    response::Html(GraphiQLSource::build().endpoint("/").finish())
}

#[derive(Parser)]
struct Cli {
    /// The url used for the database, something of the form "postgres://postgres:example@localhost/ccd-scan"
    #[arg(long, env = "DATABASE_URL")]
    database_url: String,

    /// GRPC interface of the node.
    #[arg(long, default_value = "http://localhost:20000")]
    node: Vec<v2::Endpoint>,

    /// Whether to run the indexer.
    #[arg(long)]
    indexer: bool,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    dotenv().ok();
    let cli = Cli::parse();

    tracing_subscriber::fmt()
        .with_max_level(tracing::Level::DEBUG)
        .init();

    let pool = PgPool::connect(&cli.database_url)
        .await
        .context("Failed constructin database connection pool")?;

    if cli.indexer {
        println!("Starting indexer");
        let (sender, receiver) = mpsc::channel(10);
        {
            let pool = pool.clone();
            tokio::spawn(async move {
                indexer::traverse_chain(cli.node, pool, sender)
                    .await
                    .expect("failed")
            });
        }
        {
            let pool = pool.clone();
            tokio::spawn(
                async move { indexer::save_blocks(receiver, pool).await.expect("failed") },
            );
        }
    }

    let schema = Schema::build(
        graphql_api::Query,
        async_graphql::EmptyMutation,
        async_graphql::EmptySubscription,
    )
    .extension(async_graphql::extensions::Tracing)
    .data(pool)
    .finish();

    println!("Schema: \n{}", schema.sdl());

    let app = Router::new().route("/", get(graphiql).post_service(GraphQL::new(schema)));

    println!("Server is running at http://localhost:8000");
    axum::serve(
        TcpListener::bind("127.0.0.1:8000")
            .await
            .context("Parsing TCP listener address failed")?,
        app,
    )
    .await
    .context("Server failed")?;

    Ok(())
}
