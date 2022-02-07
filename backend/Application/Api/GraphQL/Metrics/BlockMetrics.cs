namespace Application.Api.GraphQL.Metrics;

public record BlockMetrics(int AvgBlockTime, int TotalBlockCount, BlockMetricsBuckets Buckets);