use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::postgres::types::PgInterval;

use super::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan};

#[derive(SimpleObject)]
struct AccountsMetrics {
    /// Total number of accounts created (all time).
    last_cumulative_accounts_created: i64,

    /// Total number of accounts created in requested period.
    accounts_created: i64,

    buckets: AccountMetricsBuckets,
}

#[derive(SimpleObject)]
struct AccountMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,

    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,

    /// Total number of accounts created (all time) at the end of the bucket
    /// period. Intended y-axis value.
    #[graphql(name = "y_LastCumulativeAccountsCreated")]
    y_last_cumulative_accounts_created: Vec<i64>,

    /// Number of accounts created within bucket time period. Intended y-axis
    /// value.
    #[graphql(name = "y_AccountsCreated")]
    y_accounts_created: Vec<i64>,
}

#[derive(Default)]
pub(crate) struct QueryAccountMetrics;

#[Object]
impl QueryAccountMetrics {
    async fn accounts_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
    ) -> ApiResult<AccountsMetrics> {
        let pool = get_pool(ctx)?;
        let end_time = Utc::now();
        let before_time = end_time - period.as_duration();
        let bucket_width = period.bucket_width();

        // The bucket interval, e.g. 6 hours.
        let bucket_interval: PgInterval = bucket_width
            .try_into()
            .map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

        let rows = sqlx::query_file!(
            "src/graphql_api/account_metrics.sql",
            end_time,
            before_time,
            bucket_interval
        )
        .fetch_all(pool)
        .await?;

        let x_time = rows.iter().map(|r| r.bucket_time).collect();
        let y_last_cumulative_accounts_created: Vec<i64> =
            rows.iter().map(|r| r.end_index).collect();
        let y_accounts_created: Vec<i64> =
            rows.iter().map(|r| r.end_index - r.start_index).collect();
        let last_cumulative_accounts_created =
            *y_last_cumulative_accounts_created.last().unwrap_or(&0);
        let accounts_created = y_accounts_created.iter().sum();

        Ok(AccountsMetrics {
            last_cumulative_accounts_created,
            accounts_created,
            buckets: AccountMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                x_time,
                y_last_cumulative_accounts_created,
                y_accounts_created,
            },
        })
    }
}
