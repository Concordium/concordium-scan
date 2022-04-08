namespace Application.Api.GraphQL.Metrics;

public record QueryParams(DateTimeOffset FromTime, DateTimeOffset ToTime, TimeSpan BucketWidth)
{
    public static QueryParams Create(MetricsPeriod period)
    {
        var toTime = DateTimeOffset.UtcNow;
        
        return period switch
        {
            MetricsPeriod.LastHour => new QueryParams(toTime.AddHours(-1), toTime, TimeSpan.FromMinutes(2)),
            MetricsPeriod.Last24Hours => new QueryParams(toTime.AddHours(-24), toTime, TimeSpan.FromHours(1)),
            MetricsPeriod.Last7Days => new QueryParams(toTime.AddDays(-7), toTime, TimeSpan.FromHours(6)),
            MetricsPeriod.Last30Days => new QueryParams(toTime.AddDays(-30), toTime, TimeSpan.FromDays(1)),
            MetricsPeriod.LastYear => new QueryParams(toTime.AddYears(-1), toTime, TimeSpan.FromDays(15)),
            _ => throw new NotImplementedException()
        };
    }
};