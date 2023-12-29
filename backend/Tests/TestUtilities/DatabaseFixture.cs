using System.IO;
using System.Threading;
using Application.Api.GraphQL.EfCore;
using Application.Database;
using Dapper;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
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

    private readonly DbContextOptions _dbContextOptions;

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

        var featureFlags = FeatureFlagsStub.Create();
        var databaseMigrator = new DatabaseMigrator(DatabaseSettings, featureFlags);
        databaseMigrator.MigrateDatabases();
        
        _dbContextOptions = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(DatabaseSettings.ConnectionString)
            .Options;
    }

    internal static NpgsqlConnection GetOpenConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
    
    internal static NpgsqlConnection GetOpenNodeCacheConnection()
    {
        var connection = new NpgsqlConnection(ConnectionStringNodeCache);
        connection.Open();
        return connection;
    }

    internal GraphQlDbContext CreateGraphQlDbContext() => new(_dbContextOptions);

    internal Mock<IDbContextFactory<GraphQlDbContext>> CreateDbContractFactoryMock()
    {
        var dbFactory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGraphQlDbContext);
        dbFactory.Setup(f => f.CreateDbContext())
            .Returns(CreateGraphQlDbContext);
        return dbFactory;
    }
    
    internal async Task AddAsync<T>(params T[] entity) where T : class
    {
        await using var context = new GraphQlDbContext(_dbContextOptions);
        
        await context.Set<T>()
            .AddRangeAsync(entity);
        await context.SaveChangesAsync();
    }


    internal static async Task TruncateTables(params string[] tables)
    {
        await using var connection = GetOpenConnection();
        foreach (var table in tables)
        {
            await connection.ExecuteAsync($"truncate table {table}");
        }
    }    

    public void Dispose()
    {
        _service.Dispose();
    }
}
