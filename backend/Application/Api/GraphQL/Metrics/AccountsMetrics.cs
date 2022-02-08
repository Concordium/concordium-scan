namespace Application.Api.GraphQL.Metrics;

public record AccountsMetrics(
    long LastCumulativeAccountsCreated, 
    int SumAccountsCreated, 
    AccountsMetricsBuckets Buckets);
    
public record AccountsMetricsBuckets(
    TimeSpan BucketWidth,
    DateTimeOffset[] X_Time,
    long[] Y_LastCumulativeAccountsCreated,
    int[] Y_SumAccountsCreated);
