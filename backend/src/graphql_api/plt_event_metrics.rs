use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject, ID};
use sqlx::postgres::types::PgInterval;
use num_traits::ToPrimitive;

use crate::{graphql_api::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan}, scalar_types::TokenId};

#[derive(Default)]
pub(crate) struct QueryPltEventMetrics;

#[derive(SimpleObject)]
struct PltEventMetrics {
    last_cumulative_event_count: i64,
    event_count: i64,

    last_cumulative_total_supply: f64,
    total_supply: f64,

    last_cumulative_unique_holders: i64,
    total_unique_holders: i64,

    buckets: PltEventMetricsBuckets,
}

#[derive(SimpleObject)]
struct PltEventMetricsBuckets {
    bucket_width: TimeSpan,

    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,

    #[graphql(name = "y_LastCumulativeEventCount")]
    y_last_cumulative_event_count: Vec<i64>,

    #[graphql(name = "y_EventCount")]
    y_event_count: Vec<i64>,

    #[graphql(name = "y_TotalSupply")]
    y_total_supply: Vec<f64>,

    #[graphql(name = "y_TotalUniqueHolders")]
    y_total_unique_holders: Vec<i64>,
}

#[Object]
impl QueryPltEventMetrics {
    async fn plt_event_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
        token_id:Option<ID>
    ) -> ApiResult<PltEventMetrics> {
        let pool = get_pool(ctx)?;

        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval = bucket_width
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;
        let plt_token_id:Option<TokenId> = token_id.map(|id| id.into());

        let rows = sqlx::query_file!(
            "src/graphql_api/plt_event_metrics.sql",
            period_interval,
            bucket_interval,
            plt_token_id.as_ref().map(|id| id.as_str())
        )
        .fetch_all(pool)
        .await?;

        let (event_count, last_cumulative_event_count) = if let (Some(start), Some(end)) = (rows.first(), rows.last()) {
            let start_count = start.start_cumulative_event_count;
            let end_count = end.end_cumulative_event_count;
            (end_count - start_count, end_count)
        } else {
            (0, 0)
        };

        let (total_supply, last_cumulative_total_supply) = if let (Some(start), Some(end)) = (rows.first(), rows.last()) {
            let start_supply = start.start_total_supply.to_f64().unwrap_or(0.0);
            let end_supply = end.end_total_supply.to_f64().unwrap_or(0.0);
            (end_supply - start_supply, end_supply)
        } else {
            (0.0, 0.0)
        };

        let (total_unique_holders, last_cumulative_unique_holders) = if let (Some(start), Some(end)) = (rows.first(), rows.last()) {
            let start_holders = start.start_total_unique_holders.to_i64().unwrap_or(0);
            let end_holders = end.end_total_unique_holders.to_i64().unwrap_or(0);
            (end_holders - start_holders, end_holders)
        } else {
            (0, 0)
        };

        let (
            x_time,
            y_last_cumulative_event_count,
            y_event_count,
            y_total_supply,
            y_total_unique_holders,
        ) = rows.iter().fold(
            (vec![], vec![], vec![], vec![], vec![]),
            |(mut x, mut y_last, mut y_diff, mut y_supply, mut y_holders), row| {
                x.push(row.bucket_time);
                let start = row.start_cumulative_event_count;
                let end = row.end_cumulative_event_count;
                y_last.push(end);
                y_diff.push(end - start);

                let supply = row.end_total_supply.to_f64().unwrap_or(0.0);
                y_supply.push(supply);

                let holders = row.end_total_unique_holders;
                y_holders.push(holders);

                (x, y_last, y_diff, y_supply, y_holders)
            },
        );

        Ok(PltEventMetrics {
            last_cumulative_event_count,
            event_count,
            last_cumulative_total_supply,
            total_supply,
            last_cumulative_unique_holders,
            total_unique_holders,
            buckets: PltEventMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                x_time,
                y_last_cumulative_event_count,
                y_event_count,
                y_total_supply,
                y_total_unique_holders,
            },
        })
    }
}
