using System.Collections.Generic;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Database;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class MetricsWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly MetricsWriter _target;
    private readonly DatabaseSettings _databaseSettings;
    private readonly DateTimeOffset _anyDateTimeOffset = new(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public MetricsWriterTest(DatabaseFixture dbFixture)
    {
        _databaseSettings = dbFixture.DatabaseSettings;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);

        _target = new MetricsWriter(_databaseSettings, new NullMetrics());
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_blocks");
        connection.Execute("TRUNCATE TABLE metrics_bakers");
        connection.Execute("TRUNCATE TABLE metrics_rewards");
        connection.Execute("TRUNCATE TABLE metrics_pool_rewards");
        connection.Execute("TRUNCATE TABLE metrics_payday_pool_rewards");
    }

    [Fact]
    public async Task AddBlockMetrics()
    {
        await _target.AddBlockMetrics(new BlockBuilder()
            .WithBlockHeight(42)
            .WithBlockStatistics(new BlockStatisticsBuilder()
                .WithBlockTime(10.1d)
                .Build())
            .WithBalanceStatistics(new BalanceStatisticsBuilder()
                .WithTotalAmount(10000)
                .WithTotalAmountReleased(9000)
                .WithTotalAmountEncrypted(1000)
                .WithTotalAmountStaked(2500)
                .Build())
            .Build());
        
        var result = QuerySingle(@"
            select time, block_height, block_time_secs, finalization_time_secs, total_microccd, total_microccd_released, total_microccd_encrypted, total_microccd_staked
            from metrics_blocks");

        Assert.Equal(42, result.block_height);
        Assert.Equal(10.1d, result.block_time_secs);
        Assert.Null(result.finalization_time_secs);
        Assert.Equal(10000, result.total_microccd);
        Assert.Equal(9000, result.total_microccd_released);
        Assert.Equal(1000, result.total_microccd_encrypted);
        Assert.Equal(2500, result.total_microccd_staked);
    }

    [Theory]
    [InlineData(10000UL, 10000UL, 1.0d)]
    [InlineData(10000UL, 5000UL, 0.5d)]
    [InlineData(333UL, 311UL, 0.9339339339d)]
    [InlineData(ulong.MaxValue, 0UL, 0d)]
    [InlineData(ulong.MaxValue, ulong.MaxValue/100, 0.01d)]
    [InlineData(10000UL, null, null)]
    public async Task AddBlockMetrics_PercentageTotalAmountReleased(ulong totalAmount, ulong? totalAmountReleased, double? expectedResult)
    {
        await _target.AddBlockMetrics(new BlockBuilder()
            .WithBalanceStatistics(new BalanceStatisticsBuilder()
                .WithTotalAmount(totalAmount)
                .WithTotalAmountReleased(totalAmountReleased)
                .Build())
            .Build());
        
        var result = QuerySingle(@"
            select total_percentage_released
            from metrics_blocks");

        Assert.Equal(expectedResult, result.total_percentage_released);
    }
    
    [Theory]
    [InlineData(10000UL, 10000UL, 1.0d)]
    [InlineData(10000UL, 5000UL, 0.5d)]
    [InlineData(333UL, 311UL, 0.9339339339d)]
    [InlineData(ulong.MaxValue, 0UL, 0d)]
    [InlineData(ulong.MaxValue, ulong.MaxValue/100, 0.01d)]
    public async Task AddBlockMetrics_PercentageTotalAmountStaked(ulong totalAmount, ulong totalAmountStaked, double? expectedResult)
    {
        await _target.AddBlockMetrics(new BlockBuilder()
            .WithBalanceStatistics(new BalanceStatisticsBuilder()
                .WithTotalAmount(totalAmount)
                .WithTotalAmountStaked(totalAmountStaked)
                .Build())
            .Build());
        
        var result = QuerySingle(@"
            select total_percentage_staked
            from metrics_blocks");

        Assert.Equal(expectedResult, result.total_percentage_staked);
    }
    
    [Theory]
    [InlineData(10000UL, 10000UL, 1.0d)]
    [InlineData(10000UL, 5000UL, 0.5d)]
    [InlineData(333UL, 311UL, 0.9339339339d)]
    [InlineData(ulong.MaxValue, 0UL, 0d)]
    [InlineData(ulong.MaxValue, ulong.MaxValue/100, 0.01d)]
    public async Task AddBlockMetrics_PercentageTotalAmountEncrypted(ulong totalAmount, ulong totalAmountEncrypted, double? expectedResult)
    {
        await _target.AddBlockMetrics(new BlockBuilder()
            .WithBalanceStatistics(new BalanceStatisticsBuilder()
                .WithTotalAmount(totalAmount)
                .WithTotalAmountEncrypted(totalAmountEncrypted)
                .Build())
            .Build());
        
        var result = QuerySingle(@"
            select total_percentage_encrypted
            from metrics_blocks");

        Assert.Equal(expectedResult, result.total_percentage_encrypted);
    }
    
    [Fact]
    public async Task AddBakerMetrics_NoChanges()
    {
        var input = new BakerUpdateResultsBuilder()
            .WithBakersAddedCount(0)
            .WithBakersRemovedCount(0)
            .Build();

        var importState = new ImportStateBuilder()
            .WithTotalBakerCount(10)
            .Build();
        
        await _target.AddBakerMetrics(_anyDateTimeOffset, input, importState);
        
        var result = Query(@"
            select time, total_baker_count, bakers_added, bakers_removed
            from metrics_bakers");

        result.Should().BeEmpty();
    }
    
    [Theory]
    [InlineData(10, 5, 15)]
    [InlineData(2, 0, 12)]
    [InlineData(0, 4, 6)]
    public async Task AddBakerMetrics_Changes(int bakersAdded, int bakersRemoved, int expectedTotalCount)
    {
        var input = new BakerUpdateResultsBuilder()
            .WithBakersAddedCount(bakersAdded)
            .WithBakersRemovedCount(bakersRemoved)
            .Build();

        var importState = new ImportStateBuilder()
            .WithTotalBakerCount(10)
            .Build();
        
        await _target.AddBakerMetrics(_anyDateTimeOffset, input, importState);
        
        var result = Query(@"
            select time, total_baker_count, bakers_added, bakers_removed
            from metrics_bakers");

        var item = Assert.Single(result);
        Assert.Equal(expectedTotalCount, item.total_baker_count);
        Assert.Equal(bakersAdded, item.bakers_added);
        Assert.Equal(bakersRemoved, item.bakers_removed);
        
        Assert.Equal(expectedTotalCount, importState.TotalBakerCount);
    }

    [Fact]
    public void AddRewardMetrics()
    {
        var input = new RewardsSummary(new[]
        {
            new AccountRewardSummaryBuilder().WithAccountId(10).WithTotalAmount(1000).Build(),
            new AccountRewardSummaryBuilder().WithAccountId(421).WithTotalAmount(24100).Build()
        });
        _target.AddRewardMetrics(_anyDateTimeOffset, input);

        var result = Query(@"
            select time, account_id, amount
            from metrics_rewards
            order by account_id").ToArray();
        
        Assert.Equal(2, result.Length);
        Assert.Equal(10, result[0].account_id);
        Assert.Equal(1000, result[0].amount);
        Assert.Equal(421, result[1].account_id);
        Assert.Equal(24100, result[1].amount);
    }

    [Fact]
    public void AddPoolRewardMetrics_BakerPool()
    {
        var block = new BlockBuilder()
            .WithId(138)
            .WithBlockSlotTime(_anyDateTimeOffset)
            .Build();

        var specialEvents = new SpecialEvent[]
        {
            new PaydayPoolRewardSpecialEvent { Pool = new BakerPoolRewardTarget(42), BakerReward = 100, TransactionFees = 35, FinalizationReward = 80 }
        };

        var rewardsSummary = new RewardsSummary(new []
        {
            new AccountRewardSummaryBuilder()
                .WithAccountId(42)
                .WithTotalAmountByType(
                    new RewardTypeAmount(RewardType.BakerReward, 98),
                    new RewardTypeAmount(RewardType.TransactionFeeReward, 30),
                    new RewardTypeAmount(RewardType.FinalizationReward, 70))
                .Build()
        });
        
        _target.AddPoolRewardMetrics(block, specialEvents, rewardsSummary);

        var result = Query(@"
                select time, pool_id, total_amount, baker_amount, delegator_amount, reward_type, block_id
                from metrics_pool_rewards
                order by pool_id, total_amount"
            )
            .Select(row => new
            {
                PoolId = (long)row.pool_id,
                Time = (DateTimeOffset)DateTime.SpecifyKind(row.time, DateTimeKind.Utc),
                TotalAmount = (long)row.total_amount,
                BakerAmount = (long)row.baker_amount,
                DelegatorAmount = (long)row.delegator_amount,
                RewardType = (int)row.reward_type,
                BlockId = (long)row.block_id
                
            })
            .ToArray();

        var expected = new[]
            {
                new { PoolId = 42L, Time = _anyDateTimeOffset, TotalAmount = 35L, BakerAmount = 30L, DelegatorAmount = 5L, RewardType = (int)RewardType.TransactionFeeReward, BlockId = 138L },
                new { PoolId = 42L, Time = _anyDateTimeOffset, TotalAmount = 80L, BakerAmount = 70L, DelegatorAmount = 10L, RewardType = (int)RewardType.FinalizationReward, BlockId = 138L },
                new { PoolId = 42L, Time = _anyDateTimeOffset, TotalAmount = 100L, BakerAmount = 98L, DelegatorAmount = 2L, RewardType = (int)RewardType.BakerReward, BlockId = 138L },
            }
            .ToArray();

        result.Should().Equal(expected);
    }

    [Fact]
    public void AddPoolRewardMetrics_PassiveDelegation()
    {
        var block = new BlockBuilder()
            .WithId(138)
            .WithBlockSlotTime(_anyDateTimeOffset)
            .Build();

        var specialEvents = new SpecialEvent[]
        {
            new PaydayPoolRewardSpecialEvent { Pool = new PassiveDelegationPoolRewardTarget(), BakerReward = 100, TransactionFees = 35, FinalizationReward = 80 }
        };

        var rewardsSummary = new RewardsSummary(Array.Empty<AccountRewardSummary>());
        
        _target.AddPoolRewardMetrics(block, specialEvents, rewardsSummary);

        var result = Query(@"
                select time, pool_id, total_amount, baker_amount, delegator_amount, reward_type, block_id
                from metrics_pool_rewards
                order by pool_id, total_amount"
            )
            .Select(row => new
            {
                PoolId = (long)row.pool_id,
                Time = (DateTimeOffset)DateTime.SpecifyKind(row.time, DateTimeKind.Utc),
                TotalAmount = (long)row.total_amount,
                BakerAmount = (long)row.baker_amount,
                DelegatorAmount = (long)row.delegator_amount,
                RewardType = (int)row.reward_type,
                BlockId = (long)row.block_id
                
            })
            .ToArray();

        var expected = new[]
            {
                new { PoolId = -1L, Time = _anyDateTimeOffset, TotalAmount = 35L, BakerAmount = 0L, DelegatorAmount = 35L, RewardType = (int)RewardType.TransactionFeeReward, BlockId = 138L },
                new { PoolId = -1L, Time = _anyDateTimeOffset, TotalAmount = 80L, BakerAmount = 0L, DelegatorAmount = 80L, RewardType = (int)RewardType.FinalizationReward, BlockId = 138L },
                new { PoolId = -1L, Time = _anyDateTimeOffset, TotalAmount = 100L, BakerAmount = 0L, DelegatorAmount = 100L, RewardType = (int)RewardType.BakerReward, BlockId = 138L },
            }
            .ToArray();

        result.Should().Equal(expected);
    }

    [Fact]
    public async Task AddPaydayPoolRewardMetrics_BakerPool()
    {
        var block = new BlockBuilder()
            .WithId(138)
            .WithBlockSlotTime(_anyDateTimeOffset)
            .Build();

        var specialEvents = new SpecialEvent[]
        {
            new PaydayPoolRewardSpecialEvent { Pool = new BakerPoolRewardTarget(42), BakerReward = 100, TransactionFees = 35, FinalizationReward = 80 }
        };

        var rewardsSummary = new RewardsSummary(new []
        {
            new AccountRewardSummaryBuilder()
                .WithAccountId(42)
                .WithTotalAmountByType(
                    new RewardTypeAmount(RewardType.BakerReward, 98),
                    new RewardTypeAmount(RewardType.TransactionFeeReward, 30),
                    new RewardTypeAmount(RewardType.FinalizationReward, 70))
                .Build()
        });
        
        _target.AddPaydayPoolRewardMetrics(block, specialEvents, rewardsSummary);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydayPoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();

        result!.Pool.Should().BeOfType<BakerPoolRewardTarget>().Which.BakerId.Should().Be(42);
        result.Timestamp.Should().Be(_anyDateTimeOffset);
        result.TransactionFeesTotalAmount.Should().Be(35);
        result.TransactionFeesBakerAmount.Should().Be(30);
        result.TransactionFeesDelegatorsAmount.Should().Be(5);
        result.BakerRewardTotalAmount.Should().Be(100);
        result.BakerRewardBakerAmount.Should().Be(98);
        result.BakerRewardDelegatorsAmount.Should().Be(2);
        result.FinalizationRewardTotalAmount.Should().Be(80);
        result.FinalizationRewardBakerAmount.Should().Be(70);
        result.FinalizationRewardDelegatorsAmount.Should().Be(10);
        result.SumTotalAmount.Should().Be(35 + 100 + 80);
        result.SumBakerAmount.Should().Be(30 + 98 + 70);
        result.SumDelegatorsAmount.Should().Be(5 + 2 + 10);
        result.BlockId.Should().Be(138);
    }

    [Fact]
    public async Task AddPaydayPoolRewardMetrics_PassiveDelegation()
    {
        var block = new BlockBuilder()
            .WithId(138)
            .WithBlockSlotTime(_anyDateTimeOffset)
            .Build();

        var specialEvents = new SpecialEvent[]
        {
            new PaydayPoolRewardSpecialEvent { Pool = new PassiveDelegationPoolRewardTarget(), BakerReward = 100, TransactionFees = 35, FinalizationReward = 80 }
        };

        var rewardsSummary = new RewardsSummary(Array.Empty<AccountRewardSummary>());
        
        _target.AddPaydayPoolRewardMetrics(block, specialEvents, rewardsSummary);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydayPoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();

        result!.Pool.Should().BeOfType<PassiveDelegationPoolRewardTarget>();
    }

    private IEnumerable<dynamic> Query(string sql)
    {
        using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
        conn.Open();

        var result = conn.Query(sql);
        conn.Close();
        return result;
    }

    private dynamic QuerySingle(string sql)
    {
        using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
        conn.Open();

        var result = conn.QuerySingle(sql);
        conn.Close();
        return result;
    }
}