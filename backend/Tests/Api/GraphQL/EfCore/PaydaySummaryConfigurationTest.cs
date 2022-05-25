using Application.Api.GraphQL.Payday;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class PaydaySummaryConfigurationTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public PaydaySummaryConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_payday_summaries");
    }
    
    [Fact]
    public async Task WriteAndRead()
    {
        var input = new PaydaySummary
        {
            BlockId = 42,
        };

        await AddPaydaySummary(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydaySummaries.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.BlockId.Should().Be(42);
    }

    private async Task AddPaydaySummary(PaydaySummary input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PaydaySummaries.Add(input);
        await dbContext.SaveChangesAsync();
    }
}