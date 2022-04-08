using System.Threading.Tasks;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class BakerMetricsQuery
{
    private readonly DatabaseSettings _dbSettings;
    private readonly ITimeProvider _timeProvider;

    public BakerMetricsQuery(DatabaseSettings dbSettings, ITimeProvider timeProvider)
    {
        _dbSettings = dbSettings;
        _timeProvider = timeProvider;
    }

    public async Task<BakerMetrics> GetBakerMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = QueryParams.Create(period, _timeProvider);

        var lastValuesSql = @"select total_baker_count
                    from metrics_bakers
                    where time <= @ToTime
                    order by time desc
                    limit 1;";
        var lastValuesData = await conn.QuerySingleAsync(lastValuesSql, queryParams);
        var lastTotalBakerCount = (int)lastValuesData.total_baker_count;

        var bucketParams = queryParams with
        {
            // make sure that the first bucket is a "full bucket" by moving the from-time one bucket
            // width back (and then afterwards remove any buckets that are entirely outside requested interval)
            FromTime = queryParams.FromTime - queryParams.BucketWidth
        };
        var bucketsSql = 
            @"select time_bucket_gapfill(@BucketWidth, time) as interval_start,
                   locf(last(total_baker_count, time),
                        coalesce((SELECT total_baker_count
                                  FROM metrics_bakers m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))              as last_total_baker_count,
                   coalesce(sum(bakers_added), 0)            as bakers_added,
                   coalesce(sum(bakers_removed), 0)          as bakers_removed
            from metrics_bakers
            where time between @FromTime and @ToTime
            group by interval_start
            order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);
        
        bucketData.RemoveAll(row => ToDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new BakerMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => ToDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (int)row.last_total_baker_count).ToArray(),
            bucketData.Select(row => (int)row.bakers_added).ToArray(),
            bucketData.Select(row => (int)row.bakers_removed).ToArray());
        var result = new BakerMetrics(lastTotalBakerCount, buckets);
        return result;
    }

    private DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return value.AsUtcDateTimeOffset();
    }
}