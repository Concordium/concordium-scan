use std::sync::Arc;

use async_graphql::{Context, Object, SimpleObject};
use sqlx::postgres::types::PgInterval;

use crate::{
    graphql_api::{ApiResult, MetricsPeriod},
    scalar_types::{Amount, BlockHeight, DateTime, TimeSpan},
};

use super::{get_config, get_pool, ApiError};

#[derive(SimpleObject)]
struct BlockMetrics {
    /// The most recent block height. Equals the total length of the chain minus
    /// one (genesis block is at height zero).
    last_block_height: BlockHeight,
    /// Total number of blocks added in requested period.
    blocks_added: i64,
    /// The average block time in seconds (slot-time difference between two
    /// adjacent blocks) in the requested period. Will be null if no blocks
    /// have been added in the requested period.
    avg_block_time: Option<f64>,
    /// The average finalization time in seconds (slot-time difference between a
    /// given block and the block that holds its finalization proof) in the
    /// requested period. Will be null if no blocks have been finalized in
    /// the requested period.
    avg_finalization_time: Option<f64>,
    /// The current total amount of CCD in existence.
    last_total_micro_ccd: Amount,
    /// The total CCD Released. This is total CCD supply not counting the
    /// balances of non circulating accounts.
    last_total_micro_ccd_released: Amount,
    /// The current total CCD released according to the Concordium promise
    /// published on deck.concordium.com. Will be null for blocks with slot
    /// time before the published release schedule.
    last_total_micro_ccd_unlocked: Option<Amount>,
    /// The current total amount of CCD staked.
    last_total_micro_ccd_staked: Amount,
    buckets: BlockMetricsBuckets,
}

#[derive(SimpleObject)]
struct BlockMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,
    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,
    /// Number of blocks added within the bucket time period. Intended y-axis
    /// value.
    #[graphql(name = "y_BlocksAdded")]
    y_blocks_added: Vec<i64>,
    /// The average block time (slot-time difference between two adjacent
    /// blocks) in the bucket period. Intended y-axis value. Will be null if
    /// no blocks have been added in the bucket period.
    #[graphql(name = "y_BlockTimeAvg")]
    y_block_time_avg: Vec<f64>,
    /// The average finalization time (slot-time difference between a given
    /// block and the block that holds its finalization proof) in the bucket
    /// period. Intended y-axis value. Will be null if no blocks have been
    /// finalized in the bucket period.
    #[graphql(name = "y_FinalizationTimeAvg")]
    y_finalization_time_avg: Vec<f64>,
    /// The total amount of CCD staked at the end of the bucket period. Intended
    /// y-axis value.
    #[graphql(name = "y_LastTotalMicroCcdStaked")]
    y_last_total_micro_ccd_staked: Vec<Amount>,
}

#[derive(Default)]
pub(crate) struct QueryBlockMetrics;

#[Object]
impl QueryBlockMetrics {
    async fn block_metrics<'a>(
        &self,
        ctx: &Context<'a>,
        period: MetricsPeriod,
    ) -> ApiResult<BlockMetrics> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let non_circulating_accounts =
            config.non_circulating_account.iter().map(|a| a.to_string()).collect::<Vec<_>>();

        let latest_block = sqlx::query!(
            "WITH non_circulating_accounts AS (
                 SELECT
                     COALESCE(SUM(amount), 0)::BIGINT AS total_amount
                 FROM accounts
                 WHERE address = ANY($1)
             )
             SELECT
                 height,
                 blocks.total_amount,
                 total_staked,
                 (blocks.total_amount - non_circulating_accounts.total_amount)::BIGINT
                     AS total_amount_released
             FROM blocks, non_circulating_accounts
             ORDER BY height DESC
             LIMIT 1",
            non_circulating_accounts.as_slice()
        )
        .fetch_one(pool)
        .await?;

        let interval: PgInterval = period
            .as_duration()
            .try_into()
            .map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

        let period_query = sqlx::query!(
            "WITH
                 p_start AS (
                     SELECT
                         height,
                         slot_time,
                         cumulative_finalization_time
                     FROM blocks
                     WHERE (NOW() - $1::interval) <= slot_time
                     LIMIT 1
                 ),
                 p_end AS (
                     SELECT
                         height,
                         slot_time,
                         cumulative_finalization_time
                     FROM blocks
                     ORDER BY slot_time DESC
                     LIMIT 1
                 )
             SELECT
                 p_end.height - p_start.height AS blocks_added,
                 ((p_end.slot_time - p_start.slot_time) /
                     NULLIF(p_end.height - p_start.height, 0)
                 ) AS avg_block_time,
                 (
                     (p_end.cumulative_finalization_time
                         - p_start.cumulative_finalization_time)::float /
                     NULLIF(p_end.height - p_start.height, 0) * 1000
                 ) AS avg_finalization_time_s
             FROM p_start, p_end",
            interval
        )
        .fetch_one(pool)
        .await?;

        let bucket_width = period.bucket_width();
        let bucket_interval: PgInterval =
            bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

        let bucket_query = sqlx::query!(
            "
SELECT
    bucket.bucket_start,
    COALESCE(bucket_last_block.height - bucket_first_block.height, 0) AS y_blocks_added,
    (
        EXTRACT(epoch FROM (bucket_last_block.slot_time - bucket_first_block.slot_time))
            / NULLIF(bucket_last_block.height - bucket_first_block.height, 0)
    )::float AS y_block_time_avg_s,
    (
        (bucket_last_block.cumulative_finalization_time -
            bucket_first_block.cumulative_finalization_time)::float / (
                NULLIF(
                    bucket_last_block.height - bucket_first_block.height,
                    0
                ) * 1000
            )
    ) AS y_finalization_time_avg_s,
    bucket_last_block.total_staked AS y_last_total_micro_ccd_staked
FROM
    date_bin_series($2::INTERVAL, NOW() - $1::INTERVAL, NOW()) AS bucket
LEFT JOIN LATERAL (
    SELECT
        height,
        slot_time,
        cumulative_finalization_time
    FROM blocks
    WHERE slot_time <= bucket.bucket_start
    ORDER BY slot_time DESC
    LIMIT 1
) bucket_first_block ON true
LEFT JOIN LATERAL (
    SELECT
        height,
        slot_time,
        cumulative_finalization_time,
        total_staked
    FROM blocks
    WHERE slot_time < bucket.bucket_end
    ORDER BY slot_time DESC
    LIMIT 1
) bucket_last_block ON true

 ",
            interval,
            bucket_interval
        )
        .fetch_all(pool)
        .await?;

        let mut buckets = BlockMetricsBuckets {
            bucket_width: bucket_width.into(),
            x_time: Vec::new(),
            y_blocks_added: Vec::new(),
            y_block_time_avg: Vec::new(),
            y_finalization_time_avg: Vec::new(),
            y_last_total_micro_ccd_staked: Vec::new(),
        };
        for row in bucket_query {
            buckets.x_time.push(row.bucket_start.ok_or(ApiError::InternalError(
                "Unexpected missing time for bucket".to_string(),
            ))?);
            buckets.y_blocks_added.push(row.y_blocks_added.unwrap_or(0));
            buckets.y_block_time_avg.push(row.y_block_time_avg_s.unwrap_or(0.0));
            buckets.y_finalization_time_avg.push(row.y_finalization_time_avg_s.unwrap_or(0.0));
            buckets
                .y_last_total_micro_ccd_staked
                .push(row.y_last_total_micro_ccd_staked.unwrap_or(0).try_into()?);
        }

        Ok(BlockMetrics {
            blocks_added: period_query.blocks_added.unwrap_or(0),
            avg_block_time: period_query.avg_block_time.map(|i| i.microseconds as f64 / 1000000.0),
            avg_finalization_time: period_query.avg_finalization_time_s,
            last_block_height: latest_block.height,
            last_total_micro_ccd: latest_block.total_amount.try_into()?,
            last_total_micro_ccd_staked: latest_block.total_staked.try_into()?,
            last_total_micro_ccd_released: latest_block
                .total_amount_released
                .unwrap_or(0)
                .try_into()?,
            last_total_micro_ccd_unlocked: None, // TODO implement unlocking schedule
            // TODO check what format this is expected to be in.
            buckets,
        })
    }
}
