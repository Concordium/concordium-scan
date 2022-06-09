using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class PoolRewardConfigurationTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyTimestamp = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public PoolRewardConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_pool_rewards");
    }

    [Fact]
    public async Task WriteAndRead_BakerPool()
    {
        var input = new PoolReward
        {
            Pool = new BakerPoolRewardTarget(123),
            Timestamp = _anyTimestamp,
            RewardType = RewardType.BakerReward,
            TotalAmount = 1000,
            BakerAmount = 750,
            DelegatorsAmount = 250,
            BlockId = 42,
        };

        await AddPoolReward(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.Pool.Should().BeOfType<BakerPoolRewardTarget>().Which.BakerId.Should().Be(123);
        result.Timestamp.Should().Be(_anyTimestamp);
        result.RewardType.Should().Be(RewardType.BakerReward);
        result.TotalAmount.Should().Be(1000);
        result.BakerAmount.Should().Be(750);
        result.DelegatorsAmount.Should().Be(250);
        result.BlockId.Should().Be(42);
    }

    [Fact]
    public async Task WriteAndRead_PassiveDelegation()
    {
        var input = new PoolReward
        {
            Pool = new PassiveDelegationPoolRewardTarget(),
            Timestamp = _anyTimestamp,
            RewardType = RewardType.BakerReward,
            TotalAmount = 1000,
            BakerAmount = 750,
            DelegatorsAmount = 250,
            BlockId = 42,
        };

        await AddPoolReward(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.Pool.Should().BeOfType<PassiveDelegationPoolRewardTarget>();
    }

    private async Task AddPoolReward(PoolReward input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PoolRewards.Add(input);
        await dbContext.SaveChangesAsync();
    }
}