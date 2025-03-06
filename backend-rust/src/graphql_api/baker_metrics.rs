use std::sync::Arc;
use std::time::Duration;
use async_graphql::{Context, Object, SimpleObject};
use crate::graphql_api::{get_pool, ApiError, ApiResult, MetricsPeriod};
use chrono::{TimeDelta};
use sqlx::postgres::types::PgInterval;
use crate::scalar_types::{DateTime, TimeSpan};

#[derive(Default)]
pub(crate) struct QueryBakerMetrics;

#[Object]
impl QueryBakerMetrics {

    async fn baker_metrics<'a>(&self, ctx: &Context<'a>, period: MetricsPeriod) ->  ApiResult<BakerMetrics> {
        let pool = get_pool(ctx)?;

        let last_cumulative_accounts_created =
            sqlx::query_scalar!("SELECT COALESCE(MAX(index), 0) FROM accounts")
                .fetch_one(pool)
                .await?
                .expect("coalesced");

        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let bucket_width = period.bucket_width();

        Ok(BakerMetrics {
            bakers_added: 0,
            bakers_removed: 0,
            last_baker_count: 0,
            buckets: BakerMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                y_bakers_added: vec![],
                y_last_baker_count: 0,
                x_time: vec![],
                y_block_time_avg: vec![]
            }
        })
    }

}


#[derive(SimpleObject)]
pub struct BakerMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,
    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,
    #[graphql(name = "y_BakersAdded")]
    y_bakers_added: Vec<u64>,
    #[graphql(name = "y_BakersRemoved")]
    y_block_time_avg: Vec<f64>,
    #[graphql(name = "y_LastBakerCount")]
    y_last_baker_count: u64,
}

#[derive(SimpleObject)]
pub struct BakerMetrics {
    last_baker_count: u64,
    bakers_added: i64,
    bakers_removed: i64,
    buckets: BakerMetricsBuckets,
}

