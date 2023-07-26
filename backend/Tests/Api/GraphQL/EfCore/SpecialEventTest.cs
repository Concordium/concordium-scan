using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class SpecialEventTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public SpecialEventTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_special_events");
    }

    [Fact]
    public async Task MintSpecialEvent()
    {
        var entity = new MintSpecialEvent
        {
            BlockId = 42,
            BakingReward = 1000,
            FinalizationReward = 2000,
            PlatformDevelopmentCharge = 3000,
            FoundationAccountAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
        };

        await AddSpecialEvent(entity);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);

        var typed = result.Should().BeOfType<MintSpecialEvent>().Subject;
        typed.BakingReward.Should().Be(1000);
        typed.FinalizationReward.Should().Be(2000);
        typed.PlatformDevelopmentCharge.Should().Be(3000);
        typed.FoundationAccountAddress.Should().Be(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
    }

    [Fact]
    public async Task FinalizationRewardsSpecialEvent()
    {
        var entity = new FinalizationRewardsSpecialEvent
        {
            BlockId = 42,
            Remainder = 1000,
            AccountAddresses = new[]
            {
                new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
                new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy")
            },
            Amounts = new[]
            {
                2000UL,
                3000UL
            }
        };

        await AddSpecialEvent(entity);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);

        var typed = result.Should().BeOfType<FinalizationRewardsSpecialEvent>().Subject;
        typed.Remainder.Should().Be(1000);
        typed.AccountAddresses.Should().Equal(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"), new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"));
        typed.Amounts.Should().Equal(2000UL, 3000UL);
    }
    
    [Fact]
    public async Task BlockRewardsSpecialEvent()
    {
        var entity = new BlockRewardsSpecialEvent
        {
            BlockId = 42,
            TransactionFees = 1000,
            OldGasAccount = 2000,
            NewGasAccount = 3000,
            BakerReward = 4000,
            FoundationCharge =  5000,
            BakerAccountAddress = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"),
            FoundationAccountAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
        };

        await AddSpecialEvent(entity);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);

        var typed = result.Should().BeOfType<BlockRewardsSpecialEvent>().Subject;
        typed.TransactionFees.Should().Be(1000);
        typed.OldGasAccount.Should().Be(2000);
        typed.NewGasAccount.Should().Be(3000);
        typed.BakerReward.Should().Be(4000);
        typed.FoundationCharge.Should().Be(5000);
        typed.BakerAccountAddress.Should().Be(new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"));
        typed.FoundationAccountAddress.Should().Be(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
    }
    
    [Fact]
    public async Task BakingRewardsSpecialEvent()
    {
        var entity = new BakingRewardsSpecialEvent
        {
            BlockId = 42,
            Remainder = 1000,
            AccountAddresses = new[]
            {
                new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
                new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy")
            },
            Amounts = new[]
            {
                2000UL,
                3000UL
            }
        };

        await AddSpecialEvent(entity);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);

        var typed = result.Should().BeOfType<BakingRewardsSpecialEvent>().Subject;
        typed.Remainder.Should().Be(1000);
        typed.AccountAddresses.Should().Equal(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"), new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"));
        typed.Amounts.Should().Equal(2000UL, 3000UL);
    }
    
    [Fact]
    public async Task PaydayAccountRewardSpecialEvent()
    {
        var entity = new PaydayAccountRewardSpecialEvent
        {
            BlockId = 42,
            Account = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
            BakerReward = 1000,
            FinalizationReward = 2000,
            TransactionFees = 3000
        };

        await AddSpecialEvent(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        
        var typed = result.Should().BeOfType<PaydayAccountRewardSpecialEvent>().Subject;
        typed.Account.Should().Be(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        typed.BakerReward.Should().Be(1000);
        typed.FinalizationReward.Should().Be(2000);
        typed.TransactionFees.Should().Be(3000);
    }
    
    [Fact]
    public async Task BlockAccrueRewardSpecialEvent()
    {
        var entity = new BlockAccrueRewardSpecialEvent
        {
            BlockId = 42,
            TransactionFees = 1000,
            OldGasAccount = 2000,
            NewGasAccount = 3000,
            BakerReward = 4000,
            PassiveReward = 5000,
            FoundationCharge = 6000,
            BakerId = 19
        };

        await AddSpecialEvent(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        
        var typed = result.Should().BeOfType<BlockAccrueRewardSpecialEvent>().Subject;
        typed.TransactionFees.Should().Be(1000);
        typed.OldGasAccount.Should().Be(2000);
        typed.NewGasAccount.Should().Be(3000);
        typed.BakerReward.Should().Be(4000);
        typed.PassiveReward.Should().Be(5000);
        typed.FoundationCharge.Should().Be(6000);
        typed.BakerId.Should().Be(19);
    }
    
    [Fact]
    public async Task PaydayFoundationRewardSpecialEvent()
    {
        var entity = new PaydayFoundationRewardSpecialEvent
        {
            BlockId = 42,
            FoundationAccount = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"),
            DevelopmentCharge = 1000 
        };

        await AddSpecialEvent(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        
        var typed = result.Should().BeOfType<PaydayFoundationRewardSpecialEvent>().Subject;
        typed.FoundationAccount.Should().Be(new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        typed.DevelopmentCharge.Should().Be(1000);
    }
    
    [Theory]
    [InlineData(17L)]
    [InlineData(null)]
    public async Task PaydayPoolRewardSpecialEvent(long? poolOwner)
    {
        PoolRewardTarget pool = poolOwner.HasValue
            ? new BakerPoolRewardTarget(poolOwner.Value)
            : new PassiveDelegationPoolRewardTarget();
        
        var entity = new PaydayPoolRewardSpecialEvent
        {
            BlockId = 42,
            Pool = pool,
            TransactionFees = 1000,
            BakerReward = 2000,
            FinalizationReward = 3000
        };

        await AddSpecialEvent(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.SpecialEvents.Single();
        result.Should().NotBeNull();
        result.BlockId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        
        var typed = result.Should().BeOfType<PaydayPoolRewardSpecialEvent>().Subject;
        typed.Pool.Should().Be(pool);
        typed.TransactionFees.Should().Be(1000);
        typed.BakerReward.Should().Be(2000);
        typed.FinalizationReward.Should().Be(3000);
    }
    
    private async Task AddSpecialEvent(SpecialEvent entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.SpecialEvents.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}