using Application.Database;
using Dapper;
using Npgsql;

namespace Application.Common.FeatureFlags;

public class SqlFeatureFlags : IFeatureFlags
{
    private readonly DatabaseSettings _dbSettings;

    public SqlFeatureFlags(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }

    public bool IsEnabled(string featureName)
    {
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        var param = new
        {
            FeatureName = featureName
        };

        var result = conn.QuerySingleOrDefault<bool?>("SELECT enabled FROM feature_flag where feature_name = @FeatureName", param);
        if (!result.HasValue)
            throw new InvalidOperationException($"No feature flag with name '{featureName}' found in database.");
        return result.Value;
    }
}