use std::sync::Arc;
use crate::{
    graphql_api::{ApiResult, MetricsPeriod},
    scalar_types::{DateTime, TimeSpan},
};
use async_graphql::{types, Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::{PgPool, Pool};
use sqlx::postgres::types::PgInterval;
use crate::graphql_api::{get_pool, ApiError};

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
        account_id: types::ID
    ) -> ApiResult<RewardMetrics> {
        reward_metrics(period, Some(account_id), get_pool(ctx)?).await
    }

}

async fn reward_metrics(period: MetricsPeriod, account_id: Option<types::ID>, pool: &PgPool) -> ApiResult<RewardMetrics> {
    let end_time = Utc::now();
    let before_time = end_time - period.as_duration();
    let bucket_width = period.bucket_width();

    let bucket_interval: PgInterval =
        bucket_width.try_into().map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;

    let rows = sqlx::query_file!(
        "src/graphql_api/reward_metrics.sql",
        end_time,
        before_time,
        bucket_interval
    )
    .fetch_all(pool)
    .await?;

    let first_row = rows.first().ok_or_else(|| {
        ApiError::InternalError("No metrics found for the given period".to_string())
    })?;

    Ok(RewardMetrics {
        sum_reward_amount
    })


    todo!()
}

#[derive(SimpleObject)]
pub struct RewardMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width:       TimeSpan,
    #[graphql(name = "x_Time")]
    x_time:             Vec<DateTime>,
    #[graphql(name = "y_SumRewards")]
    y_bakers_added:     Vec<u64>,
}

#[derive(SimpleObject)]
pub struct RewardMetrics {
    /// Total rewards at the end of the period
    sum_reward_amount: u64,
    /// Bucket-wise data for bakers added, removed, and the bucket times.
    buckets:          RewardMetricsBuckets,
}
