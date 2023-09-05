namespace Application.Configurations;

public class FeatureFlagOptions
{
    /// <summary>
    /// This configurations sets if ANY import will be done by the application.
    ///
    /// Hence changing this will affect all importing flows both from node and database.
    ///
    /// Naming is as-is since it is a configuration variable and to avoid making a breaking change.
    /// </summary>
    public bool ConcordiumNodeImportEnabled { get; init; }
    public bool MigrateDatabasesAtStartup { get; init; }
    public bool ConcordiumNodeImportValidationEnabled { get; init; }
}
