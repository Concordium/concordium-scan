using Application.Database;
using Tests.TestUtilities.Stubs;

namespace Tests.DatabaseScripts;

/// <summary>
/// Validates the database scripts embedded in the DatabaseScripts project.
/// If this test fails, something is wrong with the naming of the scripts.
/// </summary>
public class SqlScriptNamingConventionsTest
{
    [Fact]
    public void ValidateScriptNamingConventions()
    {
        var connectionString = ""; // this test will not execute any scripts, so connection string is irrelevant! 
        var settings = new DatabaseSettings {ConnectionString = connectionString};

        var featureFlags = FeatureFlagsStub.Create();
        var databaseMigrator = new DatabaseMigrator(settings, featureFlags);
        databaseMigrator.EnsureScriptNamingConventionsFollowed();
    }
}