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

        var lastValuesSql = @"select block_height, total_microccd, total_encrypted_microccd
                    from metrics_blocks
                    where time <= @ToTime
                    order by time desc
                    limit 1;";
        var lastValuesData = await conn.QuerySingleAsync(lastValuesSql, queryParams);

        var lastBlockHeight = (long)lastValuesData.block_height;
        var lastTotalMicroCcd = (long)lastValuesData.total_microccd;
        var lastTotalEncryptedMicroCcd = (long)lastValuesData.total_encrypted_microccd;

        var sql = 
            @"select count(*) as total_block_count,
                     avg(block_time_secs) as avg_block_time_secs
              from metrics_blocks
              where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);

        var totalBlockCount = (int)data.total_block_count;
        var avgBlockTime = data.avg_block_time_secs != null ? Math.Round((double)data.avg_block_time_secs, 1) : (double?)null;
        
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
                   locf(last(total_microccd, time),
                        coalesce((SELECT total_microccd
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as last_total_microccd,
                   min(total_encrypted_microccd)          as min_total_encrypted_microccd,
                   max(total_encrypted_microccd)          as max_total_encrypted_microccd,
                   locf(last(total_encrypted_microccd, time),
                        coalesce((SELECT total_encrypted_microccd
                                  FROM metrics_blocks m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as last_total_encrypted_microccd
            from metrics_blocks
            where time between @FromTime and @ToTime
            group by interval_start
            order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => AsUtcDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new BlockMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => AsUtcDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (int)row.count).ToArray(),
            bucketData.Select(row => (double?)row.min_block_time_secs).ToArray(),
            bucketData.Select(row => row.avg_block_time_secs != null ? Math.Round((double)row.avg_block_time_secs, 1) : (double?)null).ToArray(),
            bucketData.Select(row => (double?)row.max_block_time_secs).ToArray(),
            bucketData.Select(row => (long)row.last_total_microccd).ToArray(),
            bucketData.Select(row => (long?)row.min_total_encrypted_microccd).ToArray(),
            bucketData.Select(row => (long?)row.max_total_encrypted_microccd).ToArray(),
            bucketData.Select(row => (long)row.last_total_encrypted_microccd).ToArray());
        var result = new BlockMetrics(lastBlockHeight, totalBlockCount, avgBlockTime, lastTotalMicroCcd, lastTotalEncryptedMicroCcd, buckets);
        return result;
    }

    public async Task<TransactionMetrics?> GetTransactionMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = CreateQueryParams(period);

        var lastValuesSql = @"select cumulative_transaction_count
                    from metrics_transactions
                    where time <= @ToTime
                    order by time desc
                    limit 1;";
        var lastValuesData = await conn.QuerySingleAsync(lastValuesSql, queryParams);
        
        var sql = @"select count(*) as transaction_count
                    from metrics_transactions
                    where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        
        var lastCumulativeTransactionCount = (long)lastValuesData.cumulative_transaction_count;
        var transactionCount = (int)data.transaction_count;

        var bucketParams = queryParams with
        {
            // make sure that the first bucket is a "full bucket" by moving the from-time one bucket
            // width back (and then afterwards remove any buckets that are entirely outside requested interval)
            FromTime = queryParams.FromTime - queryParams.BucketWidth
        };
        
        var bucketsSql = 
            @"select time_bucket_gapfill(@BucketWidth, time) as interval_start,
                     locf(
                         last(cumulative_transaction_count, time),
                         coalesce(
                             (SELECT cumulative_transaction_count FROM metrics_transactions m2 WHERE m2.time < @FromTime ORDER BY time DESC LIMIT 1), 0)
                     ) as last_cumulative_transaction_count,
                     coalesce(count(*), 0) as transaction_count
              from metrics_transactions
              where time between @FromTime and @ToTime
              group by interval_start
              order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => AsUtcDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new TransactionMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => AsUtcDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (long)row.last_cumulative_transaction_count).ToArray(),
            bucketData.Select(row => (int)row.transaction_count).ToArray());
        var result = new TransactionMetrics(lastCumulativeTransactionCount, transactionCount, buckets);
        return result;
    }

    public async Task<AccountsMetrics?> GetAccountsMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = CreateQueryParams(period);

        // TODO: Still need to figure out what to do if no new accounts have been created in requested time interval!
        //       For "sum_accounts_created": Should use gap-filling setting value to zero
        //       For "last_cumulative_accounts_created": LOCF (last object carried forward).
        //                                               A little tricky though, because the first value would be outside the dataset
        //       Besides that: LOCF and gap filling in timescaledb is community edition, requires non-azure-hosted instance
        var sql = @"select max(cumulative_accounts_created) as last_cumulative_accounts_created, sum(accounts_created) as sum_accounts_created
                    from metrics_accounts
                    where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        if (data.total_transaction_count == 0)
            return null; // Means "no data" - makes no sense (there will always be accounts - maybe not any new accounts)
                         // will be removed once TODO above is taken care of!
        
        var lastCumulativeAccountsCreated = (long)data.last_cumulative_accounts_created;
        var sumAccountsCreated = (int)data.sum_accounts_created;

        var bucketParams = queryParams with { FromTime = queryParams.FromTime - queryParams.BucketWidth }; 
        var bucketsSql = @"select time_bucket(@BucketWidth, time) as interval_start, 
                                  last(cumulative_accounts_created, time) as last_cumulative_accounts_created,
                                  sum(accounts_created) as sum_accounts_created
                           from metrics_accounts
                           where time between @FromTime and @ToTime
                           group by interval_start
                           order by interval_start desc;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => AsUtcDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new AccountsMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => AsUtcDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (long)row.last_cumulative_accounts_created).ToArray(),
            bucketData.Select(row => (int)row.sum_accounts_created).ToArray());
        var result = new AccountsMetrics(lastCumulativeAccountsCreated, sumAccountsCreated, buckets);
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