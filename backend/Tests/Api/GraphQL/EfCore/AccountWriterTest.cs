using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class AccountWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly AccountWriter _target;

    public AccountWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new AccountWriter(_dbContextFactory);
    
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }
    
    [Fact]
    public async Task Account_AccountCreated()
    {
        var slotTime = new DateTimeOffset(2020, 10, 01, 12, 0, 15, TimeSpan.Zero);
        
        var createdAccounts = new [] {
            new AccountInfo { AccountAddress = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd") }};

        await _target.AddAccounts(createdAccounts, slotTime);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = await dbContext.Accounts.SingleAsync();
        account.Id.Should().BeGreaterThan(0);
        account.Address.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        account.CreatedAt.Should().Be(slotTime);
    }
}