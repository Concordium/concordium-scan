using Application.Api.GraphQL.EfCore;
using Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Tests.Api.GraphQL.EfCore;

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
}