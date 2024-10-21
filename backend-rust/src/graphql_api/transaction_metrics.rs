use async_graphql::{Context, Object, SimpleObject};

use crate::graphql_api::{context_ext::ContextExt, ApiResult, MetricsPeriod};

use super::{DateTime, TimeSpan};

#[derive(Default)]
pub(crate) struct TransactionMetricsQuery;

#[derive(SimpleObject)]
struct TransactionMetrics {
    /// Total number of transactions (all time).
    last_cumulative_transaction_count: usize,
    /// Total number of transactions in the requested period.
    transaction_count: usize,
    buckets: TransactionMetricsBuckets,
}

#[derive(SimpleObject)]
struct TransactionMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,

    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,

    /// Total number of transactions (all time) at the end of the bucket period.
    /// Intended y-axis value.
    #[graphql(name = "y_LastCumulativeTransactionCount")]
    y_last_cumulative_transaction_count: Vec<usize>,

    /// Total number of transactions within the bucket time period. Intended
    /// y-axis value.
    #[graphql(name = "y_TransactionCount")]
    y_transaction_count: Vec<usize>,
}

// TODO: Finish the transaction_metrics function and remove this allow.
#[allow(unreachable_code)]
#[Object]
impl TransactionMetricsQuery {
    async fn transaction_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
    ) -> ApiResult<TransactionMetrics> {
        let pool = ctx.pool()?;

        todo!()
    }
}
