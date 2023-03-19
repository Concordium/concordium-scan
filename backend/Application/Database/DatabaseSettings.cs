namespace Application.Database;

public class DatabaseSettings
{
    public string ConnectionString { get; init; }
    public string ConnectionStringNodeCache { get; init; }

    /// <summary>
    /// Timeout for each query.
    /// </summary>
    /// <value></value>
    public TimeSpan QueryTimeout { get; init; }
    
    /// <summary>
    /// Timeout for each executed query during Migration.
    /// </summary>
    public TimeSpan MigrationTimeout { get; init; }
}
