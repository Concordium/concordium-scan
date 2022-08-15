using System.Threading.Tasks;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class BlockMetricsQuery
{
    private readonly DatabaseSettings _dbSettings;
    private readonly ITimeProvider _timeProvider;

    public BlockMetricsQuery(DatabaseSettings dbSettings, ITimeProvider timeProvider)
    {
        _dbSettings = dbSettings;
        _timeProvider = timeProvider;
    }

    public async Task<BlockMetrics> GetBlockMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = QueryParams.Create(period, _timeProvider);
        var lastReleasedSql = @"SELECT time, total_microccd_released
                FROM graphql_total_microccd_released
                WHERE time <= @ToTime
                ORDER BY time DESC
                limit 1";

        var lastReleasedData = await conn.QuerySingleAsync(lastReleasedSql, queryParams);

        var lastValuesSql = @"select block_height, total_microccd, total_microccd_released, total_microccd_encrypted, total_microccd_staked, total_percentage_released, total_percentage_encrypted, total_percentage_staked
                    from metrics_blocks
                    where time <= @ToTime
                    order by time desc
                    limit 1;";
        var lastValuesData = await conn.QuerySingleAsync(lastValuesSql, queryParams);

        var lastBlockHeight = (long)lastValuesData.block_height;
        var lastTotalMicroCcd = (long)lastValuesData.total_microccd;
        var lastTotalMicroCcdReleased = (long?)lastReleasedData.total_microccd_released;
        var lastTotalMicroCcdEncrypted = (long)lastValuesData.total_microccd_encrypted;
        var lastTotalMicroCcdStaked = (long)lastValuesData.total_microccd_staked;
        var lastTotalPercentageReleased = (double?)lastValuesData.total_percentage_released;
        var lastTotalPercentageEncrypted = (double)lastValuesData.total_percentage_encrypted;
        var lastTotalPercentageStaked = (double)lastValuesData.total_percentage_staked;

        var sql = 
            @"select count(*) as total_block_count,
                     avg(block_time_secs) as avg_block_time_secs,
                     avg(finalization_time_secs) as avg_finalization_time_secs
              from metrics_blocks
              where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);

        var totalBlockCount = (int)data.total_block_count;
        var avgBlockTime = data.avg_block_time_secs != null ? Math.Round((double)data.avg_block_time_secs, 1) : (double?)null;
        var avgFinalizationTime = data.avg_finalization_time_secs != null ? Math.Round((double)data.avg_finalization_time_secs, 1) : (double?)null;
        
        var bucketParams = queryParams with
        {
            // make sure that the first bucket is a "full bucket" by moving the from-time one bucket
            // width back (and then afterwards remove any buckets that are entirely outside requested interval)
            FromTime = queryParams.FromTime - queryParams.BucketWidth
        };
        var bucketsSql = 
            @"select time_bucket_gapfill(@BucketWidth, time) as interval_start,
                   coalesce(count(*), 0)                  as count,
                   min(block_time_secs)                   as min_block_time_secs,
                   avg(block_time_secs)                   as avg_block_time_secs,
                   max(block_time_secs)                   as max_block_time_secs,
                   min(finalization_time_secs)            as min_finalization_time_secs,
                   avg(finalization_time_secs)            as avg_finalization_time_secs,
                   max(finalization_time_secs)            as max_finalization_time_secs,
                   locf(last(total_microccd, time),
                        coalesce((SELECT total_microccd
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as last_total_microccd,
                   min(total_microccd_encrypted)          as min_total_microccd_encrypted,
                   max(total_microccd_encrypted)          as max_total_microccd_encrypted,
                   locf(last(total_microccd_encrypted, time),
                        coalesce((SELECT total_microccd_encrypted
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as last_total_microccd_encrypted,
                   locf(min(total_microccd_staked),
                        coalesce((SELECT total_microccd_staked
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as min_total_microccd_staked,
                   locf(max(total_microccd_staked),
                        coalesce((SELECT total_microccd_staked
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as max_total_microccd_staked,
                   locf(last(total_microccd_staked, time),
                        coalesce((SELECT total_microccd_staked
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as last_total_microccd_staked
            from metrics_blocks
            where time between @FromTime and @ToTime
            group by interval_start
            order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => ToDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new BlockMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => ToDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (int)row.count).ToArray(),
            bucketData.Select(row => (double?)row.min_block_time_secs).ToArray(),
            bucketData.Select(row => row.avg_block_time_secs != null ? Math.Round((double)row.avg_block_time_secs, 1) : (double?)null).ToArray(),
            bucketData.Select(row => (double?)row.max_block_time_secs).ToArray(),
            bucketData.Select(row => (double?)row.min_finalization_time_secs).ToArray(),
            bucketData.Select(row => row.avg_finalization_time_secs != null ? Math.Round((double)row.avg_finalization_time_secs, 1) : (double?)null).ToArray(),
            bucketData.Select(row => (double?)row.max_finalization_time_secs).ToArray(),
            bucketData.Select(row => (long)row.last_total_microccd).ToArray(),
            bucketData.Select(row => (long?)row.min_total_microccd_encrypted).ToArray(),
            bucketData.Select(row => (long?)row.max_total_microccd_encrypted).ToArray(),
            bucketData.Select(row => (long)row.last_total_microccd_encrypted).ToArray(),
            bucketData.Select(row => (long)row.min_total_microccd_staked).ToArray(),
            bucketData.Select(row => (long)row.max_total_microccd_staked).ToArray(),
            bucketData.Select(row => (long)row.last_total_microccd_staked).ToArray());
        
        var result = new BlockMetrics(lastBlockHeight, totalBlockCount, avgBlockTime, avgFinalizationTime, 
            lastTotalMicroCcd, lastTotalMicroCcdReleased, lastTotalMicroCcdEncrypted, lastTotalMicroCcdStaked, 
            lastTotalPercentageReleased, lastTotalPercentageEncrypted, lastTotalPercentageStaked, buckets);
        return result;
    }

    private DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return value.AsUtcDateTimeOffset();
    }
}