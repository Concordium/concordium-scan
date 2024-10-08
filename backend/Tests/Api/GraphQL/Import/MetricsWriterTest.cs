﻿using System.Collections.Generic;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Payday;
using Application.Database;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class MetricsWriterTest
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
        
        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_blocks");
        connection.Execute("TRUNCATE TABLE metrics_bakers");
        connection.Execute("TRUNCATE TABLE metrics_rewards");
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

        var paydaySummary = new PaydaySummary
        {
            PaydayDurationSeconds = 2 * 60 * 60
        };

        var paydayPoolStakeSnapshot = new PaydayPoolStakeSnapshot(
            new []{ new PaydayPoolStakeSnapshotItem(42, 9000000, 1000000)});

        var paydayPassiveDelegationStakeSnapshot = new PaydayPassiveDelegationStakeSnapshot(200000);

        var importState = new ImportState()
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9",
            LatestWrittenChainParameters = new ChainParametersV2(){
                RewardPeriodLength = 2
            },
            EpochDuration = 1000 * 60 * 60 // milliseconds
        };

        _target.AddPaydayPoolRewardMetrics(block, specialEvents, rewardsSummary, paydaySummary, paydayPoolStakeSnapshot, paydayPassiveDelegationStakeSnapshot, importState);

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
        result.PaydayDurationSeconds.Should().Be(2 * 60 * 60);
        result.TotalApy.Should().BeApproximately(0.09, 0.01);
        result.BakerApy.Should().BeApproximately(0.1, 0.01);
        result.DelegatorsApy.Should().BeApproximately(0.07, 0.01);
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
        
        var paydaySummary = new PaydaySummary
        {
            PaydayDurationSeconds = 2 * 60 * 60
        };

        var paydayPoolStakeSnapshot = new PaydayPoolStakeSnapshot(
            new []{ new PaydayPoolStakeSnapshotItem(42, 9000000, 1000000)});

        var paydayPassiveDelegationStakeSnapshot = new PaydayPassiveDelegationStakeSnapshot(20000000);

        var importState = new ImportState()
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9",
            LatestWrittenChainParameters = new ChainParametersV2(){
                RewardPeriodLength = 2
            },
            EpochDuration = 1000 * 60 * 60 // milliseconds
        };
        _target.AddPaydayPoolRewardMetrics(block, specialEvents, rewardsSummary, paydaySummary, paydayPoolStakeSnapshot, paydayPassiveDelegationStakeSnapshot, importState);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydayPoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();

        result!.Pool.Should().BeOfType<PassiveDelegationPoolRewardTarget>();
        result.TotalApy.Should().BeApproximately(0.04, 0.01);
        result.BakerApy.Should().BeNull();
        result.DelegatorsApy.Should().BeApproximately(0.04, 0.01);

    }

    [Fact]
    public void CalculateApy_HasStake()
    {
        // Example from Excel sheet reviewed by Christian Matts from Concordium
        var reward = 13148991050L;
        var stake = 795760615434465L;
        var durationSeconds = 2 * 60 * 60;

        var result = MetricsWriter.CalculateApy(reward, stake, durationSeconds);
        result.Should().BeApproximately(0.075056970, 0.000000001);
    }

    [Fact]
    public void CalculateApy_HasNoStake()
    {
        // Example from Excel sheet reviewed by Christian Matts from Concordium
        var reward = 13148991050L;
        var stake = 0L;
        var durationSeconds = 2 * 60 * 60;

        var result = MetricsWriter.CalculateApy(reward, stake, durationSeconds);
        result.Should().BeNull();
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
