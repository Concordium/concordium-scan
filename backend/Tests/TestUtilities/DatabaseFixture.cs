using System.IO;
using System.Threading;
using Application.Database;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Npgsql;
using Tests.TestUtilities.Stubs;

namespace Tests.TestUtilities;

public sealed class DatabaseFixture : IDisposable
{
    private readonly ICompositeService _service;
    private const string ConnectionString = "Host=localhost;Port=55432;Database=ccscan_test;User ID=postgres;Password=password;Include Error Detail=true;";
    private const string ConnectionStringNodeCache = "Host=localhost;Port=55432;Database=ccscan_node_cache_test;User ID=postgres;Password=password;Include Error Detail=true;";
    
    internal readonly DatabaseSettings DatabaseSettings = new()
    {
        ConnectionString = ConnectionString,
        ConnectionStringNodeCache = ConnectionStringNodeCache
    };

    public DatabaseFixture()
    {
        var file = Path.Combine(Directory.GetCurrentDirectory(), "TestUtilities/docker-compose.yaml");
        _service = new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(file)
            .RemoveOrphans()
            .WaitForPort("timescaledb-test", "5432/tcp", 30_000)
            .Build()
            .Start();
        
        Thread.Sleep(5_000);
        
        var featureFlags = new FeatureFlagsStub(migrateDatabasesAtStartup:true);
        var databaseMigrator = new DatabaseMigrator(DatabaseSettings, featureFlags);
        databaseMigrator.MigrateDatabases();
    }

    internal NpgsqlConnection GetOpenConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
    
    internal NpgsqlConnection GetOpenNodeCacheConnection()
    {
        var connection = new NpgsqlConnection(ConnectionStringNodeCache);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}