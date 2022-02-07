using System.Threading.Tasks;
using Application.Database;
using Dapper;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class MetricsQuery
{
    private readonly DatabaseSettings _dbSettings;

    public MetricsQuery(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }

    public async Task<BlockMetrics?> GetBlockMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = CreateQueryParams(period);

        var sql = @"select round(avg(block_time_secs)) as avg_block_time_secs, count(*) as total_block_count
                    from metrics_block
                    where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        if (data.total_block_count == 0)
            return null; // Means "no data"
        
        var avgBlockTime = (int)data.avg_block_time_secs;
        var totalBlockCount = (int)data.total_block_count;

        var bucketParams = queryParams with { FromTime = queryParams.FromTime - queryParams.BucketWidth }; 
        var bucketsSql = @"select time_bucket(@BucketWidth, time) as interval_start, count(*) as count, min(block_time_secs) as min_block_time_secs, round(avg(block_time_secs)) as avg_block_time_secs, max(block_time_secs) as max_block_time_secs 
                    from metrics_block
                    where time between @FromTime and @ToTime
                    group by interval_start
                    order by interval_start desc;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => AsUtcDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new BlockMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => AsUtcDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (int)row.count).ToArray(),
            bucketData.Select(row => (int)row.min_block_time_secs).ToArray(),
            bucketData.Select(row => (int)row.avg_block_time_secs).ToArray(),
            bucketData.Select(row => (int)row.max_block_time_secs).ToArray());
        var result = new BlockMetrics(avgBlockTime, totalBlockCount, buckets);
        return result;
    }

    private static DateTimeOffset AsUtcDateTimeOffset(DateTime timestampValue)
    {
        return DateTime.SpecifyKind(timestampValue, DateTimeKind.Utc);
    }

    private static QueryParams CreateQueryParams(MetricsPeriod metricsPeriod)
    {
        var toTime = DateTimeOffset.UtcNow;
        
        return metricsPeriod switch
        {
            MetricsPeriod.LastHour => new QueryParams(toTime.AddHours(-1), toTime, TimeSpan.FromMinutes(2)),
            MetricsPeriod.Last24Hours => new QueryParams(toTime.AddHours(-24), toTime, TimeSpan.FromHours(1)),
            MetricsPeriod.Last7Days => new QueryParams(toTime.AddDays(-7), toTime, TimeSpan.FromHours(6)),
            MetricsPeriod.Last30Days => new QueryParams(toTime.AddDays(-30), toTime, TimeSpan.FromDays(1)),
            MetricsPeriod.LastYear => new QueryParams(toTime.AddYears(-1), toTime, TimeSpan.FromDays(15)),
            _ => throw new NotImplementedException()
        };
    }

    private record QueryParams(DateTimeOffset FromTime, DateTimeOffset ToTime, TimeSpan BucketWidth);
}