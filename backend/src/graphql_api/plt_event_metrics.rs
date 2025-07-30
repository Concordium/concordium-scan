/// Contains the GraphQL query for PLT event metrics.
/// This query retrieves metrics related to PLT token events such as transfers,
/// mints, burns, and token module creations over a specified period, with
/// optional token filtering. It also provides bucketed data for visualizing
/// trends over time. The results include counts and volumes for each event
/// type, along with time buckets for analysis.
use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use sqlx::postgres::types::PgInterval;

use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, DateTime, MetricsPeriod, TimeSpan},
    scalar_types::TokenId,
};

#[derive(Default)]
pub(crate) struct QueryPltEventMetrics;

/// This struct is used to define the GraphQL query for PLT event metrics.
#[derive(SimpleObject)]
struct PltEventMetrics {
    /// Total number of transfers in the requested period.
    transfer_count:  i64,
    /// Total volume of transfers in the requested period.
    transfer_volume: f64,
    /// Total number of mints in the requested period.
    mint_count:      i64,
    /// Total volume of mints in the requested period.
    mint_volume:     f64,

    /// Total number of burns in the requested period.
    burn_count:  i64,
    /// Total volume of burns in the requested period.
    burn_volume: f64,

    /// Total number of token modules created in the requested period.
    token_module_count: i64,
    /// Total number of events in the requested period.
    total_event_count:  i64,
    /// Buckets for the PLT event metrics.
    buckets:            PltEventMetricsBuckets,
}
/// This struct is used to define the buckets for PLT event metrics.
#[derive(SimpleObject)]
struct PltEventMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width:     TimeSpan,
    /// The time values for each bucket.
    #[graphql(name = "x_Time")]
    x_time:           Vec<DateTime>,
    /// The counts and volumes for each event type in the buckets.
    #[graphql(name = "y_TransferCount")]
    y_transfer_count: Vec<i64>,

    #[graphql(name = "y_TransferVolume")]
    y_transfer_volume: Vec<f64>,

    /// Counts and volumes for mint events in the buckets.
    #[graphql(name = "y_MintCount")]
    y_mint_count: Vec<i64>,

    #[graphql(name = "y_MintVolume")]
    y_mint_volume: Vec<f64>,
    /// Counts and volumes for burn events in the buckets.
    #[graphql(name = "y_BurnCount")]
    y_burn_count:  Vec<i64>,

    #[graphql(name = "y_BurnVolume")]
    y_burn_volume:        Vec<f64>,
    /// Counts of token modules created in the buckets.
    #[graphql(name = "y_TokenModuleCount")]
    y_token_module_count: Vec<i64>,

    #[graphql(name = "y_TotalEventCount")]
    y_total_event_count: Vec<i64>,
}

/// Implementation of the GraphQL query for PLT event metrics.
/// This query retrieves metrics related to PLT token events over a specified
/// period. It allows filtering by token ID and provides bucketed data for
/// analysis. The results include counts and volumes for transfers, mints,
/// burns, and token module creations.
#[Object]
impl QueryPltEventMetrics {
    async fn plt_event_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
        token_id: Option<TokenId>,
    ) -> ApiResult<PltEventMetrics> {
        let pool = get_pool(ctx)?;

        let period_interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;

        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|e| ApiError::DurationOutOfRange(Arc::new(e)))?;
        /// One extra query to get the token index if a token_id is provided.
        let plt_token_index: Option<i64> = if let Some(token_id) = token_id {
            let result = sqlx::query!("SELECT index FROM plt_tokens WHERE token_id = $1", token_id)
                .fetch_optional(pool)
                .await?;

            if let Some(row) = result {
                Some(row.index)
            } else {
                return Err(ApiError::NotFound);
            }
        } else {
            None
        };

        let rows = sqlx::query_file!(
            "src/graphql_api/plt_event_metrics.sql",
            period_interval,
            bucket_interval,
            plt_token_index
        )
        .fetch_all(pool)
        .await?;

        let mut x_time = Vec::with_capacity(rows.len());
        let mut y_transfer_count = Vec::with_capacity(rows.len());
        let mut y_transfer_volume = Vec::with_capacity(rows.len());
        let mut y_mint_count = Vec::with_capacity(rows.len());
        let mut y_mint_volume = Vec::with_capacity(rows.len());
        let mut y_burn_count = Vec::with_capacity(rows.len());
        let mut y_burn_volume = Vec::with_capacity(rows.len());
        let mut y_token_module_count = Vec::with_capacity(rows.len());
        let mut y_total_event_count = Vec::with_capacity(rows.len());

        let mut total_transfer_count = 0;
        let mut total_transfer_volume = 0.0;
        let mut total_mint_count = 0;
        let mut total_mint_volume = 0.0;
        let mut total_burn_count = 0;
        let mut total_burn_volume = 0.0;
        let mut total_token_module_count = 0;
        let mut total_event_count = 0;
        /// Iterate through the rows and populate the metrics variables.
        /// Each row corresponds to a time bucket with counts and volumes for
        /// each event type. The results are aggregated to provide
        /// totals for the entire period.
        for row in rows {
            x_time.push(row.bucket_time);

            let transfer_count = row.transfer_count.unwrap_or(0);
            let transfer_volume = row.transfer_volume.unwrap_or(0.0);
            let mint_count = row.mint_count.unwrap_or(0);
            let mint_volume = row.mint_volume.unwrap_or(0.0);
            let burn_count = row.burn_count.unwrap_or(0);
            let burn_volume = row.burn_volume.unwrap_or(0.0);
            let token_module_count = row.token_module_count.unwrap_or(0);
            let event_count = row.total_event_count.unwrap_or(0);

            total_transfer_count += transfer_count;
            total_transfer_volume += transfer_volume;
            total_mint_count += mint_count;
            total_mint_volume += mint_volume;
            total_burn_count += burn_count;
            total_burn_volume += burn_volume;
            total_token_module_count += token_module_count;
            total_event_count += event_count;
            /// Push the counts and volumes into their respective vectors for
            /// each bucket. This allows for easy visualization and
            /// analysis of trends over time.
            y_transfer_count.push(transfer_count);
            y_transfer_volume.push(transfer_volume);
            y_mint_count.push(mint_count);
            y_mint_volume.push(mint_volume);
            y_burn_count.push(burn_count);
            y_burn_volume.push(burn_volume);
            y_token_module_count.push(token_module_count);
            y_total_event_count.push(event_count);
        }

        Ok(PltEventMetrics {
            transfer_count: total_transfer_count,
            transfer_volume: total_transfer_volume,
            mint_count: total_mint_count,
            mint_volume: total_mint_volume,
            burn_count: total_burn_count,
            burn_volume: total_burn_volume,
            token_module_count: total_token_module_count,
            total_event_count,

            buckets: PltEventMetricsBuckets {
                bucket_width: TimeSpan(bucket_width),
                x_time,
                y_transfer_count,
                y_transfer_volume,
                y_mint_count,
                y_mint_volume,
                y_burn_count,
                y_burn_volume,
                y_token_module_count,
                y_total_event_count,
            },
        })
    }
}
