using Application.Configurations;
using Microsoft.Extensions.Options;

namespace Tests.TestUtilities.Stubs;

internal static class FeatureFlagsStub
{
    internal static IOptions<FeatureFlagOptions> Create(Action<FeatureFlagOptions> extraOptions = null)
    {
        var featureFlagOptions = new FeatureFlagOptions
        {
            ConcordiumNodeImportEnabled = true,
            MigrateDatabasesAtStartup = true,
            ConcordiumNodeImportValidationEnabled = true
        };
        extraOptions?.Invoke(featureFlagOptions);
        return Options.Create(featureFlagOptions);
    }
}

