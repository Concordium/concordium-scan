using HotChocolate;

namespace Application.Api.GraphQL.Metrics;

public record AccountsMetrics(
    [property:GraphQLDescription("Total number of accounts created (all time)")]
    long LastCumulativeAccountsCreated, 
    [property:GraphQLDescription("Total number of accounts created in requested period.")]
    int AccountsCreated, 
    AccountsMetricsBuckets Buckets);
    
public record AccountsMetricsBuckets(
    [property:GraphQLDescription("The width (time interval) of each bucket.")]
    TimeSpan BucketWidth,
    [property:GraphQLDescription("Start of the bucket time period. Intended x-axis value.")]
    DateTimeOffset[] X_Time,
    [property:GraphQLDescription("Total number of accounts created (all time) at the end of the bucket period. Intended y-axis value.")]
    long[] Y_LastCumulativeAccountsCreated,
    [property:GraphQLDescription("Number of accounts created within bucket time period. Intended y-axis value.")]
    int[] Y_AccountsCreated);
