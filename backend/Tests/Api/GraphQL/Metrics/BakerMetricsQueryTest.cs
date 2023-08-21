using Application.Api.GraphQL.Metrics;
using Application.Database;
using Dapper;
using FluentAssertions;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Metrics;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class BakerMetricsQueryTest
{
    private readonly BakerMetricsQuery _target;
    private readonly DatabaseSettings _dbSettings;
    private readonly TimeProviderStub _timeProviderStub;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public BakerMetricsQueryTest(DatabaseFixture dbFixture)
    {
        _dbSettings = dbFixture.DatabaseSettings;
        _timeProviderStub = new TimeProviderStub();
        _target = new BakerMetricsQuery(dbFixture.DatabaseSettings, _timeProviderStub);

        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_bakers");
    }

    [Fact]
    public async Task GetBakerMetrics_NewMetricsWithinPeriod()
    {
        await InsertBakerMetrics(
            new { Time = _anyDateTimeOffset.AddDays(-20), TotalBakerCount = 10, BakersAdded = 2, BakersRemoved = 0 },
            new { Time = _anyDateTimeOffset.AddDays(-10), TotalBakerCount = 13, BakersAdded = 5, BakersRemoved = 2 },
            new { Time = _anyDateTimeOffset.AddDays(0), TotalBakerCount = 20, BakersAdded = 9, BakersRemoved = 2 }
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBakerMetrics(MetricsPeriod.Last30Days);
        result.LastBakerCount.Should().Be(20);
        result.BakersAdded.Should().Be(16);
        result.BakersRemoved.Should().Be(4);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_LastBakerCount.First().Should().Be(0);
        result.Buckets.Y_LastBakerCount.Last().Should().Be(20);
    }
    
    [Fact]
    public async Task GetBakerMetrics_NoMetricsWithinPeriod()
    {
        await InsertBakerMetrics(
            new { Time = _anyDateTimeOffset.AddDays(-50), TotalBakerCount = 10, BakersAdded = 2, BakersRemoved = 0 },
            new { Time = _anyDateTimeOffset.AddDays(-45), TotalBakerCount = 13, BakersAdded = 5, BakersRemoved = 2 },
            new { Time = _anyDateTimeOffset.AddDays(-40), TotalBakerCount = 20, BakersAdded = 9, BakersRemoved = 2 }
        );

        _timeProviderStub.UtcNow = _anyDateTimeOffset;

        var result = await _target.GetBakerMetrics(MetricsPeriod.Last30Days);
        result.LastBakerCount.Should().Be(20);
        result.BakersAdded.Should().Be(0);
        result.BakersRemoved.Should().Be(0);
        result.Buckets.Should().NotBeNull();
        result.Buckets.Y_LastBakerCount.Should().AllSatisfy(x => x.Should().Be(20));
        result.Buckets.Y_BakersAdded.Should().AllSatisfy(x => x.Should().Be(0));
        result.Buckets.Y_BakersRemoved.Should().AllSatisfy(x => x.Should().Be(0));
    }

    private async Task InsertBakerMetrics(params dynamic[] param)
    {
        var sql = @"
                insert into metrics_bakers (time, total_baker_count, bakers_added, bakers_removed) 
                values (@Time, @TotalBakerCount, @BakersAdded, @BakersRemoved)";

        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, param);
        await conn.CloseAsync();
    }
}