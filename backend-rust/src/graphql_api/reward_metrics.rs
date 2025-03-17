use crate::{
    graphql_api::{get_pool, ApiError, ApiResult, MetricsPeriod},
    scalar_types::{DateTime, TimeSpan},
};
use async_graphql::{types, Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::{postgres::types::PgInterval, PgPool};
use std::sync::Arc;

#[derive(Default)]
pub(crate) struct QueryRewardMetrics;

#[Object]
impl QueryRewardMetrics {
    async fn reward_metrics<'a>(
        &self,
        ctx: &Context<'a>,
        period: MetricsPeriod,
    ) -> ApiResult<RewardMetrics> {
        reward_metrics(period, None, get_pool(ctx)?).await
    }

    async fn reward_metrics_for_account<'a>(
        &self,
        ctx: &Context<'a>,
        period: MetricsPeriod,
        account_id: types::ID,
    ) -> ApiResult<RewardMetrics> {
        reward_metrics(period, Some(account_id), get_pool(ctx)?).await
    }
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

    let rows = sqlx::query_file!(
        "src/graphql_api/reward_metrics.sql",
        end_time,
        before_time,
        bucket_interval,
        value
    )
    .fetch_all(pool)
    .await?;

    let sum_reward_amount: u64 = match rows.first() {
        None => Err(ApiError::InternalError("No metrics found for the given period".to_string())),
        Some(row) => row.before_bucket_rewards.try_into().map_err(|_| {
            ApiError::InternalError("Returned reward for entity is less than 0".to_string())
        }),
    }?;

    let (x_time, y_sum_rewards) = rows
        .iter()
        .map(|row| (row.bucket_time, row.after_bucket_rewards - row.before_bucket_rewards))
        .unzip();

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
pub struct RewardMetrics {
    /// Total rewards at the end of the interval
    sum_reward_amount: u64,
    /// Bucket-wise data for rewards
    buckets:           RewardMetricsBuckets,
}
