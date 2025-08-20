//! Contains the GraphQL query for PLT transfer metrics.
//! This query retrieves metrics related to PLT token transfer events over a
//! specified period for a specific token (token_id is mandatory). It provides
//! bucketed data for visualizing transfer trends over time using pre-calculated
//! cumulative metrics for optimal performance.
use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use sqlx::postgres::types::PgInterval;

use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan},
    scalar_types::TokenId,
};

#[derive(Default)]
pub(crate) struct QueryPltTransferMetricsByTokenId;

/// This struct is used to define the GraphQL query for PLT transfer metrics.
#[derive(SimpleObject)]
struct PltTransferMetricsByTokenId {
    // Total number of transfers in the requested period.
    transfer_count:  i64,
    // Total volume of transfers in the requested period.
    transfer_volume: f64,
    // Decimal places of the token
    decimal:         i32,
    // Buckets for the PLT transfer metrics.
    buckets:         PltTransferMetricsBuckets,
}
/// This struct is used to define the buckets for PLT transfer metrics.
#[derive(SimpleObject)]
struct PltTransferMetricsBuckets {
    // The width (time interval) of each bucket.
    bucket_width:      TimeSpan,
    // The time values for each bucket.
    #[graphql(name = "x_Time")]
    x_time:            Vec<DateTime>,
    // The transfer counts for each bucket.
    #[graphql(name = "y_TransferCount")]
    y_transfer_count:  Vec<i64>,
    // The transfer volumes for each bucket.
    #[graphql(name = "y_TransferVolume")]
    y_transfer_volume: Vec<f64>,
}

/// Implementation of the GraphQL query for PLT transfer metrics.
/// This query retrieves metrics related to PLT token transfer events over a
/// specified period for a specific token (token_id is mandatory). It provides
/// bucketed data for analysis using pre-calculated cumulative transfer metrics
/// for optimal performance and focuses exclusively on transfer analytics.
#[Object]
impl QueryPltTransferMetricsByTokenId {
    async fn plt_transfer_metrics_by_token_id(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
        token_id: TokenId,
    ) -> ApiResult<PltTransferMetricsByTokenId> {
        let pool = get_pool(ctx)?;

        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        // Get the token index for the provided token_id
        let record =
            sqlx::query!("SELECT index, decimal FROM plt_tokens WHERE token_id = $1", token_id)
                .fetch_optional(pool)
                .await?
                .ok_or(ApiError::NotFound)?;
        let plt_token_index = record.index;
        let plt_token_decimal = record.decimal;

        let rows = sqlx::query_file!(
            "src/graphql_api/plt_transfer_metrics.sql",
            period_interval,
            bucket_interval,
            plt_token_index
        )
        .fetch_all(pool)
        .await?;

        let mut x_time = Vec::with_capacity(rows.len());
        let mut y_transfer_count = Vec::with_capacity(rows.len());
        let mut y_transfer_volume = Vec::with_capacity(rows.len());

        let mut total_transfer_count = 0;
        let mut total_transfer_volume = 0.0;

        // Iterate through the rows and populate the transfer metrics.
        // Each row corresponds to a time bucket with transfer counts and volumes.
        for row in rows {
            x_time.push(row.bucket_time);

            let transfer_count = row.transfer_count.unwrap_or(0);
            let transfer_volume = row
                .transfer_volume
                .as_ref()
                .and_then(num_traits::ToPrimitive::to_f64)
                .unwrap_or(0.0);
            total_transfer_count += transfer_count;
            total_transfer_volume += transfer_volume;

            // Push the counts and volumes into their respective vectors for
            // each bucket for transfer trend visualization.
            y_transfer_count.push(transfer_count);
            y_transfer_volume.push(transfer_volume);
        }

        Ok(PltTransferMetricsByTokenId {
            transfer_count:  total_transfer_count,
            transfer_volume: total_transfer_volume,
            decimal:         plt_token_decimal,

            buckets: PltTransferMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                x_time,
                y_transfer_count,
                y_transfer_volume,
            },
        })
    }
}

#[derive(Default)]
pub(crate) struct QueryGlobalPltMetrics;

/// Represents protocol-level token (PLT) metrics for a given period.
///
/// This struct is returned by the GraphQL API and provides summary statistics
/// for PLT token activity over a specified time window.
#[derive(SimpleObject)]
struct GlobalPltMetrics {
    /// Total number of PLT events (transfers, mints, burns, etc.) in the
    /// period.
    event_count:     i64,
    /// Total volume(amount) of PLT tokens transferred in the period.
    transfer_volume: f64,
}

#[Object]
impl QueryGlobalPltMetrics {
    // Query for PLT metrics over a specified time period.
    async fn global_plt_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
    ) -> ApiResult<GlobalPltMetrics> {
        let pool = get_pool(ctx)?;
        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let row = sqlx::query!(
            "WITH
                start_row AS (
                    SELECT cumulative_event_count, cumulative_transfer_amount
                    FROM metrics_plt
                    WHERE event_timestamp < NOW() - $1::interval
                    ORDER BY event_timestamp DESC
                    LIMIT 1
                ),
                end_row AS (
                    SELECT cumulative_event_count, cumulative_transfer_amount
                    FROM metrics_plt
                    WHERE event_timestamp >= NOW() - $1::interval
                    ORDER BY event_timestamp DESC
                    LIMIT 1
                )
                SELECT
               GREATEST(COALESCE((SELECT cumulative_event_count FROM end_row), 0) - \
             COALESCE((SELECT cumulative_event_count FROM start_row), 0), 0) AS event_count,
               GREATEST(COALESCE((SELECT cumulative_transfer_amount FROM end_row), 0) - \
             COALESCE((SELECT cumulative_transfer_amount FROM start_row), 0), 0) AS \
             transfer_volume;",
            period_interval
        )
        .fetch_optional(pool)
        .await?;

        let (event_count, transfer_volume) = if let Some(row) = row {
            (
                row.event_count.unwrap_or(0),
                row.transfer_volume
                    .as_ref()
                    .and_then(num_traits::ToPrimitive::to_f64)
                    .unwrap_or(0.0),
            )
        } else {
            (0, 0.0)
        };

        Ok(GlobalPltMetrics {
            transfer_volume,
            event_count,
        })
    }
}
