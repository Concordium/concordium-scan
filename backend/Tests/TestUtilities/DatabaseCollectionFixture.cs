namespace Tests.TestUtilities;

[CollectionDefinition(DatabaseCollection)]
public sealed class DatabaseCollectionFixture : ICollectionFixture<DatabaseFixture>
{
    internal const string DatabaseCollection = "Database Collection";
}