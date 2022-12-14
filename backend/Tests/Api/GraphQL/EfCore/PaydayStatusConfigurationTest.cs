using Application.Api.GraphQL;
using Application.Api.GraphQL.Payday;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class PaydayStatusConfigurationTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2020, 10, 01, 12, 31, 42, 123, TimeSpan.Zero);

    public PaydayStatusConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_payday_status");
    }
    
    [Fact]
    public async Task WriteAndRead()
    {
        var input = new PaydayStatus
        {
            PaydayStartTime = _anyDateTimeOffset,
            NextPaydayTime = _anyDateTimeOffset.AddHours(2),
            ProtocolVersion = 4
        };

        await AddPaydayStatus(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydayStatuses.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.PaydayStartTime.Should().Be(_anyDateTimeOffset);
        result.NextPaydayTime.Should().Be(_anyDateTimeOffset.AddHours(2));
        result.ProtocolVersion.Should().Be(4);
    }

    private async Task AddPaydayStatus(PaydayStatus input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PaydayStatuses.Add(input);
        await dbContext.SaveChangesAsync();
    }
}