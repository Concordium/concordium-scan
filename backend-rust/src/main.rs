use anyhow::Context;
use async_graphql::{
    http::GraphiQLSource,
    Schema,
};
use async_graphql_axum::{
    GraphQL,
    GraphQLSubscription,
};
use axum::{
    response::{
        self,
        IntoResponse,
    },
    routing::get,
    Router,
};
use clap::Parser;
use concordium_rust_sdk::v2;
use dotenv::dotenv;
use sqlx::PgPool;
use std::path::PathBuf;
use tokio::{
    net::TcpListener,
    sync::mpsc,
};

mod graphql_api;
mod indexer;

pub async fn graphiql() -> impl IntoResponse {
    response::Html(
        GraphiQLSource::build()
            .endpoint("/")
            .subscription_endpoint("/ws")
            .finish(),
    )
}

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

    if cli.indexer {
        eprintln!("Starting indexer");
        let last_height_stored = sqlx::query!(
            r#"
SELECT height FROM blocks ORDER BY height DESC LIMIT 1
"#
        )
        .fetch_optional(&pool)
        .await?
        .map(|r| r.height);

        let start_height = if let Some(height) = last_height_stored {
            u64::try_from(height)? + 1
        } else {
            indexer::save_genesis_data(cli.node[0].clone(), &pool).await?;
            1
        };

        let (sender, receiver) = mpsc::channel(10);
        {
            // TODO: Graceful shutdown if this task fails
            tokio::spawn(async move {
                indexer::traverse_chain(cli.node, sender, start_height.into())
                    .await
                    .expect("failed")
            });
        }
        {
            let pool = pool.clone();
            // TODO: Graceful shutdown if this task fails
            tokio::spawn(
                async move { indexer::save_blocks(receiver, pool).await.expect("failed") },
            );
        }
    }

    let (subscription, subscription_context) = graphql_api::Subscription::new();
    {
        let pool = pool.clone();
        // TODO: Graceful shutdown if this task fails
        tokio::spawn(async move {
            graphql_api::Subscription::handle_notifications(subscription_context, pool)
                .await
                .expect("PostgreSQL notification task failed")
        });
    }

    let schema = Schema::build(
        graphql_api::Query,
        async_graphql::EmptyMutation,
        subscription,
    )
    .extension(async_graphql::extensions::Tracing)
    .data(pool)
    .finish();

    if let Some(schema_file) = cli.schema_out {
        eprintln!("Writing schema to {}", schema_file.to_string_lossy());
        std::fs::write(schema_file, schema.sdl()).context("Failed to write schema")?;
    }

    let app = Router::new()
        .route(
            "/",
            get(graphiql).post_service(GraphQL::new(schema.clone())),
        )
        .route_service("/ws", GraphQLSubscription::new(schema));

    eprintln!("Server is running at http://localhost:8000");
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
