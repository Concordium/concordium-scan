namespace Application.Common.FeatureFlags;

public interface IFeatureFlags
{
    bool ConcordiumNodeImportEnabled { get; }
    bool MigrateDatabasesAtStartup { get; }
    bool ConcordiumNodeImportValidationEnabled { get; }
}