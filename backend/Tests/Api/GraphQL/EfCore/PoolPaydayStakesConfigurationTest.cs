using Application.Api.GraphQL.Import;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class PoolPaydayStakesConfigurationTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public PoolPaydayStakesConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_pool_payday_stakes");
    }

    [Fact]
    public async Task ReadWrite()
    {
        var input = new PoolPaydayStakes
        {
            PayoutBlockId = 42,
            PoolId = 7,
            BakerStake = 1000,
            DelegatedStake = 2000
        };

        await Write(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PoolPaydayStakes.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.PayoutBlockId.Should().Be(42);
        result.PoolId.Should().Be(7);
        result.BakerStake.Should().Be(1000);
        result.DelegatedStake.Should().Be(2000);
    }

    private async Task Write(PoolPaydayStakes input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PoolPaydayStakes.Add(input);
        await dbContext.SaveChangesAsync();
    }
}