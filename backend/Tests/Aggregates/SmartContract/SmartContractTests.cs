using System.Threading;
using Application.Aggregates.SmartContract;
using Application.Aggregates.SmartContract.Configurations;
using Application.Api.GraphQL.EfCore;
using Application.Database;
using Concordium.Sdk.Client;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities.Stubs;

namespace Tests.Aggregates.SmartContract;

public sealed class SmartContractTests
{

    private const string ConnectionString = "Host=localhost;Port=25432;Database=ccscan;User ID=postgres;Password=password;Include Error Detail=true;";
    private const string ConnectionStringNodeCache = "Host=localhost;Port=25432;Database=ccscan_node_cache;User ID=postgres;Password=password;Include Error Detail=true;";
    private readonly DatabaseSettings _databaseSettings = new()
    {
        ConnectionString = ConnectionString,
        ConnectionStringNodeCache = ConnectionStringNodeCache
    };
    
    private DbContextOptions<GraphQlDbContext> StartupDatabase()
    {
        var featureFlags = new FeatureFlagsStub(migrateDatabasesAtStartup:true);
        var databaseMigrator = new DatabaseMigrator(_databaseSettings, featureFlags);
        databaseMigrator.MigrateDatabases();
        var dbContextOptions = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return dbContextOptions;
    }
    
    [Fact(Skip = "Long running")]
    public async Task QueryNode()
    {
        var options = StartupDatabase();
        
        using var cts = new CancellationTokenSource();
        using var client = new ConcordiumClient(new Uri("http://127.0.0.1:20100"), new ConcordiumClientOptions());
        var testDbFactory = new TestDbFactory(options);
        var nodeClient = new SmartContractNodeClient(client);
        var smartContract = new SmartContractAggregate(testDbFactory, new SmartContractAggregateOptions());

        await smartContract.NodeImportJob(nodeClient, cts.Token);
    }
}


internal class TestDbFactory : ISmartContractRepositoryFactory
{
    private readonly DbContextOptions<GraphQlDbContext> _options;

    public TestDbFactory(DbContextOptions<GraphQlDbContext> options)
    {
        _options = options;
    }

    public Task<ISmartContractRepository> CreateAsync()
    {
        var smartContractRepository = new SmartContractRepository(new GraphQlDbContext(_options));
        return Task.FromResult<ISmartContractRepository>(smartContractRepository);
    }
}

