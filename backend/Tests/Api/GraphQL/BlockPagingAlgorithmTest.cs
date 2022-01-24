using System.Collections.Generic;
using System.Threading;
using Application.Api.GraphQL;
using FluentAssertions;
using HotChocolate.Types.Pagination;

namespace Tests.Api.GraphQL;

public class BlockPagingAlgorithmTest
{
    private readonly BlockPagingAlgorithm _target;

    public BlockPagingAlgorithmTest()
    {
        _target = new BlockPagingAlgorithm();
    }

    [Theory]
    // first queries
    [InlineData(1, null, null, null, new long[] { 15 }, false, true)]
    [InlineData(2, null, null, null, new long[] { 15, 14 }, false, true)]
    [InlineData(4, null, null, null, new long[] { 15, 14, 13, 12 }, false, true)]
    [InlineData(5, null, null, null, new long[] { 15, 14, 13, 12, 11 }, false, false)]
    // first-after queries
    [InlineData(1, null, "15", null, new long[] { 14 }, true, true)]
    [InlineData(2, null, "15", null, new long[] { 14, 13 }, true, true)]
    [InlineData(2, null, "14", null, new long[] { 13, 12 }, true, true)]
    [InlineData(2, null, "13", null, new long[] { 12, 11 }, true, false)]
    [InlineData(2, null, "12", null, new long[] { 11 }, true, false)]
    // last queries
    [InlineData(null, 1, null, null, new long[] { 11 }, true, false)]
    [InlineData(null, 2, null, null, new long[] { 12, 11 }, true, false)]
    [InlineData(null, 4, null, null, new long[] { 14, 13, 12, 11 }, true, false)]
    [InlineData(null, 5, null, null, new long[] { 15, 14, 13, 12, 11 }, false, false)]
    // last-before queries
    [InlineData(null, 1, null, "11", new long[] { 12 }, true, true)]
    [InlineData(null, 2, null, "11", new long[] { 13, 12 }, true, true)]
    [InlineData(null, 2, null, "12", new long[] { 14, 13 }, true, true)]
    [InlineData(null, 2, null, "13", new long[] { 15, 14 }, false, true)]
    [InlineData(null, 2, null, "14", new long[] { 15 }, false, true)]
    public async Task FactMethodName(int? first, int? last, string? after, string? before, long[] expectedNodeIds, bool expectedHasPrevPage, bool expectedHasNextPage)
    {
        var blocks = new List<Block>
        {
            new BlockBuilder().WithId(15).WithBlockHeight(105).Build(),
            new BlockBuilder().WithId(14).WithBlockHeight(104).Build(),
            new BlockBuilder().WithId(13).WithBlockHeight(103).Build(),
            new BlockBuilder().WithId(12).WithBlockHeight(102).Build(),
            new BlockBuilder().WithId(11).WithBlockHeight(101).Build(),
        };
        
        var query = blocks.AsQueryable();
        var arguments = new CursorPagingArguments(first, last, after, before);
        
        var result = await _target.ApplyPaginationAsync(query, arguments, CancellationToken.None);
        
        result.Edges.Select(edge => edge.Node.Id).Should().Equal(expectedNodeIds);
        result.Info.TotalCount.Should().BeNull();
        result.Info.HasPreviousPage.Should().Be(expectedHasPrevPage);
        result.Info.HasNextPage.Should().Be(expectedHasNextPage);
        result.Info.StartCursor.Should().Be(expectedNodeIds.First().ToString());
        result.Info.EndCursor.Should().Be(expectedNodeIds.Last().ToString());
    }
}