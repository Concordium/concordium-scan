using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Import;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class AccountWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly AccountWriter _target;
    private readonly DateTimeOffset _anyDateTimeOffset = new(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);

    public AccountWriterTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
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
                BaseAddress = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
                CanonicalAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"),
                Amount = 57290,
                TransactionCount = 0,
                CreatedAt = time
            }};

        await _target.InsertAccounts(createdAccounts);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = await dbContext.Accounts.SingleAsync();
        account.Id.Should().Be(accountIndex);
        account.BaseAddress.AsString.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        account.CanonicalAddress.AsString.Should().Be("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
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
            AccountBalance = 14002,
            EntryType = AccountStatementEntryType.AmountEncrypted,
            BlockId = 11,
            TransactionId = 22
        };
        _target.InsertAccountStatementEntries(new[] { input });

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.AccountStatementEntries.SingleAsync();
        
        result.Should().NotBeNull();
        result.AccountId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        result.Timestamp.Should().Be(new DateTimeOffset(2020, 10, 01, 12, 31, 42, 123, TimeSpan.Zero));
        result.Amount.Should().Be(132);
        result.AccountBalance.Should().Be(14002);
        result.EntryType.Should().Be(AccountStatementEntryType.AmountEncrypted);
        result.BlockId.Should().Be(11);
        result.TransactionId.Should().Be(22);
    }

    [Fact]
    public async Task UpdateAccounts()
    {
        await AddAccounts(
            new AccountBuilder().WithId(10).WithAmount(0).WithTransactionCount(0).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(42).WithAmount(100000).WithTransactionCount(777).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(99).WithAmount(1500).WithTransactionCount(20).WithUniqueAddress().Build());

        var updates = new[]
        {
            new AccountUpdate(10, 2222, 80),
            new AccountUpdate(42, -1000, 3)
        };
        
        var result = _target.UpdateAccounts(updates);
        result.Should().Equal(
            new AccountUpdateResult(10, 0, 2222),
            new AccountUpdateResult(42, 100000, 99000));
    }

    [Fact]
    public async Task UpdateAccount()
    {
        await AddAccounts(
            new AccountBuilder().WithId(10).WithAmount(0).WithTransactionCount(0).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(42).WithAmount(100000).WithTransactionCount(777).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(99).WithAmount(1500).WithTransactionCount(20).WithUniqueAddress().Build());

        var item = new AccountUpdateStub(42, 1000);
        
        await _target.UpdateAccount(item, 
            src => src.AccountId, 
            (src, dst) => dst.Amount += src.ValueToAdd);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var accounts = dbContext.Accounts.OrderBy(x => x.Id).ToArray();
        accounts.Length.Should().Be(3);
        accounts[0].Amount.Should().Be(0UL);
        accounts[1].Amount.Should().Be(101000UL);
        accounts[2].Amount.Should().Be(1500UL);
    }
    
    [Theory]
    [InlineData(10, new long[] {}, new long[] { 10, 11, 12, 13, 14 })]
    [InlineData(59, new long[] {12}, new long[] { 10, 11, 13, 14 })]
    [InlineData(60, new long[] {10, 12}, new long[] { 11, 13, 14 })]
    [InlineData(61, new long[] {10, 12}, new long[] { 11, 13, 14 })]
    [InlineData(90, new long[] {10, 11, 12}, new long[] { 13, 14 })]
    public async Task UpdateAccountsWithPendingDelegationChange(int minutesToAdd, long[] expectedAccountIdsModified, long[] expectedAccountIdsNotModified)
    {
        await AddAccounts(
            new AccountBuilder().WithId(10).WithDelegation(new DelegationBuilder().WithPendingChange(new PendingDelegationRemoval(_anyDateTimeOffset.AddMinutes(60))).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(11).WithDelegation(new DelegationBuilder().WithPendingChange(new PendingDelegationReduceStake(_anyDateTimeOffset.AddMinutes(90), 1000)).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(12).WithDelegation(new DelegationBuilder().WithPendingChange(new PendingDelegationRemoval(_anyDateTimeOffset.AddMinutes(30))).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(13).WithDelegation(new DelegationBuilder().WithPendingChange(null).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(14).WithDelegation(null).WithUniqueAddress().Build());

        await _target.UpdateAccountsWithPendingDelegationChange(_anyDateTimeOffset.AddMinutes(minutesToAdd), baker => baker.Amount = 100);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Accounts.OrderBy(x => x.Id).ToArrayAsync();

        fromDb.Where(x => x.Amount == 100).Select(x => x.Id).Should().Equal(expectedAccountIdsModified);
        fromDb.Where(x => x.Amount != 100).Select(x => x.Id).Should().Equal(expectedAccountIdsNotModified);
    }

    [Fact]
    public async Task UpdateAccounts_WithWhereClauseAndUpdateAction()
    {
        await AddAccounts(
            new AccountBuilder().WithId(10).WithDelegation(new DelegationBuilder().WithDelegationTarget(new BakerDelegationTarget(42)).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(11).WithDelegation(new DelegationBuilder().WithDelegationTarget(new BakerDelegationTarget(43)).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(12).WithDelegation(new DelegationBuilder().WithDelegationTarget(new PassiveDelegationTarget()).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(13).WithDelegation(null).WithUniqueAddress().Build());

        await _target.UpdateAccounts(x => x.Delegation != null && x.Delegation.DelegationTarget == new BakerDelegationTarget(42),
            x => x.Delegation!.DelegationTarget = new PassiveDelegationTarget());
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Accounts.OrderBy(x => x.Id).ToArrayAsync();
        fromDb[0].Delegation!.DelegationTarget.Should().Be(new PassiveDelegationTarget());
        fromDb[1].Delegation!.DelegationTarget.Should().Be(new BakerDelegationTarget(43));
        fromDb[2].Delegation!.DelegationTarget.Should().Be(new PassiveDelegationTarget());
        fromDb[3].Delegation.Should().BeNull();
    }
    
    [Fact]
    public async Task UpdateDelegationStakeIfRestakingEarnings_AccountDoesNotExist()
    {
        var reward = new AccountReward(42, 100);
        await _target.UpdateDelegationStakeIfRestakingEarnings(new[] { reward });
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Accounts.ToArrayAsync();
        result.Length.Should().Be(0);
    }
    
    [Fact]
    public async Task UpdateDelegationStakeIfRestakingEarnings_DelegationNull()
    {
        await AddAccounts(new AccountBuilder().WithId(42).WithDelegation(null).WithUniqueAddress().Build());
    
        var reward = new AccountReward(42, 100);
        await _target.UpdateDelegationStakeIfRestakingEarnings(new[] { reward });
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Accounts.SingleAsync();
        result.Delegation.Should().BeNull();
    }
    
    [Theory]
    [InlineData(false, 1000)]
    [InlineData(true, 1100)]
    public async Task UpdateDelegationStakeIfRestakingEarnings_DelegationNotNull(bool restakeEarnings, ulong expectedResult)
    {
        await AddAccounts(new AccountBuilder().WithId(42).WithDelegation(new DelegationBuilder().WithStakedAmount(1000).WithRestakeEarnings(restakeEarnings).Build()).WithUniqueAddress().Build());

        var bakerStakeUpdate = new AccountReward(42, 100);
        await _target.UpdateDelegationStakeIfRestakingEarnings(new[] { bakerStakeUpdate });
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Accounts.SingleAsync();
        result.Delegation!.StakedAmount.Should().Be(expectedResult);
    }
    
    private record AccountUpdateStub(ulong AccountId, ulong ValueToAdd); 
    
    private async Task AddAccounts(params Account[] entities)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        context.Accounts.AddRange(entities);
        await context.SaveChangesAsync();
    }
}