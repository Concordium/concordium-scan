use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, MetricsPeriod},
    scalar_types::{DateTime, Long, TimeSpan},
};
use async_graphql::{types, Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::{postgres::types::PgInterval, PgPool};
use std::sync::Arc;

#[derive(Default)]
pub(crate) struct QueryRewardMetrics;

#[Object]
impl QueryRewardMetrics {
    async fn reward_metrics(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
    ) -> ApiResult<RewardMetrics> {
        reward_metrics(period, None, get_pool(ctx)?).await
    }

    async fn reward_metrics_for_account(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
        account_id: types::ID,
    ) -> ApiResult<RewardMetrics> {
        reward_metrics(period, Some(account_id), get_pool(ctx)?).await
    }

    async fn pool_reward_metrics_for_baker_pool(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
        baker_id: types::ID,
    ) -> ApiResult<PoolRewardMetrics> {
        let pool = get_pool(ctx)?;
        let baker_id: i64 = baker_id.try_into().map_err(ApiError::InvalidIdInt)?;
        pool_reward_metrics(period, Some(baker_id), pool).await
    }

    async fn pool_reward_metrics_for_passive_delegation(
        &self,
        ctx: &Context<'_>,
        period: MetricsPeriod,
    ) -> ApiResult<PoolRewardMetrics> {
        let pool = get_pool(ctx)?;
        pool_reward_metrics(period, None, pool).await
    }
}

/// Query PoolRewardMetrics for a given period of a given pool. The pool is
/// identified using the baker/validator id and None represents the passive
/// pool.
async fn pool_reward_metrics(
    period: MetricsPeriod,
    baker_id: Option<i64>,
    pool: &PgPool,
) -> ApiResult<PoolRewardMetrics> {
    let end_time = Utc::now();
    let before_time = end_time - period.as_duration();
    let bucket_width = period.bucket_width();
    let bucket_interval: PgInterval =
        bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;
    let rows = sqlx::query!(
            r#"
            SELECT
                bucket_time.bucket_start AS "bucket_time!",
                COALESCE(sub.accumulated_total_stake, 0)::BIGINT AS "accumulated_total_stake!",
                COALESCE(sub.accumulated_delegators_stake, 0)::BIGINT AS "accumulated_delegators_stake!"
            FROM
                date_bin_series(
                    $3::interval,
                    $2,
                    $1
                ) AS bucket_time
            LEFT JOIN LATERAL (
                SELECT
                    SUM(payday_total_transaction_rewards + payday_total_baking_rewards + payday_total_finalization_rewards) AS accumulated_total_stake,
                    SUM(payday_delegators_transaction_rewards + payday_delegators_finalization_rewards + payday_delegators_baking_rewards) AS accumulated_delegators_stake
                FROM bakers_payday_pool_rewards
                LEFT JOIN blocks ON blocks.height = payday_block_height
                WHERE
                    blocks.slot_time > bucket_time.bucket_start
                    AND blocks.slot_time <= bucket_time.bucket_end
                    AND pool_owner_for_primary_key = $4
            ) sub ON true;
            "#,
            end_time,              // $1
            before_time,           // $2
            bucket_interval,       // $3
            baker_id.unwrap_or(-1) // $4 Note: -1 represents the passive pool.
        )
        .fetch_all(pool)
        .await?;

    let mut x_time = Vec::with_capacity(rows.len());
    let mut y_sum_baker_rewards: Vec<Long> = Vec::with_capacity(rows.len());
    let mut y_sum_delegators_rewards: Vec<Long> = Vec::with_capacity(rows.len());
    let mut y_sum_total_rewards: Vec<Long> = Vec::with_capacity(rows.len());
    let mut sum_total_reward_amount = Long(0);
    let mut sum_delegators_reward_amount = Long(0);

    for row in &rows {
        x_time.push(row.bucket_time);
        let accumulated_total_stake = Long(row.accumulated_total_stake);
        let accumulated_delegator_stake = Long(row.accumulated_delegators_stake);

        y_sum_total_rewards.push(accumulated_total_stake);
        y_sum_baker_rewards.push(accumulated_total_stake - accumulated_delegator_stake);
        y_sum_delegators_rewards.push(accumulated_delegator_stake);
        sum_total_reward_amount += accumulated_total_stake;
        sum_delegators_reward_amount += accumulated_delegator_stake;
    }

    let sum_baker_reward_amount = sum_total_reward_amount - sum_delegators_reward_amount;

    Ok(PoolRewardMetrics {
        sum_baker_reward_amount,
        sum_delegators_reward_amount,
        sum_total_reward_amount,
        buckets: PoolRewardMetricsBuckets {
            bucket_width: TimeSpan(bucket_width),
            x_time,
            y_sum_baker_rewards,
            y_sum_delegators_rewards,
            y_sum_total_rewards,
        },
    })
}

async fn reward_metrics(
    period: MetricsPeriod,
    account_id: Option<types::ID>,
    pool: &PgPool,
) -> ApiResult<RewardMetrics> {
    let end_time = Utc::now();
    let before_time = end_time - period.as_duration();
    let bucket_width = period.bucket_width();

    let bucket_interval: PgInterval =
        bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

    let value: Option<i64> =
        account_id.map(|x| x.try_into().map_err(ApiError::InvalidIdInt)).transpose()?;

    let rows = sqlx::query!(
        r#"
        SELECT
            bucket_time.bucket_start AS "bucket_time!",
            (SELECT COALESCE(SUM(amount), 0)
                FROM metrics_rewards
                WHERE
                    block_slot_time > bucket_time.bucket_start
                    AND block_slot_time <= bucket_time.bucket_end
                    AND (
                        $4::BIGINT IS NULL
                        OR account_index = $4::BIGINT
                    )
            )::BIGINT AS "accumulated_amount!"
        FROM
            date_bin_series(
                $3::interval,
                $2,
                $1
            ) AS bucket_time
        "#,
        end_time,
        before_time,
        bucket_interval,
        value
    )
    .fetch_all(pool)
    .await?;

    let (x_time, y_sum_rewards): (Vec<DateTime>, Vec<i64>) =
        rows.iter().map(|row| (row.bucket_time, row.accumulated_amount)).unzip();

    let sum_reward_amount = y_sum_rewards.iter().sum();

    Ok(RewardMetrics {
        sum_reward_amount,
        buckets: RewardMetricsBuckets {
            bucket_width: TimeSpan(bucket_width),
            x_time,
            y_sum_rewards,
        },
    })
}

#[derive(SimpleObject)]
pub struct RewardMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width:  TimeSpan,
    #[graphql(name = "x_Time")]
    x_time:        Vec<DateTime>,
    #[graphql(name = "y_SumRewards")]
    y_sum_rewards: Vec<i64>,
}

#[derive(SimpleObject)]
pub struct PoolRewardMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width:             TimeSpan,
    #[graphql(name = "x_Time")]
    x_time:                   Vec<DateTime>,
    #[graphql(name = "y_SumTotalRewards")]
    y_sum_total_rewards:      Vec<Long>,
    #[graphql(name = "y_SumBakerRewards")]
    y_sum_baker_rewards:      Vec<Long>,
    #[graphql(name = "y_SumDelegatorsRewards")]
    y_sum_delegators_rewards: Vec<Long>,
}

#[derive(SimpleObject)]
pub struct RewardMetrics {
    /// Total rewards at the end of the interval
    sum_reward_amount: i64,
    /// Bucket-wise data for rewards
    buckets:           RewardMetricsBuckets,
}

#[derive(SimpleObject)]
pub struct PoolRewardMetrics {
    /// Total rewards at the end of the interval
    sum_total_reward_amount: Long,
    /// Baker rewards at the end of the interval
    sum_baker_reward_amount: Long,
    /// Delegator rewards at the end of the interval
    sum_delegators_reward_amount: Long,
    /// Bucket-wise data for rewards
    buckets: PoolRewardMetricsBuckets,
}
