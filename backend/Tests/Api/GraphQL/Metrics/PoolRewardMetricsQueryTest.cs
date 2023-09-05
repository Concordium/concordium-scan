using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Metrics;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Metrics;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class PoolRewardMetricsQueryTest
{
    private readonly PoolRewardMetricsQuery _target;
    private readonly TimeProviderStub _timeProviderStub;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public PoolRewardMetricsQueryTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _timeProviderStub = new TimeProviderStub();
        _target = new PoolRewardMetricsQuery(dbFixture.DatabaseSettings, _timeProviderStub);

        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_payday_pool_rewards");
    }

    [Fact]
    public async Task GetPoolRewardMetricsForBakerPool_NewMetricsWithinPeriod()
    {
        await InsertPoolRewards(
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-20)).WithPool(new BakerPoolRewardTarget(42)).WithSumAmounts(3000, 2700, 300).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-20)).WithPool(new BakerPoolRewardTarget(10)).WithSumAmounts(2000, 1650, 350).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-15)).WithPool(new BakerPoolRewardTarget(42)).WithSumAmounts(1000, 900, 100).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-10)).WithPool(new PassiveDelegationPoolRewardTarget()).WithSumAmounts(3000, 2700, 300).Build()
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetPoolRewardMetricsForBakerPool(42, MetricsPeriod.Last30Days);
        result.SumTotalRewardAmount.Should().Be(4000);
        result.SumBakerRewardAmount.Should().Be(3600);
        result.SumDelegatorsRewardAmount.Should().Be(400);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_SumTotalRewards.Sum().Should().Be(4000);
        result.Buckets.Y_SumBakerRewards.Sum().Should().Be(3600);
        result.Buckets.Y_SumDelegatorsRewards.Sum().Should().Be(400);
    }
    
    [Fact]
    public async Task GetPoolRewardMetricsForBakerPool_NoMetricsWithinPeriod()
    {
        await InsertPoolRewards(
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-50)).WithPool(new BakerPoolRewardTarget(42)).WithSumAmounts(3000, 2700, 300).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-45)).WithPool(new BakerPoolRewardTarget(10)).WithSumAmounts(2000, 1650, 350).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-40)).WithPool(new BakerPoolRewardTarget(42)).WithSumAmounts(1000, 900, 100).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-40)).WithPool(new PassiveDelegationPoolRewardTarget()).WithSumAmounts(3000, 2700, 300).Build()
        );
    
        _timeProviderStub.UtcNow = _anyDateTimeOffset;
    
        var result = await _target.GetPoolRewardMetricsForBakerPool(42, MetricsPeriod.Last30Days);
        result.SumTotalRewardAmount.Should().Be(0);
        result.SumBakerRewardAmount.Should().Be(0);
        result.SumDelegatorsRewardAmount.Should().Be(0);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_SumTotalRewards.Should().AllSatisfy(x => x.Should().Be(0));
    }

    [Fact]
    public async Task GetPoolRewardMetricsForPassiveDelegation_NewMetricsWithinPeriod()
    {
        await InsertPoolRewards(
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-20)).WithPool(new BakerPoolRewardTarget(42)).WithSumAmounts(3000, 2700, 300).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-20)).WithPool(new BakerPoolRewardTarget(10)).WithSumAmounts(2000, 1650, 350).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-15)).WithPool(new BakerPoolRewardTarget(42)).WithSumAmounts(1000, 900, 100).Build(),
            new PaydayPoolRewardBuilder().WithTimestamp(_anyDateTimeOffset.AddDays(-10)).WithPool(new PassiveDelegationPoolRewardTarget()).WithSumAmounts(3000, 2700, 300).Build()
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetPoolRewardMetricsForPassiveDelegation(MetricsPeriod.Last30Days);
        result.SumTotalRewardAmount.Should().Be(3000);
        result.SumBakerRewardAmount.Should().Be(2700);
        result.SumDelegatorsRewardAmount.Should().Be(300);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_SumTotalRewards.Sum().Should().Be(3000);
        result.Buckets.Y_SumBakerRewards.Sum().Should().Be(2700);
        result.Buckets.Y_SumDelegatorsRewards.Sum().Should().Be(300);
    }


    private async Task InsertPoolRewards(params PaydayPoolReward[] param)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PaydayPoolRewards.AddRange(param);
        await dbContext.SaveChangesAsync();
    }
}