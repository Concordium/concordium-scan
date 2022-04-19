using Application.Common;

namespace Application.Api.GraphQL.Metrics;

public record RewardQueryParams(DateTimeOffset FromTime, DateTimeOffset ToTime, TimeSpan BucketWidth, long? BakerId)
{
    public static RewardQueryParams Create(MetricsPeriod period, long? bakerId, ITimeProvider timeProvider)
    {
        var baseParams = QueryParams.Create(period, timeProvider);
        return new RewardQueryParams(baseParams.FromTime, baseParams.ToTime, baseParams.BucketWidth, bakerId);
    }
}