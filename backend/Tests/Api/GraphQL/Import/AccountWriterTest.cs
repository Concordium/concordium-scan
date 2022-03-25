using Application.Api.GraphQL;
using Application.Api.GraphQL.Import;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class AccountWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly AccountWriter _target;

    public AccountWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new AccountWriter(_dbContextFactory, new NullMetrics());
    
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_account_transactions");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
        connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(143)]
    public async Task InsertAccounts(long accountIndex)
    {
        var time = new DateTimeOffset(2020, 10, 01, 12, 0, 15, TimeSpan.Zero);
        
        var createdAccounts = new [] {
            new Account
            {
                Id = accountIndex,
                BaseAddress = new Application.Api.GraphQL.AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
                CanonicalAddress = "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy",
                Amount = 57290,
                TransactionCount = 0,
                CreatedAt = time
            }};

        await _target.InsertAccounts(createdAccounts);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = await dbContext.Accounts.SingleAsync();
        account.Id.Should().Be(accountIndex);
        account.BaseAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        account.CanonicalAddress.Should().Be("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        account.Amount.Should().Be(57290);
        account.TransactionCount.Should().Be(0);
        account.CreatedAt.Should().Be(time);
    }
    
    [Fact]
    public async Task WriteAccountStatementEntries()
    {
        var input = new AccountStatementEntry
        {
            AccountId = 42,
            Timestamp = new DateTimeOffset(2020, 10, 01, 12, 31, 42, 123, TimeSpan.Zero),
            Amount = 132,
            EntryType = EntryType.AmountEncrypted,
            BlockId = 11,
            TransactionId = 22
        };
        await _target.InsertAccountStatementEntries(new[] { input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountStatementEntries.SingleAsync();
        
        result.Should().NotBeNull();
        result.AccountId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        result.Timestamp.Should().Be(new DateTimeOffset(2020, 10, 01, 12, 31, 42, 123, TimeSpan.Zero));
        result.Amount.Should().Be(132);
        result.EntryType.Should().Be(EntryType.AmountEncrypted);
        result.BlockId.Should().Be(11);
        result.TransactionId.Should().Be(22);
    }
}