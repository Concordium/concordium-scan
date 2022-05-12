using Application.Import.ConcordiumNode;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests;

[Collection("Postgres Collection")]
public class NodeCacheTest : IClassFixture<DatabaseFixture>
{
    private readonly NodeCache _target;

    public NodeCacheTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _target = new NodeCache(dbFixture.DatabaseSettings, new NullMetrics());
        
        using var connection = dbFixture.GetOpenNodeCacheConnection();
        connection.Execute("TRUNCATE TABLE block_summary");
    }

    [Fact]
    public void WriteAndRead()
    {
        _target.WriteBlockSummary("foo", "hello world");
        var result = _target.ReadBlockSummary("foo");
        result.Should().Be("hello world");
    }
}