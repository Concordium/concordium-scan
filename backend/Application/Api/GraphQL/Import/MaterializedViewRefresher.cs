using System.Threading.Tasks;
using Application.Common.Diagnostics;
using Application.Database;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL.Import;

public class MaterializedViewRefresher
{
    private readonly DatabaseSettings _dbSettings;
    private readonly IMetrics _metrics;
    private long? _lastRefreshBakerStatsTime = null;

    public MaterializedViewRefresher(DatabaseSettings dbSettings, IMetrics metrics)
    {
        _dbSettings = dbSettings;
        _metrics = metrics;
    }

    public async Task RefreshAllIfNeeded(BlockWriteResult blockWriteResult)
    {
        await RefreshBakerStatsIfNeeded();
        await RefreshPoolApysIfNeeded(blockWriteResult);
    }

    private async Task RefreshBakerStatsIfNeeded()
    {
        var currentTickCount = Environment.TickCount64;
        if (!_lastRefreshBakerStatsTime.HasValue || currentTickCount > _lastRefreshBakerStatsTime.Value + 10_000)
        {
            using var counter = _metrics.MeasureDuration(nameof(MaterializedViewRefresher), nameof(RefreshBakerStatsIfNeeded));

            await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("refresh materialized view graphql_baker_statistics");

            _lastRefreshBakerStatsTime = currentTickCount;
        }
    }

    private async Task RefreshPoolApysIfNeeded(BlockWriteResult blockWriteResult)
    {
        if (blockWriteResult.PaydayStatus is FirstBlockAfterPayday)
        {
            using var counter = _metrics.MeasureDuration(nameof(MaterializedViewRefresher), nameof(RefreshPoolApysIfNeeded));

            await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("refresh materialized view graphql_pool_mean_apys");
        }
    }
}