/// Module containing types and logic for building an async_graphql extension
/// which allows for monitoring of the service.
use async_graphql::async_trait::async_trait;
use futures::prelude::*;
use prometheus_client::{
    encoding::EncodeLabelSet,
    metrics::{
        counter::Counter,
        family::Family,
        gauge::Gauge,
        histogram::{self, Histogram},
    },
    registry::Registry,
};
use std::sync::Arc;
use tokio::time::Instant;

#[derive(Debug, Clone, EncodeLabelSet, PartialEq, Eq, Hash)]
struct QueryLabels {
    /// Identifier of the top level query.
    query: String,
}

#[derive(Clone)]
pub struct HttpMonitorExtension {
    /// Metric for tracking current number of requests in-flight.
    http_status_codes:   Family<QueryLabels, Gauge>,
}

/// Type representing the Prometheus labels used for metrics related to
/// queries.
#[derive(Debug, Clone, EncodeLabelSet, PartialEq, Eq, Hash)]
struct QueryLabels {
    /// Identifier of the top level query.
    query: String,
}

/// Extension for async_graphql adding monitoring.
#[derive(Clone)]
pub struct GraphQLMonitorExtension {
    /// Metric for tracking current number of requests in-flight.
    in_flight_requests:   Family<QueryLabels, Gauge>,
    /// Metric for counting total number of requests.
    total_requests:       Family<QueryLabels, Counter>,
    /// Metric for collecting execution duration for requests.
    request_duration:     Family<QueryLabels, Histogram>,
    /// Metric tracking current open subscriptions.
    active_subscriptions: Gauge,
}
impl GraphQLMonitorExtension {
    pub fn new(registry: &mut Registry) -> Self {
        let in_flight_requests: Family<QueryLabels, Gauge> = Default::default();
        registry.register(
            "in_flight_queries",
            "Current number of queries in-flight",
            in_flight_requests.clone(),
        );
        let total_requests: Family<QueryLabels, Counter> = Default::default();
        registry.register(
            "requests",
            "Total number of requests received",
            total_requests.clone(),
        );
        let request_duration: Family<QueryLabels, Histogram> =
            Family::new_with_constructor(|| {
                Histogram::new(histogram::exponential_buckets(0.010, 2.0, 10))
            });
        registry.register(
            "request_duration_seconds",
            "Duration of seconds used to fetch all of the block information",
            request_duration.clone(),
        );
        let active_subscriptions: Gauge = Default::default();
        registry.register(
            "active_subscription",
            "Current number of active subscriptions",
            active_subscriptions.clone(),
        );
        GraphQLMonitorExtension {
            in_flight_requests,
            total_requests,
            request_duration,
            active_subscriptions,
        }
    }
}
impl async_graphql::extensions::ExtensionFactory for GraphQLMonitorExtension {
    fn create(&self) -> Arc<dyn async_graphql::extensions::Extension> { Arc::new(self.clone()) }
}
#[async_trait]
impl async_graphql::extensions::Extension for GraphQLMonitorExtension {
    async fn execute(
        &self,
        ctx: &async_graphql::extensions::ExtensionContext<'_>,
        operation_name: Option<&str>,
        next: async_graphql::extensions::NextExecute<'_>,
    ) -> async_graphql::Response {
        let label = QueryLabels {
            query: operation_name.unwrap_or("<none>").to_owned(),
        };
        self.in_flight_requests.get_or_create(&label).inc();
        self.total_requests.get_or_create(&label).inc();
        let start = Instant::now();
        let response = next.run(ctx, operation_name).await;
        let duration = start.elapsed();
        self.request_duration.get_or_create(&label).observe(duration.as_secs_f64());
        self.in_flight_requests.get_or_create(&label).dec();
        response
    }

    /// Called at subscribe request.
    fn subscribe<'s>(
        &self,
        ctx: &async_graphql::extensions::ExtensionContext<'_>,
        stream: stream::BoxStream<'s, async_graphql::Response>,
        next: async_graphql::extensions::NextSubscribe<'_>,
    ) -> stream::BoxStream<'s, async_graphql::Response> {
        let stream = next.run(ctx, stream);
        let wrapped_stream = WrappedStream::new(stream, self.active_subscriptions.clone());
        wrapped_stream.boxed()
    }
}
/// Wrapper around a stream to update metrics when it gets dropped.
struct WrappedStream<'s> {
    inner:                stream::BoxStream<'s, async_graphql::Response>,
    active_subscriptions: Gauge,
}
impl<'s> WrappedStream<'s> {
    fn new(
        stream: stream::BoxStream<'s, async_graphql::Response>,
        active_subscriptions: Gauge,
    ) -> Self {
        active_subscriptions.inc();
        Self {
            inner: stream,
            active_subscriptions,
        }
    }
}
impl futures::stream::Stream for WrappedStream<'_> {
    type Item = async_graphql::Response;

    fn poll_next(
        mut self: std::pin::Pin<&mut Self>,
        cx: &mut std::task::Context<'_>,
    ) -> std::task::Poll<Option<Self::Item>> {
        self.inner.poll_next_unpin(cx)
    }
}
impl std::ops::Drop for WrappedStream<'_> {
    fn drop(&mut self) { self.active_subscriptions.dec(); }
}
