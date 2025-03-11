use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, MetricsPeriod},
    scalar_types::{DateTime, TimeSpan},
};
use async_graphql::{Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::postgres::types::PgInterval;
use std::sync::Arc;

#[derive(Default)]
pub(crate) struct QueryBakerMetrics;

#[Object]
impl QueryBakerMetrics {
    /// Fetches baker metrics for the specified period.
    ///
    /// This function queries the database for baker metrics such as the number
    /// of bakers added, removed, and the last baker count in the specified
    /// time period. It returns the results as a structured `BakerMetrics`
    /// object.
    async fn baker_metrics<'a>(
        &self,
        ctx: &Context<'a>,
        period: MetricsPeriod,
    ) -> ApiResult<BakerMetrics> {
        let pool = get_pool(ctx)?;

        let end_time = Utc::now();

        let before_time = end_time - period.as_duration();

        let before_period_row = sqlx::query!(
            r#"
            SELECT
                total_bakers_added,
                total_bakers_removed
            FROM metrics_bakers
            LEFT JOIN blocks ON metrics_bakers.block_height = blocks.height
            WHERE blocks.slot_time < $1
            ORDER BY metrics_bakers.block_height DESC
            LIMIT 1
            "#,
            before_time,
        )
        .fetch_optional(pool)
        .await?;

        let last_in_period_row = sqlx::query!(
            r#"
            SELECT
                total_bakers_added,
                total_bakers_removed
            FROM metrics_bakers
            LEFT JOIN blocks ON metrics_bakers.block_height = blocks.height
            WHERE blocks.slot_time < $1
            ORDER BY metrics_bakers.block_height DESC
            LIMIT 1
            "#,
            end_time
        )
        .fetch_optional(pool)
        .await?;

        let (before_added, before_removed) = before_period_row
            .map(|r| (r.total_bakers_added, r.total_bakers_removed))
            .unwrap_or((0, 0));
        let (after_added, after_removed) = last_in_period_row
            .map(|r| (r.total_bakers_added, r.total_bakers_removed))
            .unwrap_or((0, 0));

        let last_baker_count = before_added - before_removed;
        let bakers_added = after_added - before_added;
        let bakers_removed = after_removed - before_removed;

        let bucket_width = period.bucket_width();

        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

        let rows = sqlx::query_file!(
            "src/graphql_api/baker_metrics.sql",
            end_time,
            before_time,
            bucket_interval,
        )
        .fetch_all(pool)
        .await?;

        let mut x_time = Vec::with_capacity(rows.len());
        let mut y_bakers_added: Vec<u64> = Vec::with_capacity(rows.len());
        let mut y_bakers_removed: Vec<u64> = Vec::with_capacity(rows.len());

        for r in rows.iter() {
            x_time.push(r.bucket_time);
            y_bakers_added.push(r.bucket_bakers_added.try_into()?);
            y_bakers_removed.push(r.bucket_bakers_removed.try_into()?);
        }

        Ok(BakerMetrics {
            bakers_added,
            bakers_removed,
            last_baker_count: last_baker_count.try_into()?,
            buckets: BakerMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                y_last_baker_count: last_baker_count.try_into()?,
                x_time,
                y_bakers_removed,
                y_bakers_added,
            },
        })
    }
}

#[derive(SimpleObject)]
pub struct BakerMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width:       TimeSpan,
    /// The time values (start of each bucket) intended for use as x-axis
    /// values.
    #[graphql(name = "x_Time")]
    x_time:             Vec<DateTime>,
    /// The number of bakers added for each bucket, intended for use as y-axis
    /// values.
    #[graphql(name = "y_BakersAdded")]
    y_bakers_added:     Vec<u64>,
    /// The number of bakers removed for each bucket, intended for use as y-axis
    /// values.
    #[graphql(name = "y_BakersRemoved")]
    y_bakers_removed:   Vec<u64>,
    /// Total bakers before the start of the period.
    #[graphql(name = "y_LastBakerCount")]
    y_last_baker_count: u64,
}

#[derive(SimpleObject)]
pub struct BakerMetrics {
    /// Total bakers before the start of the period
    last_baker_count: u64,
    /// The number of bakers added during the specified period.
    bakers_added:     i64,
    /// The number of bakers removed during the specified period.
    bakers_removed:   i64,
    /// Bucket-wise data for bakers added, removed, and the bucket times.
    buckets:          BakerMetricsBuckets,
}
