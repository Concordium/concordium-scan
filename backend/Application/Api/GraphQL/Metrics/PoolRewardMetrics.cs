using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record PoolRewardMetrics(
    [property:GraphQLDescription("Sum of all rewards in requested period as micro CCD")]
    long SumTotalRewardAmount,
    [property:GraphQLDescription("Sum of all rewards in requested period that were awarded to the baker (as micro CCD)")]
    long SumBakerRewardAmount,
    [property:GraphQLDescription("Sum of all rewards in requested period that were awarded to the delegators (as micro CCD)")]
    long SumDelegatorsRewardAmount,
    
    PoolRewardMetricsBuckets Buckets);
    
public record PoolRewardMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,

    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    
    [property:GraphQLDescription("Sum of rewards (as micro CCD) within bucket time period. Intended y-axis value.")]
    long[] Y_SumTotalRewards,
    
    [property:GraphQLDescription("Sum of rewards that were awarded to the baker (as micro CCD) within bucket time period. Intended y-axis value.")]
    long[] Y_SumBakerRewards,
    
    [property:GraphQLDescription("Sum of rewards that were awarded to the delegators (as micro CCD) within bucket time period. Intended y-axis value.")]
    long[] Y_SumDelegatorsRewards);