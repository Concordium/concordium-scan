using Application.Api.GraphQL.Accounts;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class AccountTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public AccountTest(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }

    [Fact]
    public async Task WriteAndReadAccount_DelegationNull()
    {
        var entity = new AccountBuilder()
            .WithId(0)
            .WithDelegation(null)
            .Build();

        await AddAccount(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = dbContext.Accounts.Single();
        account.Delegation.Should().BeNull();
    }
    
    [Fact]
    public async Task WriteAndReadAccount_DelegationNotNull()
    {
        var entity = new AccountBuilder()
            .WithId(0)
            .WithDelegation(new DelegationBuilder()
                .WithRestakeEarnings(true)
                .Build())
            .Build();

        await AddAccount(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = dbContext.Accounts.Single();
        account.Delegation.Should().NotBeNull();
        account.Delegation!.RestakeEarnings.Should().BeTrue();
    }
    
    private async Task AddAccount(Account entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(entity);
        await dbContext.SaveChangesAsync();
    }

}