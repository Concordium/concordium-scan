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

    public MaterializedViewRefresher(DatabaseSettings dbSettings, IMetrics metrics)
    {
        _dbSettings = dbSettings;
        _metrics = metrics;
    }

    public async Task RefreshAll()
    {
        using var counter = _metrics.MeasureDuration(nameof(MaterializedViewRefresher), nameof(RefreshAll));

        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync("refresh materialized view graphql_baker_statistics");
    }
}