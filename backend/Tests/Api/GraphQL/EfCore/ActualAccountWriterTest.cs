using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class ActualAccountWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly ActualAccountWriter _target;

    public ActualAccountWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new ActualAccountWriter(_dbContextFactory);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_account_statement_entries");
    }

    [Fact]
    public async Task WriteAccountStatementEntries()
    {
        var input = new AccountStatementEntry
        {
            AccountId = 42,
            Timestamp = new DateTimeOffset(2020, 10, 01, 12, 31, 42, 123, TimeSpan.Zero),
            Amount = 132,
            EntryType = EntryType.AmountEncrypted
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
    }
}