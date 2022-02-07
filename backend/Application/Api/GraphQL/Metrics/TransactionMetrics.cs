namespace Application.Api.GraphQL.Metrics;

public record TransactionMetrics(int TotalTransactionCount, TransactionMetricsBuckets Buckets);