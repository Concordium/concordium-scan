using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Common;
using Application.Database;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL.Bakers;

public class ApyQuery
{
    private readonly DatabaseSettings _dbSettings;
    private readonly ITimeProvider _timeProvider;

    public ApyQuery(DatabaseSettings dbSettings, ITimeProvider timeProvider)
    {
        _dbSettings = dbSettings;
        _timeProvider = timeProvider;
    }
    
    public async Task<PoolApy> GetApy(PoolRewardTarget pool, ApyPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var utcNow = _timeProvider.UtcNow;
        var queryParams = new
        {
            FromTime = period switch
            {
                ApyPeriod.Last7Days => utcNow.AddDays(-7),
                ApyPeriod.Last30Days => utcNow.AddDays(-30),
                _ => throw new NotImplementedException()
            },
            ToTime = utcNow,
            PoolId = PoolRewardTargetToLongConverter.ConvertToLong(pool)
        };
            
        var sql = @"
            select exp(avg(ln(total))) - 1      as total_apy_geom_mean,
                   exp(avg(ln(baker))) - 1      as baker_apy_geom_mean,
                   exp(avg(ln(delegators))) - 1 as delegators_apy_geom_mean
            from (select coalesce(total_apy, 0) + 1      as total,
                         coalesce(baker_apy, 0) + 1      as baker,
                         coalesce(delegators_apy, 0) + 1 as delegators
                  from graphql_payday_summaries ps
                           left join metrics_payday_pool_rewards r on r.block_id = ps.block_id
                  where ps.payday_time between @FromTime and @ToTime
                    and r.pool_id = @PoolId) a;";
        
        var result = await conn.QuerySingleAsync(sql, queryParams);
        var totalApy = (double?)result.total_apy_geom_mean;
        var bakerApy = (double?)result.baker_apy_geom_mean;
        var delegatorsApy = (double?)result.delegators_apy_geom_mean;

        return new PoolApy(totalApy, bakerApy, delegatorsApy);
    }
}

public enum ApyPeriod
{
    Last7Days,
    Last30Days
}

public record PoolApy(
    double? TotalApy,
    double? BakerApy,
    double? DelegatorsApy);