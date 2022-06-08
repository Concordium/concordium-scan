namespace Application.Common.FeatureFlags;

public class SettingsBasedFeatureFlags : IFeatureFlags
{
    public bool ConcordiumNodeImportEnabled { get; init; }
    public bool MigrateDatabasesAtStartup { get; init; }
    public bool ConcordiumNodeImportValidationEnabled { get; init; }
}
