using Application.Import.ConcordiumNode;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Import.ConcordiumNode;

[Collection("Postgres Collection")]
public class NodeCacheRepositoryTest : IClassFixture<DatabaseFixture>
{
    private readonly NodeCacheRepository _target;

    public NodeCacheRepositoryTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _target = new NodeCacheRepository(dbFixture.DatabaseSettings, new NullMetrics());
        
        using var connection = dbFixture.GetOpenNodeCacheConnection();
        connection.Execute("TRUNCATE TABLE block_summary");
    }

    [Fact]
    public void Read_EmptyTable()
    {
        var result = _target.ReadBlockSummary("foo");
        result.Should().BeNull();
    }
    
    [Theory]
    [InlineData("foo", "hello world")]
    [InlineData("bar", "lorem ipsum")]
    [InlineData("xyz", null)]
    public void WriteAndRead(string query, string? expectedResult)
    {
        _target.WriteBlockSummary("foo", "hello world");
        _target.WriteBlockSummary("bar", "lorem ipsum");
        
        var result = _target.ReadBlockSummary(query);
        result.Should().Be(expectedResult);
    }
}