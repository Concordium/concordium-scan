use axum::{extract::State, routing::get, Json, Router};
use axum_prometheus::{PrometheusMetricLayerBuilder};
use axum_prometheus::metrics::gauge;
use serde_json::json;
use sqlx::PgPool;
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;

/// Run server exposing the Prometheus metrics
pub async fn serve(
    tcp_listener: TcpListener,
    pool: PgPool,
    stop_signal: CancellationToken,
    prefix: String
) -> anyhow::Result<()> {
    let (metric_layer, metric_handle) = PrometheusMetricLayerBuilder::new()
        .with_prefix(prefix)
        .with_default_metrics()
        .build_pair();
    let health_routes = Router::new().route("/", get(health)).with_state(pool);
    let app = Router::new()
        .route("/metrics", get(|| async move { metric_handle.render() }))
        .nest("/health", health_routes)
        .layer(metric_layer);
    gauge!("service_info", &[("version", clap::crate_version!().to_string())]).set(1);
    gauge!("service_startup_timestamp_millis").set(chrono::Utc::now().timestamp_millis() as f64);
    axum::serve(tcp_listener, app).with_graceful_shutdown(stop_signal.cancelled_owned()).await?;
    Ok(())
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
