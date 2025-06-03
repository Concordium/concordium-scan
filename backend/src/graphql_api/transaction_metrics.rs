use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use sqlx::postgres::types::PgInterval;

use crate::graphql_api::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan};

#[derive(Default)]
pub(crate) struct QueryTransactionMetrics;

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
    y_last_cumulative_transaction_count: Vec<i64>,

    /// Total number of transactions within the bucket time period. Intended
    /// y-axis value.
    #[graphql(name = "y_TransactionCount")]
    y_transaction_count: Vec<i64>,
}

#[Object]
impl QueryTransactionMetrics {
    async fn transaction_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
    ) -> ApiResult<TransactionMetrics> {
        let pool = get_pool(ctx)?;
        // The full period interval, e.g. 7 days.
        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;
        // The bucket interval, e.g. 6 hours.
        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;
        let rows = sqlx::query_file!(
            "src/graphql_api/transaction_metrics.sql",
            period_interval,
            bucket_interval,
        )
        .fetch_all(pool)
        .await?;
        let (transaction_count, last_cumulative_transaction_count) =
            if let (Some(first_row), Some(last_row)) = (rows.first(), rows.last()) {
                let count = last_row.end_cumulative_num_txs - first_row.start_cumulative_num_txs;
                (count, last_row.end_cumulative_num_txs)
            } else {
                (0, 0)
            };
        let (x_time, (y_last_cumulative_transaction_count, y_transaction_count)) = rows
            .iter()
            .map(|row| {
                let x_time = row.bucket_time;
                let y_last_cumulative_transaction_count = row.end_cumulative_num_txs;
                let y_transaction_count = row.end_cumulative_num_txs - row.start_cumulative_num_txs;
                (x_time, (y_last_cumulative_transaction_count, y_transaction_count))
            })
            .collect();
        Ok(TransactionMetrics {
            last_cumulative_transaction_count,
            transaction_count,
            buckets: TransactionMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                x_time,
                y_last_cumulative_transaction_count,
                y_transaction_count,
            },
        })
    }
}
