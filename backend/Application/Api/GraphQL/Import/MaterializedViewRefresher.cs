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
    private long? _lastRefreshTime = null;

    public MaterializedViewRefresher(DatabaseSettings dbSettings, IMetrics metrics)
    {
        _dbSettings = dbSettings;
        _metrics = metrics;
    }

    public async Task RefreshAllIfNeeded()
    {
        var currentTickCount = Environment.TickCount64;
        if (!_lastRefreshTime.HasValue || currentTickCount > _lastRefreshTime.Value + 10_000)
        {
            using var counter = _metrics.MeasureDuration(nameof(MaterializedViewRefresher), nameof(RefreshAllIfNeeded));

            await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("refresh materialized view graphql_baker_statistics");
            
            _lastRefreshTime = currentTickCount;
        }
    }
}