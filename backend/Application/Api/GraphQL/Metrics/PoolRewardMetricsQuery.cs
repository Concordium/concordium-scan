using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Common;
using Application.Database;
using Dapper;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Npgsql;

namespace Application.Api.GraphQL.Metrics;

[ExtendObjectType(typeof(Query))]
public class PoolRewardMetricsQuery
{
    private readonly DatabaseSettings _dbSettings;
    private readonly ITimeProvider _timeProvider;

    public PoolRewardMetricsQuery(DatabaseSettings dbSettings, ITimeProvider timeProvider)
    {
        _dbSettings = dbSettings;
        _timeProvider = timeProvider;
    }

    public Task<PoolRewardMetrics> GetPoolRewardMetricsForPassiveDelegation(MetricsPeriod period)
    {
        var pool = new PassiveDelegationPoolRewardTarget();
        return GetPoolRewardMetrics(pool, period);
    }

    public Task<PoolRewardMetrics> GetPoolRewardMetricsForBakerPool([ID] long bakerId, MetricsPeriod period)
    {
        var pool = new BakerPoolRewardTarget(bakerId);
        return GetPoolRewardMetrics(pool, period);
    }

    private async Task<PoolRewardMetrics> GetPoolRewardMetrics(PoolRewardTarget pool, MetricsPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var bakerId = PoolRewardTargetToLongConverter.ConvertToLong(pool);
        var queryParams = RewardQueryParams.Create(period, bakerId, _timeProvider);

        var sql = @"select coalesce(sum(total_amount), 0)       as sum_total_amount,
                           coalesce(sum(baker_amount), 0)       as sum_baker_amount,
                           coalesce(sum(delegator_amount), 0)   as sum_delegator_amount
                    from metrics_pool_rewards
                    where time between @FromTime and @ToTime
                    and (pool_id = @BakerId);";
        var data = await conn.QuerySingleAsync(sql, queryParams);

        var sumTotalAmount = (long)data.sum_total_amount;
        var sumBakerAmount = (long)data.sum_baker_amount;
        var sumDelegatorsAmount = (long)data.sum_delegator_amount;

        var bucketParams = queryParams with
        {
            // make sure that the first bucket is a "full bucket" by moving the from-time one bucket
            // width back (and then afterwards remove any buckets that are entirely outside requested interval)
            FromTime = queryParams.FromTime - queryParams.BucketWidth
        };
        var bucketsSql =
            @"select time_bucket_gapfill(@BucketWidth, time)  as interval_start,
                     coalesce(sum(total_amount), 0)           as sum_total_amount,
                     coalesce(sum(baker_amount), 0)           as sum_baker_amount,
                     coalesce(sum(delegator_amount), 0)       as sum_delegator_amount
            from metrics_pool_rewards
            where time between @FromTime and @ToTime
            and (pool_id = @BakerId)
            group by interval_start
            order by interval_start;";
        var bucketData = (List<dynamic>)await conn.QueryAsync(bucketsSql, bucketParams);

        bucketData.RemoveAll(row => ToDateTimeOffset(row.interval_start) <= queryParams.FromTime - queryParams.BucketWidth);

        var buckets = new PoolRewardMetricsBuckets(
            queryParams.BucketWidth,
            bucketData.Select(row => ToDateTimeOffset((DateTime)row.interval_start)).ToArray(),
            bucketData.Select(row => (long)row.sum_total_amount).ToArray(),
            bucketData.Select(row => (long)row.sum_baker_amount).ToArray(),
            bucketData.Select(row => (long)row.sum_delegator_amount).ToArray());
        var result = new PoolRewardMetrics(sumTotalAmount, sumBakerAmount, sumDelegatorsAmount, buckets);
        return result;
    }

    private DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return value.AsUtcDateTimeOffset();
    }
}