using Application.Common;

namespace Application.Api.GraphQL.Metrics;

public record QueryParams(DateTimeOffset FromTime, DateTimeOffset ToTime, TimeSpan BucketWidth)
{
    public static QueryParams Create(MetricsPeriod period)
    {
        return Create(period, DateTimeOffset.UtcNow);
    }

    public static QueryParams Create(MetricsPeriod period, ITimeProvider timeProvider)
    {
        return Create(period, timeProvider.UtcNow);
    }

    private static QueryParams Create(MetricsPeriod period, DateTimeOffset utcNow)
    {
        return period switch
        {
            MetricsPeriod.LastHour => new QueryParams(utcNow.AddHours(-1), utcNow, TimeSpan.FromMinutes(2)),
            MetricsPeriod.Last24Hours => new QueryParams(utcNow.AddHours(-24), utcNow, TimeSpan.FromHours(1)),
            MetricsPeriod.Last7Days => new QueryParams(utcNow.AddDays(-7), utcNow, TimeSpan.FromHours(6)),
            MetricsPeriod.Last30Days => new QueryParams(utcNow.AddDays(-30), utcNow, TimeSpan.FromDays(1)),
            MetricsPeriod.LastYear => new QueryParams(utcNow.AddYears(-1), utcNow, TimeSpan.FromDays(15)),
            _ => throw new NotImplementedException()
        };
    }
}