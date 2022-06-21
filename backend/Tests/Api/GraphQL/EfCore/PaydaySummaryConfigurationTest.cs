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
    private readonly DateTimeOffset _anyTimestamp = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

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
            PaydayTime = _anyTimestamp,
            PaydayDurationSeconds = 7800
        };

        await AddPaydaySummary(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydaySummaries.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.BlockId.Should().Be(42);
        result.PaydayTime.Should().Be(_anyTimestamp);
        result.PaydayDurationSeconds.Should().Be(7800);
    }

    private async Task AddPaydaySummary(PaydaySummary input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PaydaySummaries.Add(input);
        await dbContext.SaveChangesAsync();
    }
}