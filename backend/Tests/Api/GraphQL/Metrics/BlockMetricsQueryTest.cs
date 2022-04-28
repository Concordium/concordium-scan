using Application.Api.GraphQL.Metrics;
using Application.Database;
using Dapper;
using FluentAssertions;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Metrics;

[Collection("Postgres Collection")]
public class BlockMetricsQueryTest : IClassFixture<DatabaseFixture>
{
    private readonly BlockMetricsQuery _target;
    private readonly DatabaseSettings _dbSettings;
    private readonly TimeProviderStub _timeProviderStub;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public BlockMetricsQueryTest(DatabaseFixture dbFixture)
    {
        _dbSettings = dbFixture.DatabaseSettings;
        _timeProviderStub = new TimeProviderStub();
        _target = new BlockMetricsQuery(dbFixture.DatabaseSettings, _timeProviderStub);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_blocks");
    }
    
    [Fact]
    public async Task GetBakerMetrics_NewMetricsWithinPeriod()
    {
        await InsertBlockMetrics(
            CreateRow(_anyDateTimeOffset.AddDays(-10))
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBlockMetrics(MetricsPeriod.Last30Days);
        result.LastTotalMicroCcd.Should().Be(100000L);
        result.Buckets.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData(70_000L)]
    [InlineData(null)]
    public async Task GetBakerMetrics_LastTotalMicroCcdReleased_NewMetricsWithinPeriod(long? lastValue)
    {
        await InsertBlockMetrics(
            CreateRow(_anyDateTimeOffset.AddDays(-20), totalMicroCcdReleased: 50_000L),
            CreateRow(_anyDateTimeOffset.AddDays(-10), totalMicroCcdReleased: 60_000L),
            CreateRow(_anyDateTimeOffset.AddDays(-5), totalMicroCcdReleased: lastValue)
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBlockMetrics(MetricsPeriod.Last30Days);
        result.LastTotalMicroCcdReleased.Should().Be(lastValue);
        result.Buckets.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0.7d)]
    [InlineData(null)]
    public async Task GetBakerMetrics_LastTotalPercentageReleased_NewMetricsWithinPeriod(double? lastValue)
    {
        await InsertBlockMetrics(
            CreateRow(_anyDateTimeOffset.AddDays(-20), lastPercentageReleased: 0.5d),
            CreateRow(_anyDateTimeOffset.AddDays(-10), lastPercentageReleased: 0.6d),
            CreateRow(_anyDateTimeOffset.AddDays(-5), lastPercentageReleased: lastValue)
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBlockMetrics(MetricsPeriod.Last30Days);
        result.LastTotalPercentageReleased.Should().Be(lastValue);
        result.Buckets.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBakerMetrics_LastTotalPercentageEncrypted_NewMetricsWithinPeriod()
    {
        await InsertBlockMetrics(
            CreateRow(_anyDateTimeOffset.AddDays(-20), lastPercentageEncrypted: 0.5d),
            CreateRow(_anyDateTimeOffset.AddDays(-10), lastPercentageEncrypted: 0.6d),
            CreateRow(_anyDateTimeOffset.AddDays(-5), lastPercentageEncrypted: 0.7d)
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBlockMetrics(MetricsPeriod.Last30Days);
        result.LastTotalPercentageEncrypted.Should().Be(0.7d);
        result.Buckets.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBakerMetrics_LastTotalPercentageStaked_NewMetricsWithinPeriod()
    {
        await InsertBlockMetrics(
            CreateRow(_anyDateTimeOffset.AddDays(-20), lastPercentageStaked: 0.5d),
            CreateRow(_anyDateTimeOffset.AddDays(-10), lastPercentageStaked: 0.6d),
            CreateRow(_anyDateTimeOffset.AddDays(-5), lastPercentageStaked: 0.7d)
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBlockMetrics(MetricsPeriod.Last30Days);
        result.LastTotalPercentageStaked.Should().Be(0.7d);
        result.Buckets.Should().NotBeNull();
    }

    private object CreateRow(DateTimeOffset time, long? totalMicroCcdReleased = 90_000L, double? lastPercentageReleased = 0.9d, double lastPercentageEncrypted = 0.1d, double lastPercentageStaked = 0.5d)
    {
        return new
        {
            Time = time,
            BlockHeight = 10,
            BlockTimeSecs = 10d,
            FinalizationTimeSecs = 11d,
            TotalMicroCcd = 100000L,
            TotalMicroCcdReleased = totalMicroCcdReleased,
            TotalMicroCcdEncrypted = 10000L,
            TotalMicroCcdStaked = 50000L,
            TotalPercentageReleased = lastPercentageReleased, 
            TotalPercentageEncrypted = lastPercentageEncrypted,
            TotalPercentageStaked = lastPercentageStaked
        };
    }

    private async Task InsertBlockMetrics(params dynamic[] param)
    {
        var sql = @"insert into metrics_blocks (time, block_height, block_time_secs, finalization_time_secs, total_microccd, total_microccd_released, total_microccd_encrypted, total_microccd_staked, total_percentage_released, total_percentage_encrypted, total_percentage_staked) 
                    values (@Time, @BlockHeight, @BlockTimeSecs, @FinalizationTimeSecs, @TotalMicroCcd, @TotalMicroCcdReleased, @TotalMicroCcdEncrypted, @TotalMicroCcdStaked, @TotalPercentageReleased, @TotalPercentageEncrypted, @TotalPercentageStaked)";

        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, param);
        await conn.CloseAsync();
    }
}