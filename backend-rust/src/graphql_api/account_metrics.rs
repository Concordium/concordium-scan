use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use sqlx::postgres::types::PgInterval;

use super::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan};

#[derive(SimpleObject)]
struct AccountMetrics {
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
    ) -> ApiResult<AccountMetrics> {
        let pool = get_pool(ctx)?;

        let last_cumulative_accounts_created =
            sqlx::query_scalar!("SELECT COALESCE(MAX(index), 0) FROM accounts")
                .fetch_one(pool)
                .await?
                .expect("coalesced");

        // The full period interval, e.g. 7 days.
        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let cumulative_accounts_created_before_period = sqlx::query_scalar!(
            "SELECT COALESCE(MAX(accounts.index), 0)
            FROM accounts
            LEFT JOIN transactions on transaction_index = transactions.index
            LEFT JOIN blocks ON transactions.block_height = height
            WHERE slot_time < (now() - $1::interval)",
            period_interval,
        )
        .fetch_one(pool)
        .await?
        .expect("coalesced");

        let accounts_created =
            last_cumulative_accounts_created - cumulative_accounts_created_before_period;

        let bucket_width = period.bucket_width();

        // The bucket interval, e.g. 6 hours.
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

        let rows = sqlx::query_file!(
            "src/graphql_api/account_metrics.sql",
            period_interval,
            bucket_interval,
        )
        .fetch_all(pool)
        .await?;

        let x_time = rows
            .iter()
            .map(|r| r.bucket_time)
            .collect();
        let y_last_cumulative_accounts_created =
            rows.iter().map(|r| r.end_index.expect("coalesced")).collect();
        let y_accounts_created = rows
            .iter()
            .map(|r| r.end_index.expect("coalesced") - r.start_index.expect("coalesced"))
            .collect();

        Ok(AccountMetrics {
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
