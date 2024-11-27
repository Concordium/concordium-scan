use axum::extract::State;
use prometheus_client::registry::Registry;
use std::sync::Arc;
use axum::{Json, Router};
use axum::routing::get;
use serde_json::json;
use sqlx::PgPool;
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;

/// Run server exposing the Prometheus metrics
pub async fn serve(
    registry: Registry,
    tcp_listener: TcpListener,
    pool: PgPool,
    stop_signal: CancellationToken,
) -> anyhow::Result<()> {
    let health_routes = Router::new()
        .route("/", get(health))
        .with_state(pool);

    let metric_routes =
        Router::new()
            .route("/", get(metrics)).with_state(Arc::new(registry));

    let app =
        Router::new()
            .nest("/metrics", metric_routes)
            .nest("/health", health_routes);
    axum::serve(tcp_listener, app).with_graceful_shutdown(stop_signal.cancelled_owned()).await?;
    Ok(())
}

/// GET Handler for route `/metrics`.
/// Exposes the metrics in the registry in the Prometheus format.
async fn metrics(State(registry): State<Arc<Registry>>) -> Result<String, String> {
    let mut buffer = String::new();
    prometheus_client::encoding::text::encode(&mut buffer, &registry)
        .map_err(|err| err.to_string())?;
    Ok(buffer)
}

async fn health(State(pool): State<PgPool>) -> Json<serde_json::Value> {
    match sqlx::query("SELECT 1").fetch_one(&pool).await {
        Ok(_) => Json(json!({
            "status": "ok",
            "database": "connected"
        })),
        Err(err) => Json(json!({
            "status": "error",
            "database": format!("not connected: {}", err)
        })),
    }
}
