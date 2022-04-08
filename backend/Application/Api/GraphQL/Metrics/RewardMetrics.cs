using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record RewardMetrics(
    [property:GraphQLDescription("Sum of all rewards in requested period as micro CCD")]
    long SumRewardAmount,
    
    RewardMetricsBuckets Buckets);
    
public record RewardMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,

    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    
    [property:GraphQLDescription("Sum of rewards as micro CCD within bucket time period. Intended y-axis value.")]
    long[] Y_SumRewards);