namespace Application.Database;

public class DatabaseSettings
{
    public string ConnectionString { get; init; }
    public string ConnectionStringNodeCache { get; init; }
    /// <summary>
    /// Configurable timeout for migrations. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan MigrationTimeout { get; init; } = TimeSpan.FromMinutes(5);
}
