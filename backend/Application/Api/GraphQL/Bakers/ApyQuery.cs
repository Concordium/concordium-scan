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

    public ApyQuery(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }
    
    public async Task<PoolApy> GetApy(PoolRewardTarget pool, ApyPeriod period)
    {
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        var queryParams = new
        {
            PoolId = PoolRewardTargetToLongConverter.ConvertToLong(pool)
        };
            
        var sql = @"
            select total_apy_geom_mean_30_days, baker_apy_geom_mean_30_days, delegators_apy_geom_mean_30_days, 
                   total_apy_geom_mean_7_days, baker_apy_geom_mean_mean_7_days, delegators_apy_geom_mean_mean_7_days
            from graphql_pool_mean_apys
            where pool_id = @PoolId;";
        
        var result = await conn.QuerySingleAsync(sql, queryParams);
        if (period == ApyPeriod.Last7Days)
        {
            var totalApy = (double?)result.total_apy_geom_mean_7_days;
            var bakerApy = (double?)result.baker_apy_geom_mean_mean_7_days;
            var delegatorsApy = (double?)result.delegators_apy_geom_mean_mean_7_days;
            return new PoolApy(totalApy, bakerApy, delegatorsApy);
        }
        if (period == ApyPeriod.Last30Days)
        {
            var totalApy = (double?)result.total_apy_geom_mean_30_days;
            var bakerApy = (double?)result.baker_apy_geom_mean_30_days;
            var delegatorsApy = (double?)result.delegators_apy_geom_mean_30_days;
            return new PoolApy(totalApy, bakerApy, delegatorsApy);
        }

        throw new NotImplementedException();
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