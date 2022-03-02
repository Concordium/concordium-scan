using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;
using AccountCreated = ConcordiumSdk.NodeApi.Types.AccountCreated;
using CredentialDeployed = ConcordiumSdk.NodeApi.Types.CredentialDeployed;
using TimestampedAmount = ConcordiumSdk.NodeApi.Types.TimestampedAmount;
using TransferredWithSchedule = ConcordiumSdk.NodeApi.Types.TransferredWithSchedule;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class AccountWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly AccountWriter _target;

    public AccountWriterTest(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new AccountWriter(_dbContextFactory);
    
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_account_transactions");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
    }
    
    [Fact]
    public async Task AddAccount_AccountCreated()
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

    [Fact]
    public async Task AddAccountTransactionRelations_AccountExists_SingleTransactionWithSameAddressTwice()
    {
        var account = await CreateAccount("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");

        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new AccountCreated(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P")),
                        new CredentialDeployed("1234", new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P")))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        await _target.AddAccountTransactionRelations(new[] { input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        result.Length.Should().Be(1);
        result[0].AccountId.Should().Be(account.Id);
        result[0].TransactionId.Should().Be(42);
    }
    
    [Fact]
    public async Task AddAccountTransactionRelations_AccountExists_MultipleTransactionsWithSameAddress()
    {
        var account = await CreateAccount("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");

        var input1 = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"))
                .Build(),
            new Transaction { Id = 42 });
        var input2 = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"))
                .Build(),
            new Transaction { Id = 43 });

        await _target.AddAccountTransactionRelations(new[] { input1, input2 });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        result.Length.Should().Be(2);
        result[0].AccountId.Should().Be(account.Id);
        result[0].TransactionId.Should().Be(42);
        result[1].AccountId.Should().Be(account.Id);
        result[1].TransactionId.Should().Be(43);
    }
    
    [Fact]
    public async Task AddAccountTransactionRelations_AccountDoesNotExist()
    {
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new AccountCreated(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P")),
                        new CredentialDeployed("1234", new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P")))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        await _target.AddAccountTransactionRelations(new[] { input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        result.Length.Should().Be(0);
    }
    
    
    [Fact]
    public async Task AddAccountReleaseScheduleItems_AccountExists()
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
    public async Task AddAccountReleaseScheduleItems_AccountDoesntExist()
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