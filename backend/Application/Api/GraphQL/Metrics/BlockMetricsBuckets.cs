namespace Application.Api.GraphQL.Metrics;

public record BlockMetricsBuckets(
    TimeSpan BucketWidth,
    DateTimeOffset[] X_Time,
    int[] Y_BlockCount,
    int[] Y_BlockTimeMin,
    int[] Y_BlockTimeAvg,
    int[] Y_BlockTimeMax);