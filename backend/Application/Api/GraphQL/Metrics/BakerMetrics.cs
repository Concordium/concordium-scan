using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record BakerMetrics(
    [property:GraphQLDescription("Current number of bakers")]
    int LastBakerCount,
    BakerMetricsBuckets Buckets);

public record BakerMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,
    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    [property:GraphQLDescription("Number of bakers at the end of the bucket period. Intended y-axis value.")]
    int[] Y_LastBakerCount,
    [property:GraphQLDescription("Number of bakers added within bucket time period. Intended y-axis value.")]
    int[] Y_BakersAdded,
    [property:GraphQLDescription("Number of bakers removed within bucket time period. Intended y-axis value.")]
    int[] Y_BakersRemoved);