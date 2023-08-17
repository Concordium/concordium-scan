using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.SmartContract;
using Application.Api.GraphQL.EfCore;
using Application.Database;
using Concordium.Sdk.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tests.TestUtilities.Stubs;

namespace Tests.Aggregates.SmartContract;

public class TestRepository : ISmartContractRepository
{
    internal IList<SmartContractReadHeight> SmartContractAggregateImportStates = new List<SmartContractReadHeight>();
    internal IList<SmartContractEvent> SmartContractEvents = new List<SmartContractEvent>();
    internal IList<ModuleReferenceEvent> ModuleReferenceEvents = new List<ModuleReferenceEvent>();
    internal IList<ModuleReferenceSmartContractLinkEvent> ModuleReferenceSmartContractLinkEvents = new List<ModuleReferenceSmartContractLinkEvent>();

    public IQueryable<T> GetReadOnlyQueryable<T>() where T : class
    {
        if (typeof(T) == typeof(SmartContractReadHeight))
        {
            return SmartContractAggregateImportStates.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(SmartContractEvent))
        {
            return SmartContractEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(ModuleReferenceEvent))
        {
            return ModuleReferenceEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(ModuleReferenceSmartContractLinkEvent))
        {
            return ModuleReferenceSmartContractLinkEvents.Cast<T>().AsQueryable();
        }
        throw new NotImplementedException($"Not implemented for type: {typeof(T)}");
    }

    public ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class
    {
        if (typeof(T) == typeof(SmartContractReadHeight))
        {
            SmartContractAggregateImportStates.Add((entity as SmartContractReadHeight)!);
            return new ValueTask<EntityEntry<T>>();
        }
        if (typeof(T) == typeof(SmartContractEvent))
        {
            SmartContractEvents.Add((entity as SmartContractEvent)!);
            return new ValueTask<EntityEntry<T>>();
        }
        if (typeof(T) == typeof(ModuleReferenceEvent))
        {
            ModuleReferenceEvents.Add((entity as ModuleReferenceEvent)!);
            return new ValueTask<EntityEntry<T>>();
        }
        if (typeof(T) == typeof(ModuleReferenceSmartContractLinkEvent))
        {
            ModuleReferenceSmartContractLinkEvents.Add((entity as ModuleReferenceSmartContractLinkEvent)!);
            return new ValueTask<EntityEntry<T>>();
        }
        throw new NotImplementedException($"Not implemented for type: {typeof(T)}");
    }

    public Task SaveChangesAsync(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

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
    
    [Fact]
    public async Task QueryNode()
    {
        var options = StartupDatabase();
        
        using var cts = new CancellationTokenSource();
        using var client = new ConcordiumClient(new Uri("http://127.0.0.1:20100"), new ConcordiumClientOptions());
        var testDbFactory = new TestDbFactory(options);
        var nodeClient = new SmartContractNodeClient(client);
        var smartContract = new SmartContractAggregate(testDbFactory, nodeClient);

        await smartContract.Import(cts.Token);
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