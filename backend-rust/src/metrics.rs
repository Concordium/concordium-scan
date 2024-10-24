use axum::extract::State;
use prometheus_client::registry::Registry;
use std::sync::Arc;
use tokio::net::TcpListener;
use tokio_util::sync::CancellationToken;

/// Run server exposing the Prometheus metrics
pub async fn serve(
    registry: Registry,
    tcp_listener: TcpListener,
    stop_signal: CancellationToken,
) -> anyhow::Result<()> {
    let app =
        axum::Router::new().route("/", axum::routing::get(metrics)).with_state(Arc::new(registry));
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
