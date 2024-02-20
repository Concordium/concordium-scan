using Application.Api.GraphQL.Configurations;
using Application.Api.GraphQL.EfCore;
using Application.Database;
using Dapper;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Tests.Api.Rest;

public class RestTestHelper
{
    private IRequestExecutor? _executor;
    private GraphQlDbContext? _dbContext;
    public IDbContextFactory<GraphQlDbContext> dbContextFactory;
    public GraphQlDbContext DbContext => _dbContext ?? throw new InvalidOperationException("Not initialized.");
    private IRequestExecutor RequestExecutor => _executor ?? throw new InvalidOperationException("Not initialized.");

    public async Task InitializeAsync(DatabaseSettings settings)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<GraphQlDbContext>(options => options.UseNpgsql(settings.ConnectionString));
        
        _executor = await services.AddGraphQLServer().Configure().BuildRequestExecutorAsync();
        dbContextFactory = services.BuildServiceProvider().GetService<IDbContextFactory<GraphQlDbContext>>()!;
        _dbContext = await dbContextFactory.CreateDbContextAsync();

        await using var conn = new NpgsqlConnection(settings.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync("truncate table graphql_blocks");
        await conn.ExecuteAsync("truncate table graphql_transactions");
        await conn.ExecuteAsync("truncate table graphql_accounts");
        await conn.ExecuteAsync("truncate table graphql_account_statement_entries");
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null) 
            await _dbContext.DisposeAsync();
    }
}
