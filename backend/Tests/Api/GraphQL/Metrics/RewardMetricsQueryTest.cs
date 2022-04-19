using Application.Api.GraphQL.Metrics;
using Application.Database;
using Dapper;
using FluentAssertions;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Metrics;

[Collection("Postgres Collection")]
public class RewardMetricsQueryTest : IClassFixture<DatabaseFixture>
{
    private readonly RewardMetricsQuery _target;
    private readonly DatabaseSettings _dbSettings;
    private readonly TimeProviderStub _timeProviderStub;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public RewardMetricsQueryTest(DatabaseFixture dbFixture)
    {
        _dbSettings = dbFixture.DatabaseSettings;
        _timeProviderStub = new TimeProviderStub();
        _target = new RewardMetricsQuery(dbFixture.DatabaseSettings, _timeProviderStub);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_rewards");
    }

    [Fact]
    public async Task GetBakerMetrics_NewMetricsWithinPeriod()
    {
        await InsertRewardMetrics(
            new { Time = _anyDateTimeOffset.AddDays(-20), AccountId = 42, Amount = 3000 },
            new { Time = _anyDateTimeOffset.AddDays(-20), AccountId = 10, Amount = 2000 },
            new { Time = _anyDateTimeOffset.AddDays(-15), AccountId = 42, Amount = 1000 },
            new { Time = _anyDateTimeOffset.AddDays(-10), AccountId = 13, Amount = 5000 }
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetRewardMetrics(MetricsPeriod.Last30Days);
        result.SumRewardAmount.Should().Be(11000);
        result.Buckets.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetBakerMetrics_NoMetricsWithinPeriod()
    {
        await InsertRewardMetrics(
            new { Time = _anyDateTimeOffset.AddDays(-50), AccountId = 42, Amount = 3000 },
            new { Time = _anyDateTimeOffset.AddDays(-45), AccountId = 10, Amount = 2000 },
            new { Time = _anyDateTimeOffset.AddDays(-40), AccountId = 42, Amount = 1000 }
        );
    
        _timeProviderStub.UtcNow = _anyDateTimeOffset;
    
        var result = await _target.GetRewardMetrics(MetricsPeriod.Last30Days);
        result.SumRewardAmount.Should().Be(0);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_SumRewards.Should().AllSatisfy(x => x.Should().Be(0));
    }

    [Fact]
    public async Task GetBakerMetricsForBaker_NewMetricsWithinPeriod()
    {
        await InsertRewardMetrics(
            new { Time = _anyDateTimeOffset.AddDays(-20), AccountId = 42, Amount = 3000 },
            new { Time = _anyDateTimeOffset.AddDays(-20), AccountId = 10, Amount = 2000 },
            new { Time = _anyDateTimeOffset.AddDays(-15), AccountId = 42, Amount = 1000 },
            new { Time = _anyDateTimeOffset.AddDays(-10), AccountId = 13, Amount = 5000 }
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetRewardMetricsForBaker(42, MetricsPeriod.Last30Days);
        result.SumRewardAmount.Should().Be(4000);
        result.Buckets.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GetBakerMetricsForBaker_NoMetricsWithinPeriod()
    {
        await InsertRewardMetrics(
            new { Time = _anyDateTimeOffset.AddDays(-50), AccountId = 42, Amount = 3000 },
            new { Time = _anyDateTimeOffset.AddDays(-45), AccountId = 10, Amount = 2000 },
            new { Time = _anyDateTimeOffset.AddDays(-40), AccountId = 42, Amount = 1000 }
        );
    
        _timeProviderStub.UtcNow = _anyDateTimeOffset;
    
        var result = await _target.GetRewardMetricsForBaker(42, MetricsPeriod.Last30Days);
        result.SumRewardAmount.Should().Be(0);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_SumRewards.Should().AllSatisfy(x => x.Should().Be(0));
    }

    private async Task InsertRewardMetrics(params dynamic[] param)
    {
        var sql = @"
                insert into metrics_rewards (time, account_id, amount) 
                values (@Time, @AccountId, @Amount)";

        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, param);
        await conn.CloseAsync();
    }
}
