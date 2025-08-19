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
pub(crate) struct QueryPltTransferMetrics;

/// This struct is used to define the GraphQL query for PLT transfer metrics.
#[derive(SimpleObject)]
struct PltTransferMetrics {
    // Total number of transfers in the requested period.
    transfer_count:  i64,
    // Total volume of transfers in the requested period.
    transfer_volume: f64,
    // Buckets for the PLT transfer metrics.
    decimal:         i32,
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
impl QueryPltTransferMetrics {
    async fn plt_transfer_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
        token_id: TokenId,
    ) -> ApiResult<PltTransferMetrics> {
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
        // Remove the first element from the rows if it exists, as it can have redundant
        // data
        let rows = if rows.is_empty() {
            &[]
        } else {
            &rows[1..]
        };

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

        Ok(PltTransferMetrics {
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

pub(crate) struct QueryPltMetrics;

#[derive(SimpleObject)]
struct PltMetrics {
    transaction_count: i64,
    transfer_volume:   f64,
    unique_accounts:   i64,
}

#[Object]
impl QueryPltMetrics {
    async fn plt_metrics(&self, ctx: &Context<'_>, period: MetricsPeriod) -> ApiResult<PltMetrics> {
        let pool = get_pool(ctx)?;
        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let row = sqlx::query!(
            "SELECT cumulative_event_count, cumulative_transfer_amount, unique_account_count
            FROM metrics_plt
            WHERE event_timestamp >= NOW() - $1::interval
            ORDER BY event_timestamp DESC
            LIMIT 1",
            period_interval
        )
        .fetch_optional(pool)
        .await?;

        let (transaction_count, transfer_volume, unique_accounts) = if let Some(row) = row {
            let transfer_volume =
                row.cumulative_transfer_amount.to_string().parse::<f64>().unwrap_or(0.0);

            (row.cumulative_event_count, transfer_volume, row.unique_account_count)
        } else {
            (0, 0.0, 0)
        };

        Ok(PltMetrics {
            transfer_volume,
            transaction_count,
            unique_accounts,
        })
    }
}
