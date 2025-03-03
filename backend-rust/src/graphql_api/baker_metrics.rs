use std::time::Duration;
use async_graphql::{Context, Object, SimpleObject};
use crate::graphql_api::{ApiResult, MetricsPeriod};
use chrono::{TimeDelta};

use crate::scalar_types::{DateTime, TimeSpan};

#[derive(Default)]
pub(crate) struct QueryBakerMetrics;

#[Object]
impl QueryBakerMetrics {

    async fn baker_metrics<'a>(&self, ctx: &Context<'a>, period: MetricsPeriod) ->  ApiResult<BakerMetrics> {
        let std_duration = Duration::from_secs(3600);
        let time_delta = TimeDelta::from_std(std_duration)
            .expect("Conversion from std::time::Duration to TimeDelta failed");
        Ok(BakerMetrics {
            bakers_added: 0,
            bakers_removed: 0,
            last_baker_count: 0,
            buckets: BakerMetricsBuckets {
                bucket_width: TimeSpan::from(time_delta),
                y_bakers_added: vec![],
                y_last_baker_count: 0,
                x_time: vec![],
                y_block_time_avg: vec![]
            }
        })
    }

}


#[derive(SimpleObject)]
pub struct BakerMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,
    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,
    #[graphql(name = "y_BakersAdded")]
    y_bakers_added: Vec<u64>,
    #[graphql(name = "y_BakersRemoved")]
    y_block_time_avg: Vec<f64>,
    #[graphql(name = "y_LastBakerCount")]
    y_last_baker_count: u64,
}

#[derive(SimpleObject)]
pub struct BakerMetrics {
    last_baker_count: u64,
    bakers_added: i64,
    bakers_removed: i64,
    buckets: BakerMetricsBuckets,
}

