namespace Application.Api.GraphQL.Metrics;

public record TransactionMetricsBuckets(
    TimeSpan BucketWidth,
    DateTimeOffset[] X_Time,
    int[] Y_TransactionCount);