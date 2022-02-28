using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;
using TimestampedAmount = ConcordiumSdk.NodeApi.Types.TimestampedAmount;
using TransferredWithSchedule = ConcordiumSdk.NodeApi.Types.TransferredWithSchedule;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class AccountReleaseScheduleWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly AccountReleaseScheduleWriter _target;

    public AccountReleaseScheduleWriterTest(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new AccountReleaseScheduleWriter(dbFixture.DatabaseSettings);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
    }

    [Fact]
    public async Task AccountExists()
    {
        var account = await CreateAccount("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");

        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"), 
                        new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        await _target.AddAccountReleaseScheduleItems(new []{ input });

        await using var conn = _dbFixture.GetOpenConnection();
        var result = conn.Query("select account_id, transaction_id, schedule_index, timestamp, amount from graphql_account_release_schedule").ToArray();

        result.Length.Should().Be(2);
        AssertEqual(result[0], account.Id, 42, 0, baseTime.AddHours(1), 515151);
        AssertEqual(result[1], account.Id, 42, 1, baseTime.AddHours(2), 4242);
    }

    [Fact]
    public async Task AccountDoesntExist()
    {
        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"), 
                        new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        await _target.AddAccountReleaseScheduleItems(new []{ input });

        await using var conn = _dbFixture.GetOpenConnection();
        var result = conn.Query("select account_id, transaction_id, schedule_index, timestamp, amount from graphql_account_release_schedule").ToArray();

        result.Length.Should().Be(0);
    }

    private static void AssertEqual(dynamic actual, long expectedAccountId, int expectedTransactionId, int expectedScheduleIndex, DateTimeOffset expectedTimestamp, int expectedAmount)
    {
        Assert.Equal(expectedAccountId, actual.account_id);
        Assert.Equal(expectedTransactionId, actual.transaction_id);
        Assert.Equal(expectedScheduleIndex, actual.schedule_index);
        Assert.Equal(expectedTimestamp, DateTime.SpecifyKind(actual.timestamp, DateTimeKind.Utc));
        Assert.Equal(expectedAmount, actual.amount);
    }

    private async Task<Account> CreateAccount(string accountAddress)
    {
        var account = new AccountBuilder().WithAddress(accountAddress).Build();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return account;
    }
}