using Application.Api.GraphQL.Import;
using Application.Database;
using Dapper;
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

    public MetricsWriterTest(DatabaseFixture dbFixture)
    {
        _databaseSettings = dbFixture.DatabaseSettings;
        _target = new MetricsWriter(_databaseSettings, new NullMetrics());
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_blocks");
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
                .WithTotalAmountEncrypted(1000)
                .WithTotalAmountStaked(2500)
                .Build())
            .Build());
        
        await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
        conn.Open();

        var result = await conn.QuerySingleAsync(@"
            select time, block_height, block_time_secs, finalization_time_secs, total_microccd, total_microccd_encrypted, total_microccd_staked
            from metrics_blocks");

        Assert.Equal(42, result.block_height);
        Assert.Equal(10.1d, result.block_time_secs);
        Assert.Null(result.finalization_time_secs);
        Assert.Equal(10000, result.total_microccd);
        Assert.Equal(1000, result.total_microccd_encrypted);
        Assert.Equal(2500, result.total_microccd_staked);
    }
}