using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.PassiveDelegations;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Api.GraphQL.PassiveDelegations;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class PassiveDelegationTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly PassiveDelegation _target;

    public PassiveDelegationTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);

        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    
        _target = new PassiveDelegation();
    }

    [Fact]
    public async Task GetDelegators()
    {
        await AddAccount(
            new AccountBuilder()
                .WithId(1)
                .WithCanonicalAddress("3wuD62JLKKS3VERpPphLrtLKLhym24PvWydMH7pq7t8H32DS8Q", true)
                .WithDelegation(null)
                .Build(),
            new AccountBuilder()
                .WithId(2)
                .WithCanonicalAddress("44MLuxs1FBwN2LwUBqffyXX2WTtTcEbd3V6gv2NhpzkgejJk2C", true)
                .WithDelegation(new DelegationBuilder()
                    .WithDelegationTarget(new PassiveDelegationTarget())
                    .WithStakedAmount(1000)
                    .Build())
                .Build(),
            new AccountBuilder()
                .WithId(3)
                .WithCanonicalAddress("3BYHd1bUu1iLPTX8VUNC97LTCDT3dJkkA3yHj4xG8VpJDQ3mJz", true)
                .WithDelegation(new DelegationBuilder()
                    .WithDelegationTarget(new PassiveDelegationTarget())
                    .WithStakedAmount(2000)
                    .Build())
                .Build(),
            new AccountBuilder()
                .WithId(4)
                .WithCanonicalAddress("42VJy2jsnvv7tBVXXis8E83CUjX7wvcw2LTiA9QKKLDc7y99uN", true)
                .WithDelegation(new DelegationBuilder()
                    .WithDelegationTarget(new BakerDelegationTarget(42))
                    .WithStakedAmount(9000)
                    .Build())
                .Build()
        );

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = _target.GetDelegators(dbContext).ToArray();
        result.Length.Should().Be(2);
        result[0].StakedAmount.Should().Be(2000);
        result[0].AccountAddress.AsString.Should().Be("3BYHd1bUu1iLPTX8VUNC97LTCDT3dJkkA3yHj4xG8VpJDQ3mJz");
        result[1].StakedAmount.Should().Be(1000);
        result[1].AccountAddress.AsString.Should().Be("44MLuxs1FBwN2LwUBqffyXX2WTtTcEbd3V6gv2NhpzkgejJk2C");
    }
    
    private async Task AddAccount(params Account[] entities)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }
}