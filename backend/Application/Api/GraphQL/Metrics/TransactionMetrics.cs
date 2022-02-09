using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record TransactionMetrics(
    [property:GraphQLDescription("Total number of transactions (all time)")]
    long LastCumulativeTransactionCount, 
    [property:GraphQLDescription("Total number of transactions in requested period.")]
    int TransactionCount, 
    TransactionMetricsBuckets Buckets);

public record TransactionMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,

    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    
    [property:GraphQLDescription("Total number of transactions (all time) at the end of the bucket period. Intended y-axis value.")]
    long[] Y_LastCumulativeTransactionCount,

    [property:GraphQLDescription("Total number of transactions within the bucket time period. Intended y-axis value.")]
    int[] Y_TransactionCount);