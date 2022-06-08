using Application.Common.FeatureFlags;

namespace Tests.TestUtilities.Stubs;

public class FeatureFlagsStub : IFeatureFlags
{
    public FeatureFlagsStub(bool migrateDatabasesAtStartup = true)
    {
        MigrateDatabasesAtStartup = migrateDatabasesAtStartup;
    }

    public bool ConcordiumNodeImportEnabled { get; } = true;
    public bool MigrateDatabasesAtStartup { get; private set; }
    public bool ConcordiumNodeImportValidationEnabled { get; } = true;
}