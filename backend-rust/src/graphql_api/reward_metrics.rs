use crate::{
    graphql_api::{ApiResult, MetricsPeriod},
    scalar_types::{DateTime, TimeSpan},
};
use async_graphql::{types, Context, Object, SimpleObject};
use chrono::Utc;
use sqlx::{PgPool, Pool};
use crate::graphql_api::get_pool;

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
    let before_period_row = sqlx::query!(
        r#"
        SELECT COALESCE(SUM(amount), 0) AS sum_amount
            FROM metrics_rewards
            LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
            WHERE blocks.slot_time BETWEEN $2 AND $3
            AND ($1 IS NULL OR account_id = $1)
        "#,
        account_id,
        before_time,
        end_time
    )
    .fetch_optional(pool)
    .await?;

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
    /// Total bakers before the start of the period
    sum_reward_amount: u64,
    /// Bucket-wise data for bakers added, removed, and the bucket times.
    buckets:          RewardMetricsBuckets,
}
