using System.Threading.Tasks;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class TransactionMetricsQuery
{
    private readonly DatabaseSettings _dbSettings;

    public TransactionMetricsQuery(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }
    
    public async Task<TransactionMetrics?> GetTransactionMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = QueryParams.Create(period);

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

        bucketData.RemoveAll(row => ToDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new TransactionMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => ToDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (long)row.last_cumulative_transaction_count).ToArray(),
            bucketData.Select(row => (int)row.transaction_count).ToArray());
        var result = new TransactionMetrics(lastCumulativeTransactionCount, transactionCount, buckets);
        return result;
    }
        
    private DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return value.AsUtcDateTimeOffset();
    }
}