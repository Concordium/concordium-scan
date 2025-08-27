//! Contains the GraphQL query for PLT transfer metrics
//! `QueryPltTransferMetricsByTokenId` and `GlobalPltMetrics`.
//!
//! The query `QueryPltTransferMetricsByTokenId` retrieves metrics related to
//! PLT token transfer events over a specified period for a specific token
//! (token_id is mandatory). It provides bucketed data for visualizing transfer
//! trends over time using pre-calculated cumulative metrics for optimal
//! performance.
//!
//! The query `GlobalPltMetrics` provides aggregated PLT metrics across all
//! protocol-level tokens for a specified time period. It returns total event
//! counts (including transfers, mints, burns, etc.) and normalized transfer
//! volumes across all PLT tokens.

use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use bigdecimal::BigDecimal;
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
    // Total amount of transfers in the requested period.
    transfer_amount: f64,
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
    // The transfer amounts for each bucket.
    #[graphql(name = "y_TransferAmount")]
    y_transfer_amount: Vec<f64>,
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
        let mut y_transfer_amount = Vec::with_capacity(rows.len());

        let mut total_transfer_count = 0;
        let mut total_transfer_amount = 0.0;

        // Iterate through the rows and populate the transfer metrics.
        // Each row corresponds to a time bucket with transfer counts and volumes.
        // first row is redundant because of the way cumulative metrics are calculated,
        // the first row is out of the interval it can have residual values from
        // the previous interval so we can skip it
        for row in rows.iter().skip(1) {
            x_time.push(row.bucket_time);

            let transfer_count = row.cumulative_transfer_count.unwrap_or(0)
                - row.prev_cumulative_transfer_count.unwrap_or(0);

            let prev_amount =
                row.prev_cumulative_transfer_amount.clone().unwrap_or(BigDecimal::from(0));
            let curr_amount = row.cumulative_transfer_amount.clone().unwrap_or(BigDecimal::from(0));
            let transfer_amount_bigdecimal = &curr_amount - &prev_amount;
            let transfer_amount =
                num_traits::ToPrimitive::to_f64(&transfer_amount_bigdecimal).unwrap_or(0.0);

            total_transfer_count += transfer_count;
            total_transfer_amount += transfer_amount;

            // Push the counts and amounts into their respective vectors for
            // each bucket for transfer trend visualization.
            y_transfer_count.push(transfer_count);
            y_transfer_amount.push(transfer_amount);
        }

        Ok(PltTransferMetricsByTokenId {
            transfer_count:  total_transfer_count,
            transfer_amount: total_transfer_amount,
            decimal:         plt_token_decimal,

            buckets: PltTransferMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                x_time,
                y_transfer_count,
                y_transfer_amount,
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
    // Total number of PLT events (transfers, mints, burns, etc.) in the
    // period.
    event_count:     i64,
    // Total volume(amount) of PLT tokens transferred in the period.
    // Sum of all transfer amounts normalized by token decimals, then aggregated across different
    // tokens. Example: 1.5 Token1 + 234.3 Token2 = 235.8 (decimal-normalized amounts summed).
    transfer_amount: f64,
}

#[Object]
impl QueryGlobalPltMetrics {
    /// Query for PLT metrics over a specified time period. (across all plts)
    /// returns GlobalPltMetrics plt event_count (Mint/Burn/Transfer etc)
    /// and transfer_volume (the total volume of transfers normalized across all
    /// plts by their respective decimals)
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

        // Get start row
        let start_row = sqlx::query!(
            "SELECT cumulative_event_count, cumulative_transfer_amount
             FROM metrics_plt
             WHERE event_timestamp < NOW() - $1::interval
             ORDER BY event_timestamp DESC
             LIMIT 1",
            period_interval
        )
        .fetch_optional(pool)
        .await?;

        // Get end row
        let end_row = sqlx::query!(
            "SELECT cumulative_event_count, cumulative_transfer_amount
             FROM metrics_plt
             WHERE event_timestamp >= NOW() - $1::interval
             ORDER BY event_timestamp DESC
             LIMIT 1",
            period_interval
        )
        .fetch_optional(pool)
        .await?;

        let (event_count, transfer_amount) = match (start_row, end_row) {
            (Some(start_row), Some(end_row)) => {
                let event_count = end_row.cumulative_event_count - start_row.cumulative_event_count;
                let transfer_amount_bigdecimal =
                    end_row.cumulative_transfer_amount - start_row.cumulative_transfer_amount;
                let transfer_amount =
                    num_traits::ToPrimitive::to_f64(&transfer_amount_bigdecimal).unwrap_or(0.0);
                (event_count, transfer_amount)
            }
            (None, Some(end_row)) => {
                // Handle case where start_row is None but end_row exists
                let event_count = end_row.cumulative_event_count;
                let transfer_amount =
                    num_traits::ToPrimitive::to_f64(&end_row.cumulative_transfer_amount)
                        .unwrap_or(0.0);
                (event_count, transfer_amount)
            }
            _ => (0, 0.0),
        };

        Ok(GlobalPltMetrics {
            transfer_amount,
            event_count,
        })
    }
}
