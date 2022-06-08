using Application.Database;
using Npgsql;
using Tests.TestUtilities.Stubs;

namespace Tests.TestUtilities;

public class DatabaseFixture
{
    private static readonly object LockObject = new();
    private static bool _databaseAlreadyMigrated;

    public const string ConnectionString = "Host=localhost;Port=5432;Database=ccscan_unittest;User ID=postgres;Password=lingo-bingo;Include Error Detail=true;";
    public const string ConnectionStringNodeCache = "Host=localhost;Port=5432;Database=ccscan_node_cache_unittest;User ID=postgres;Password=lingo-bingo;Include Error Detail=true;";
    public DatabaseSettings DatabaseSettings => new()
    {
        ConnectionString = ConnectionString,
        ConnectionStringNodeCache = ConnectionStringNodeCache
    };

    public DatabaseFixture()
    {
        lock(LockObject)
        {
            if (!_databaseAlreadyMigrated)
            {
                var featureFlags = new FeatureFlagsStub(migrateDatabasesAtStartup:true);
                var databaseMigrator = new DatabaseMigrator(DatabaseSettings, featureFlags);
                databaseMigrator.MigrateDatabases();

                Console.WriteLine("Database migrated");
                _databaseAlreadyMigrated = true;
            }
        }
    }

    public NpgsqlConnection GetOpenConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
    
    public NpgsqlConnection GetOpenNodeCacheConnection()
    {
        var connection = new NpgsqlConnection(ConnectionStringNodeCache);
        connection.Open();
        return connection;
    }
}