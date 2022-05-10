﻿using Application.Api.GraphQL.Accounts;
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
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);

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
                .WithPendingChange(null)
                .Build())
            .Build();

        await AddAccount(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = dbContext.Accounts.Single();
        account.Delegation.Should().NotBeNull();
        account.Delegation!.RestakeEarnings.Should().BeTrue();
        account.Delegation.PendingChange.Should().BeNull();
    }
    
    [Fact]
    public async Task WriteAndReadAccount_DelegationNotNull_PendingChangeRemoveDelegation()
    {
        var entity = new AccountBuilder()
            .WithId(0)
            .WithDelegation(new DelegationBuilder()
                .WithRestakeEarnings(true)
                .WithPendingChange(new PendingDelegationRemoval(_anyDateTimeOffset))
                .Build())
            .Build();

        await AddAccount(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = dbContext.Accounts.Single();
        var change = account.Delegation!.PendingChange.Should().BeOfType<PendingDelegationRemoval>().Subject!;
        change.EffectiveTime.Should().Be(_anyDateTimeOffset);
    }
    
    [Fact]
    public async Task WriteAndReadAccount_DelegationNotNull_PendingChangeReduceStake()
    {
        var entity = new AccountBuilder()
            .WithId(0)
            .WithDelegation(new DelegationBuilder()
                .WithRestakeEarnings(true)
                .WithPendingChange(new PendingDelegationReduceStake(_anyDateTimeOffset, 1000))
                .Build())
            .Build();

        await AddAccount(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var account = dbContext.Accounts.Single();
        var change = account.Delegation!.PendingChange.Should().BeOfType<PendingDelegationReduceStake>().Subject!;
        change.EffectiveTime.Should().Be(_anyDateTimeOffset);
        change.NewStakedAmount.Should().Be(1000UL);
    }
    
    private async Task AddAccount(Account entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(entity);
        await dbContext.SaveChangesAsync();
    }

}