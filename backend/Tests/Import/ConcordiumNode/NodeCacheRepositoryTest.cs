using Application.Import.ConcordiumNode;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Import.ConcordiumNode;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class NodeCacheRepositoryTest
{
    private readonly NodeCacheRepository _target;

    public NodeCacheRepositoryTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _target = new NodeCacheRepository(dbFixture.DatabaseSettings, new NullMetrics());
        
        using var connection = DatabaseFixture.GetOpenNodeCacheConnection();
        connection.Execute("TRUNCATE TABLE block_summary");
    }

    [Fact]
    public void Read_EmptyTable()
    {
        var result = _target.ReadBlockSummary("foo");
        result.Should().BeNull();
    }
    
    [Theory]
    [InlineData("foo", "{\"value\": 42}")]
    [InlineData("bar", "{\"value\": 1337}")]
    [InlineData("xyz", null)]
    public void WriteAndRead(string query, string? expectedResult)
    {
        _target.WriteBlockSummary("foo", "{\"value\": 42}");
        _target.WriteBlockSummary("bar", "{\"value\": 1337}");
        
        var result = _target.ReadBlockSummary(query);
        result.Should().Be(expectedResult);
    }
}