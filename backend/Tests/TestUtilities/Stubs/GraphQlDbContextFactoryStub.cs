using Application.Api.GraphQL.EfCore;
using Application.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tests.TestUtilities.Stubs;

public class GraphQlDbContextFactoryStub : IDbContextFactory<GraphQlDbContext>
{
    private readonly DatabaseSettings _settings;
    private readonly ITestOutputHelper? _testOutputHelper;
    private readonly LogLevel _logLevel;

    public GraphQlDbContextFactoryStub(DatabaseSettings settings) : this(settings, null)
    {
    }

    public GraphQlDbContextFactoryStub(DatabaseSettings settings, ITestOutputHelper? testOutputHelper, LogLevel logLevel = LogLevel.Information)
    {
        _settings = settings;
        _testOutputHelper = testOutputHelper;
        _logLevel = logLevel;
    }

    public GraphQlDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(_settings.ConnectionString);

        if (_testOutputHelper != null)
            optionsBuilder.LogTo(_testOutputHelper.WriteLine, _logLevel);
        
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