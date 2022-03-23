using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;
using AccountCreated = ConcordiumSdk.NodeApi.Types.AccountCreated;
using CredentialDeployed = ConcordiumSdk.NodeApi.Types.CredentialDeployed;
using TimestampedAmount = ConcordiumSdk.NodeApi.Types.TimestampedAmount;
using Transferred = ConcordiumSdk.NodeApi.Types.Transferred;
using TransferredWithSchedule = ConcordiumSdk.NodeApi.Types.TransferredWithSchedule;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class AccountWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly AccountWriter _target;
    private readonly AccountLookupStub _accountLookupStub;

    public AccountWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _accountLookupStub = new AccountLookupStub();
        _target = new AccountWriter(_dbContextFactory, _accountLookupStub);
    
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
        connection.Execute("TRUNCATE TABLE graphql_account_transactions");
        connection.Execute("TRUNCATE TABLE graphql_account_release_schedule");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(143)]
    public async Task AddAccount_AccountCreated(long accountIndex)
    {
        var slotTime = new DateTimeOffset(2020, 10, 01, 12, 0, 15, TimeSpan.Zero);
        var accountAddress = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        
        var createdAccounts = new [] {
            new AccountInfo
            {
                AccountIndex = (ulong)accountIndex,
                AccountAddress = accountAddress,
                AccountAmount = CcdAmount.FromMicroCcd(57290)
            }};

        await _target.AddAccounts(createdAccounts, slotTime);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = await dbContext.Accounts.SingleAsync();
        account.Id.Should().Be(accountIndex);
        account.CanonicalAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        account.BaseAddress.AsString.Should().Be(accountAddress.GetBaseAddress().AsString);
        account.Amount.Should().Be(57290);
        account.TransactionCount.Should().Be(0);
        account.CreatedAt.Should().Be(slotTime);
    }

    [Fact]
    public async Task AddAccountTransactionRelations_AccountExists_SingleTransactionWithSameAddressTwice()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, 13);

        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(null)
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new AccountCreated(canonicalAddress),
                        new CredentialDeployed("1234", canonicalAddress))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        var returnedResult = await _target.AddAccountTransactionRelations(new[] { input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var readResult = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        readResult.Length.Should().Be(1);
        readResult[0].AccountId.Should().Be(13);
        readResult[0].TransactionId.Should().Be(42);

        returnedResult.Should().BeEquivalentTo(readResult);
    }
    
    [Fact]
    public async Task AddAccountTransactionRelations_AccountExists_MultipleTransactionsWithSameAddress()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, 15);

        var input1 = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(canonicalAddress)
                .Build(),
            new Transaction { Id = 42 });
        var input2 = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(canonicalAddress)
                .Build(),
            new Transaction { Id = 43 });

        var returnedResult = await _target.AddAccountTransactionRelations(new[] { input1, input2 });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var readResult = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        readResult.Length.Should().Be(2);
        readResult[0].AccountId.Should().Be(15);
        readResult[0].TransactionId.Should().Be(42);
        readResult[1].AccountId.Should().Be(15);
        readResult[1].TransactionId.Should().Be(43);
        
        returnedResult.Should().BeEquivalentTo(readResult);
    }
    
    /// <summary>
    /// Some account addresses found in the hierarchy might not exist (example: some reject reasons will include non-existing addresses).
    /// Therefore we will simply avoid creating relations for these addresses.
    /// </summary>
    [Fact]
    public async Task AddAccountTransactionRelations_AccountDoesNotExist()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, null);

        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(null)
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new AccountCreated(canonicalAddress),
                        new CredentialDeployed("1234", canonicalAddress))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        var returnedResult = await _target.AddAccountTransactionRelations(new[] { input });
        returnedResult.Should().BeEmpty();
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var readResult = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        readResult.Should().BeEmpty();
    }
    
    [Fact]
    public async Task AddAccountTransactionRelations_AccountExists_SingleTransactionWithAnAliasAddress()
    {
        var canonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        var aliasAddress = canonicalAddress.CreateAliasAddress(48, 11, 99);
        
        _accountLookupStub.AddToCache(canonicalAddress.GetBaseAddress().AsString, 15);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithSender(canonicalAddress)
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Transferred(CcdAmount.FromCcd(10), canonicalAddress, aliasAddress))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });

        var returnedResult = await _target.AddAccountTransactionRelations(new[] { input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var readResult = await dbContext.AccountTransactionRelations.AsNoTracking().ToArrayAsync();
        readResult.Length.Should().Be(1);
        readResult[0].AccountId.Should().Be(15);
        readResult[0].TransactionId.Should().Be(42);

        returnedResult.Should().BeEquivalentTo(readResult);
    }
    
    [Fact]
    public async Task AddAccountReleaseScheduleItems_AccountsExists_CanonicalAddress()
    {
        var toCanonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        await CreateAccount(13, toCanonicalAddress);
        
        var fromCanonicalAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        await CreateAccount(14, fromCanonicalAddress);

        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        fromCanonicalAddress, 
                        toCanonicalAddress,
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        await _target.AddAccountReleaseScheduleItems(new []{ input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountReleaseScheduleItems.ToArrayAsync();

        result.Length.Should().Be(2);
        AssertEqual(result[0], 13, 42, 0, baseTime.AddHours(1), 515151, 14);
        AssertEqual(result[1], 13, 42, 1, baseTime.AddHours(2), 4242, 14);
    }

    [Fact]
    public async Task AddAccountReleaseScheduleItems_AccountsExists_AliasAddresses()
    {
        var toCanonicalAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        await CreateAccount(10, toCanonicalAddress);
        var toAliasAddress = toCanonicalAddress.CreateAliasAddress(38, 11, 200);
        
        var fromCanonicalAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        await CreateAccount(27, fromCanonicalAddress);
        var fromAliasAddress = fromCanonicalAddress.CreateAliasAddress(10, 79, 5);

        var baseTime = new DateTimeOffset(2021, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        var input = new TransactionPair(
            new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        fromAliasAddress, 
                        toAliasAddress,
                        new []
                        {
                            new TimestampedAmount(baseTime.AddHours(1), CcdAmount.FromMicroCcd(515151)),
                            new TimestampedAmount(baseTime.AddHours(2), CcdAmount.FromMicroCcd(4242)),
                        }))
                    .Build())
                .Build(),
            new Transaction { Id = 42 });
        
        await _target.AddAccountReleaseScheduleItems(new []{ input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountReleaseScheduleItems.ToArrayAsync();

        result.Length.Should().Be(2);
        AssertEqual(result[0], 10, 42, 0, baseTime.AddHours(1), 515151, 27);
        AssertEqual(result[1], 10, 42, 1, baseTime.AddHours(2), 4242, 27);
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
        
        // We do not ever expect a scheduled transfer to complete successfully if either sender or receiver does not exist!
        await Assert.ThrowsAnyAsync<PostgresException>(() => _target.AddAccountReleaseScheduleItems(new []{ input }));
    }

    private static void AssertEqual(AccountReleaseScheduleItem actual, long expectedAccountId, int expectedTransactionId, int expectedScheduleIndex, DateTimeOffset expectedTimestamp, ulong expectedAmount, long expectedFromAccountId)
    {
        Assert.Equal(expectedAccountId, actual.AccountId);
        Assert.Equal(expectedTransactionId, actual.TransactionId);
        Assert.Equal(expectedScheduleIndex, actual.Index);
        Assert.Equal(expectedTimestamp, actual.Timestamp);
        Assert.Equal(expectedAmount, actual.Amount);
        Assert.Equal(expectedFromAccountId, actual.FromAccountId);
    }
    
    private async Task CreateAccount(long accountId, AccountAddress canonicalAccountAddress)
    {
        var account = new AccountBuilder()
            .WithId(accountId)
            .WithCanonicalAddress(canonicalAccountAddress.AsString)
            .WithBaseAddress(canonicalAccountAddress.GetBaseAddress().AsString)
            .Build();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
    }
}