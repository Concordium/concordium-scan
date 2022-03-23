using Application.Api.GraphQL.EfCore;
using Application.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Tests.TestUtilities.Stubs;

public class GraphQlDbContextFactoryStub : IDbContextFactory<GraphQlDbContext>
{
    private readonly DatabaseSettings _settings;

    public GraphQlDbContextFactoryStub(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public GraphQlDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(_settings.ConnectionString);

        return new GraphQlDbContext(optionsBuilder.Options);
    }
    
    public GraphQlDbContext CreateDbContextWithLog(Action<string> action, LogLevel level = LogLevel.Information)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(_settings.ConnectionString);

        optionsBuilder.LogTo(action, level);
        return new GraphQlDbContext(optionsBuilder.Options);
    }
}