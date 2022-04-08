using System.Threading.Tasks;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class RewardMetricsQuery
{
    private readonly DatabaseSettings _dbSettings;
    private readonly ITimeProvider _timeProvider;

    public RewardMetricsQuery(DatabaseSettings dbSettings, ITimeProvider timeProvider)
    {
        _dbSettings = dbSettings;
        _timeProvider = timeProvider;
    }

    public async Task<RewardMetrics> GetRewardMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = QueryParams.Create(period, _timeProvider);

        var sql = @"select coalesce(sum(amount), 0) as sum_amount
                    from metrics_rewards
                    where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        
        var sumAmount = (int)data.sum_amount;

        var bucketParams = queryParams with
        {
            // make sure that the first bucket is a "full bucket" by moving the from-time one bucket
            // width back (and then afterwards remove any buckets that are entirely outside requested interval)
            FromTime = queryParams.FromTime - queryParams.BucketWidth
        };
        var bucketsSql = 
            @"select time_bucket_gapfill(@BucketWidth, time) as interval_start,
                     coalesce(sum(amount), 0)                as sum_amount
            from metrics_rewards
            where time between @FromTime and @ToTime
            group by interval_start
            order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);
        
        bucketData.RemoveAll(row => ToDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new RewardMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => ToDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (long)row.sum_amount).ToArray());
        var result = new RewardMetrics(sumAmount, buckets);
        return result;
    }

    private DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return value.AsUtcDateTimeOffset();
    }
}