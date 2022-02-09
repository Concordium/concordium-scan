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

        // TODO: Still need to figure out what to do if no new blocks have been created in requested time interval! Not very likely!
        var sql = 
            @"select max(block_height) as last_block_height,
                     count(*) as total_block_count ,
                     round(avg(block_time_secs), 1) as avg_block_time_secs
              from metrics_block
              where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        if (data.total_block_count == 0)
            return null; // "No data"

        var lastBlockHeight = (long)data.last_block_height;
        var totalBlockCount = (int)data.total_block_count;
        var avgBlockTime = (double)data.avg_block_time_secs;

        var bucketParams = queryParams with { FromTime = queryParams.FromTime - queryParams.BucketWidth }; 
        var bucketsSql = 
            @"select time_bucket(@BucketWidth, time) as interval_start, 
                     count(*) as count, 
                     round(min(block_time_secs), 1) as min_block_time_secs, 
                     round(avg(block_time_secs), 1) as avg_block_time_secs, 
                     round(max(block_time_secs), 1) as max_block_time_secs 
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
            bucketData.Select(row => (double)row.min_block_time_secs).ToArray(),
            bucketData.Select(row => (double)row.avg_block_time_secs).ToArray(),
            bucketData.Select(row => (double)row.max_block_time_secs).ToArray());
        var result = new BlockMetrics(lastBlockHeight, totalBlockCount, avgBlockTime, buckets);
        return result;
    }

    public async Task<TransactionMetrics?> GetTransactionMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = CreateQueryParams(period);

        var sql = @"select count(*) as total_transaction_count
                    from metrics_transaction
                    where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        if (data.total_transaction_count == 0)
            return null; // Means "no data"
        
        var totalTransactionCount = (int)data.total_transaction_count;

        var bucketParams = queryParams with { FromTime = queryParams.FromTime - queryParams.BucketWidth }; 
        var bucketsSql = @"select time_bucket(@BucketWidth, time) as interval_start, count(*) as count 
                    from metrics_transaction
                    where time between @FromTime and @ToTime
                    group by interval_start
                    order by interval_start desc;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => AsUtcDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new TransactionMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => AsUtcDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (int)row.count).ToArray());
        var result = new TransactionMetrics(totalTransactionCount, buckets);
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