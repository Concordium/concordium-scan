using Application.Database;
using Npgsql;

namespace Tests.TestUtilities;

public class DatabaseFixture
{
    private static readonly object LockObject = new();
    private static bool _databaseAlreadyMigrated;

    public const string ConnectionString = "Host=localhost;Port=5432;Database=ccscan_unittest;User ID=postgres;Password=lingo-bingo;Include Error Detail=true;";
    public DatabaseSettings DatabaseSettings => new() {ConnectionString = ConnectionString};

    public DatabaseFixture()
    {
        lock(LockObject)
        {
            if (!_databaseAlreadyMigrated)
            {
                var databaseMigrator = new DatabaseMigrator(DatabaseSettings);
                databaseMigrator.MigrateDatabase();

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
}