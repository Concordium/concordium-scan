using Application.Api.GraphQL.PassiveDelegations;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class PassiveDelegationConfigurationTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public PassiveDelegationConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_passive_delegation");
    }

    [Fact]
    public async Task WriteAndRead()
    {
        var input = new PassiveDelegation
        {
            Id = -1,
            DelegatorCount = 17,
            DelegatedStake = 4100,
            DelegatedStakePercentage = 0.13m,
            CurrentPaydayDelegatedStake = 12000
        };

        await AddPassiveDelegation(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PassiveDelegations.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.Id.Should().Be(-1);
        result!.DelegatorCount.Should().Be(17);
        result.DelegatedStake.Should().Be(4100);
        result.DelegatedStakePercentage.Should().Be(0.13m);
        result.CurrentPaydayDelegatedStake.Should().Be(12000);
    }

    private async Task AddPassiveDelegation(PassiveDelegation input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PassiveDelegations.Add(input);
        await dbContext.SaveChangesAsync();
    }
}
