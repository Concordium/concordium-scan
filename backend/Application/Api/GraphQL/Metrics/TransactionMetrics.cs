using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record TransactionMetrics(
    [property:GraphQLDescription("Total number of transactions added in requested period.")]
    int TransactionsAdded, 
    TransactionMetricsBuckets Buckets);

public record TransactionMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,
    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    [property:GraphQLDescription("Total number of transactions added within the bucket time period. Intended y-axis value.")]
    int[] Y_TransactionsAdded);