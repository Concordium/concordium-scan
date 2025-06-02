use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, MetricsPeriod},
    scalar_types::{DateTime, TimeSpan},
};
use async_graphql::{Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::postgres::types::PgInterval;
use std::{ops::Sub, sync::Arc};

use super::InternalError;

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
        let bucket_width = period.bucket_width();

        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

        let rows = sqlx::query_file!(
            "src/graphql_api/baker_metrics.sql",
            end_time,
            before_time,
            bucket_interval
        )
        .fetch_all(pool)
        .await?;

        let first_row = rows.first().ok_or_else(|| {
            InternalError::InternalError("No metrics found for the given period".to_string())
        })?;

        let mut current_period_baker_count: u64 =
            first_row.added_before.sub(first_row.removed_before).try_into().map_err(|_| {
                InternalError::InternalError("Invalid initial baker count".to_string())
            })?;

        let (mut bakers_added, mut bakers_removed) = (0, 0);
        let mut x_time = Vec::with_capacity(rows.len());
        let mut y_bakers_added: Vec<u64> = Vec::with_capacity(rows.len());
        let mut y_bakers_removed: Vec<u64> = Vec::with_capacity(rows.len());
        let mut y_last_baker_count: Vec<u64> = Vec::with_capacity(rows.len());
        for r in rows.iter() {
            x_time.push(r.bucket_time);

            let bucket_bakers_added = r.added_after - r.added_before;
            let added_during_period: u64 = bucket_bakers_added.try_into()?;
            bakers_added += added_during_period;
            y_bakers_added.push(added_during_period);

            let bucket_bakers_removed = r.removed_after - r.removed_before;
            let removed_during_period: u64 = bucket_bakers_removed.try_into()?;
            bakers_removed += removed_during_period;
            y_bakers_removed.push(removed_during_period);

            current_period_baker_count =
                current_period_baker_count + added_during_period - removed_during_period;
            y_last_baker_count.push(current_period_baker_count);
        }

        let last_baker_count = y_last_baker_count.last().ok_or_else(|| {
            InternalError::InternalError("Failed to compute final baker count".to_string())
        })?;

        Ok(BakerMetrics {
            bakers_added:     bakers_added.try_into()?,
            bakers_removed:   bakers_removed.try_into()?,
            last_baker_count: *last_baker_count,
            buckets:          BakerMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                y_last_baker_count,
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
    /// Total bakers during each period
    #[graphql(name = "y_LastBakerCount")]
    y_last_baker_count: Vec<u64>,
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
