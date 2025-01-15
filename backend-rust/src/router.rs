use axum::{extract::State, routing::get, Json, Router};
use axum_prometheus::{PrometheusMetricLayer, PrometheusMetricLayerBuilder};
use prometheus_client::registry::Registry;
use serde_json::json;
use sqlx::PgPool;
use std::sync::Arc;
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;

/// Run server exposing the Prometheus metrics
pub async fn serve(
    tcp_listener: TcpListener,
    pool: PgPool,
    stop_signal: CancellationToken,
) -> anyhow::Result<()> {
    let (metric_layer, metric_handle) = PrometheusMetricLayerBuilder::new()
        .with_prefix("ccdscan")
        .with_default_metrics()
        .build_pair();

    let health_routes = Router::new().route("/", get(health)).with_state(pool);
    let app = Router::new()
        .route("/metrics", get(|| async move { metric_handle.render() }))
        .nest("/health", health_routes)
        .layer(metric_layer);

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
