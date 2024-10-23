use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use sqlx::postgres::types::PgInterval;

use crate::graphql_api::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan};

#[derive(Default)]
pub(crate) struct TransactionMetricsQuery;

#[derive(SimpleObject)]
struct TransactionMetrics {
    /// Total number of transactions (all time).
    last_cumulative_transaction_count: i64,
    /// Total number of transactions in the requested period.
    transaction_count: i64,
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
        let pool = get_pool(ctx)?;

        let last_cumulative_transaction_count = sqlx::query_scalar!(
            "SELECT cumulative_num_txs FROM blocks ORDER BY height DESC LIMIT 1"
        )
        .fetch_one(pool)
        .await?;

        let interval: PgInterval = period.as_duration().try_into().map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let cumulative_transaction_count_before_period = sqlx::query_scalar!(
            "SELECT cumulative_num_txs
            FROM blocks
            WHERE slot_time < (now() - $1::interval)
            ORDER BY height DESC
            LIMIT 1",
            interval,
        )
        .fetch_one(pool)
        .await?;

        let transaction_count =
            last_cumulative_transaction_count - cumulative_transaction_count_before_period;

        Ok(TransactionMetrics {
            last_cumulative_transaction_count,
            transaction_count,
            buckets: todo!(),
        })
    }
}
