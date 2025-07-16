use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject, ID};
use num_traits::ToPrimitive;
use sqlx::postgres::types::PgInterval;

use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan},
    scalar_types::TokenId,
};

#[derive(Default)]
pub(crate) struct QueryPltEventMetrics;

#[derive(SimpleObject)]
struct PltEventMetrics {
    last_cumulative_event_count: i64,
    event_count:                 i64,

    last_cumulative_total_supply: f64,
    total_supply:                 f64,

    last_cumulative_unique_holders: i64,
    total_unique_holders:           i64,

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
        token_id: Option<ID>,
    ) -> ApiResult<PltEventMetrics> {
        let pool = get_pool(ctx)?;

        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let plt_token_id: Option<TokenId> = token_id.map(Into::into);

        let rows = sqlx::query_file!(
            "src/graphql_api/plt_event_metrics.sql",
            period_interval,
            bucket_interval,
            plt_token_id.as_ref().map(|id| id.as_str())
        )
        .fetch_all(pool)
        .await?;

        if rows.is_empty() {
            return Ok(PltEventMetrics {
                last_cumulative_event_count: 0,
                event_count: 0,
                last_cumulative_total_supply: 0.0,
                total_supply: 0.0,
                last_cumulative_unique_holders: 0,
                total_unique_holders: 0,
                buckets: PltEventMetricsBuckets {
                    bucket_width: TimeSpan(bucket_width),
                    x_time: vec![],
                    y_last_cumulative_event_count: vec![],
                    y_event_count: vec![],
                    y_total_supply: vec![],
                    y_total_unique_holders: vec![],
                },
            });
        }

        let first = rows.first().unwrap();
        let last = rows.last().unwrap();

        let start_event_count = first.start_cumulative_event_count;
        let end_event_count = last.end_cumulative_event_count;
        let event_count = end_event_count - start_event_count;

        let start_supply = first.start_total_supply.to_f64().unwrap_or(0.0);
        let end_supply = last.end_total_supply.to_f64().unwrap_or(0.0);
        let total_supply = end_supply - start_supply;

        let start_holders = first.start_total_unique_holders.to_i64().unwrap_or(0);
        let end_holders = last.end_total_unique_holders.to_i64().unwrap_or(0);
        let total_unique_holders = end_holders - start_holders;

        let mut x_time = Vec::with_capacity(rows.len());
        let mut y_last_cumulative_event_count = Vec::with_capacity(rows.len());
        let mut y_event_count = Vec::with_capacity(rows.len());
        let mut y_total_supply = Vec::with_capacity(rows.len());
        let mut y_total_unique_holders = Vec::with_capacity(rows.len());

        for row in rows {
            x_time.push(row.bucket_time);
            y_last_cumulative_event_count.push(row.end_cumulative_event_count);
            y_event_count.push(row.end_cumulative_event_count - row.start_cumulative_event_count);
            y_total_supply.push(row.end_total_supply.to_f64().unwrap_or(0.0));
            y_total_unique_holders.push(row.end_total_unique_holders);
        }

        Ok(PltEventMetrics {
            last_cumulative_event_count: end_event_count,
            event_count,
            last_cumulative_total_supply: end_supply,
            total_supply,
            last_cumulative_unique_holders: end_holders,
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
