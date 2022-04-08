using System.Threading.Tasks;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class AccountsMetricsQuery
{
    private readonly DatabaseSettings _dbSettings;

    public AccountsMetricsQuery(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }

    public async Task<AccountsMetrics?> GetAccountsMetrics(MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = QueryParams.Create(period);

        var lastValuesSql = @"select cumulative_accounts_created
                    from metrics_accounts
                    where time <= @ToTime
                    order by time desc
                    limit 1;";
        var lastValuesData = await conn.QuerySingleAsync(lastValuesSql, queryParams);
        var lastCumulativeAccountsCreated = (long)lastValuesData.cumulative_accounts_created;

        var sql = @"select coalesce(sum(accounts_created), 0) as sum_accounts_created
                    from metrics_accounts
                    where time between @FromTime and @ToTime;";
        var data = await conn.QuerySingleAsync(sql, queryParams);
        
        var sumAccountsCreated = (int)data.sum_accounts_created;

        var bucketParams = queryParams with
        {
            // make sure that the first bucket is a "full bucket" by moving the from-time one bucket
            // width back (and then afterwards remove any buckets that are entirely outside requested interval)
            FromTime = queryParams.FromTime - queryParams.BucketWidth
        };
        var bucketsSql = @"
            select time_bucket_gapfill(@BucketWidth, time) as interval_start,
                   locf(last(cumulative_accounts_created, time),
                        coalesce((SELECT cumulative_accounts_created
                                  FROM metrics_accounts m2
                                  WHERE m2.time < @FromTime
                                  ORDER BY time DESC
                                  LIMIT 1), 0))           as last_cumulative_accounts_created,
                   coalesce(sum(accounts_created), 0)     as sum_accounts_created
            from metrics_accounts
            where time  between @FromTime and @ToTime
            group by interval_start
            order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => ToDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);
        
        var buckets = new AccountsMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => ToDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (long)row.last_cumulative_accounts_created).ToArray(),
            bucketData.Select(row => (int)row.sum_accounts_created).ToArray());
        var result = new AccountsMetrics(lastCumulativeAccountsCreated, sumAccountsCreated, buckets);
        return result;
    }

    private DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return value.AsUtcDateTimeOffset();
    }
}