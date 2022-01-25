using System.Collections.Generic;
using System.Threading;
using Application.Api.GraphQL.Pagination;
using FluentAssertions;
using HotChocolate.Types.Pagination;

namespace Tests.Api.GraphQL.Pagination;

public class CursorPagingAlgorithmBaseTest
{
    [Theory]
    // first queries
    [InlineData(1, null, null, null, new [] { 11 }, false, true)]
    [InlineData(2, null, null, null, new [] { 11, 12 }, false, true)]
    [InlineData(4, null, null, null, new [] { 11, 12, 13, 14 }, false, true)]
    [InlineData(5, null, null, null, new [] { 11, 12, 13, 14, 15 }, false, false)]
    [InlineData(6, null, null, null, new [] { 11, 12, 13, 14, 15 }, false, false)]
    // first-after queries
    [InlineData(1, null, "11", null, new [] { 12 }, true, true)]
    [InlineData(2, null, "11", null, new [] { 12, 13 }, true, true)]
    [InlineData(2, null, "12", null, new [] { 13, 14 }, true, true)]
    [InlineData(2, null, "13", null, new [] { 14, 15 }, true, false)]
    [InlineData(2, null, "14", null, new [] { 15 }, true, false)]
    // last queries
    [InlineData(null, 1, null, null, new [] { 15 }, true, false)]
    [InlineData(null, 2, null, null, new [] { 14, 15 }, true, false)]
    [InlineData(null, 4, null, null, new [] { 12, 13, 14, 15 }, true, false)]
    [InlineData(null, 5, null, null, new [] { 11, 12, 13, 14, 15 }, false, false)]
    [InlineData(null, 6, null, null, new [] { 11, 12, 13, 14, 15 }, false, false)]
    // last-before queries
    [InlineData(null, 1, null, "15", new [] { 14 }, true, true)]
    [InlineData(null, 2, null, "15", new [] { 13, 14 }, true, true)]
    [InlineData(null, 2, null, "14", new [] { 12, 13 }, true, true)]
    [InlineData(null, 2, null, "13", new [] { 11, 12 }, false, true)]
    [InlineData(null, 2, null, "12", new [] { 11 }, false, true)]
    public async Task ApplyPaginationOnDataOrderedAscending(int? first, int? last, string? after, string? before, int[] expectedNodes, bool expectedHasPrevPage, bool expectedHasNextPage)
    {
        var blocks = new List<int> { 11, 12, 13, 14, 15 };
        var query = blocks.AsQueryable();
        
        var arguments = new CursorPagingArguments(first, last, after, before);
        
        var target = new AscendingDataTarget();
        var result = await target.ApplyPaginationAsync(query, arguments, CancellationToken.None);
        
        result.Edges.Select(x => x.Node).Should().Equal(expectedNodes);
        result.Info.TotalCount.Should().BeNull();
        result.Info.HasPreviousPage.Should().Be(expectedHasPrevPage);
        result.Info.HasNextPage.Should().Be(expectedHasNextPage);
        result.Info.StartCursor.Should().Be(expectedNodes.First().ToString());
        result.Info.EndCursor.Should().Be(expectedNodes.Last().ToString());
    }
    
    [Theory]
    // first queries
    [InlineData(1, null, null, null, new [] { 15 }, false, true)]
    [InlineData(2, null, null, null, new [] { 15, 14 }, false, true)]
    [InlineData(4, null, null, null, new [] { 15, 14, 13, 12 }, false, true)]
    [InlineData(5, null, null, null, new [] { 15, 14, 13, 12, 11 }, false, false)]
    [InlineData(6, null, null, null, new [] { 15, 14, 13, 12, 11 }, false, false)]
    // first-after queries
    [InlineData(1, null, "15", null, new [] { 14 }, true, true)]
    [InlineData(2, null, "15", null, new [] { 14, 13 }, true, true)]
    [InlineData(2, null, "14", null, new [] { 13, 12 }, true, true)]
    [InlineData(2, null, "13", null, new [] { 12, 11 }, true, false)]
    [InlineData(2, null, "12", null, new [] { 11 }, true, false)]
    // last queries
    [InlineData(null, 1, null, null, new [] { 11 }, true, false)]
    [InlineData(null, 2, null, null, new [] { 12, 11 }, true, false)]
    [InlineData(null, 4, null, null, new [] { 14, 13, 12, 11 }, true, false)]
    [InlineData(null, 5, null, null, new [] { 15, 14, 13, 12, 11 }, false, false)]
    [InlineData(null, 6, null, null, new [] { 15, 14, 13, 12, 11 }, false, false)]
    // last-before queries
    [InlineData(null, 1, null, "11", new [] { 12 }, true, true)]
    [InlineData(null, 2, null, "11", new [] { 13, 12 }, true, true)]
    [InlineData(null, 2, null, "12", new [] { 14, 13 }, true, true)]
    [InlineData(null, 2, null, "13", new [] { 15, 14 }, false, true)]
    [InlineData(null, 2, null, "14", new [] { 15 }, false, true)]
    public async Task ApplyPaginationOnDataOrderedDescending(int? first, int? last, string? after, string? before, int[] expectedNodes, bool expectedHasPrevPage, bool expectedHasNextPage)
    {
        var blocks = new List<int> { 15, 14, 13, 12, 11 };
        var query = blocks.AsQueryable();
        
        var arguments = new CursorPagingArguments(first, last, after, before);
        
        var target = new DescendingDataTarget();
        var result = await target.ApplyPaginationAsync(query, arguments, CancellationToken.None);
        
        result.Edges.Select(x => x.Node).Should().Equal(expectedNodes);
        result.Info.TotalCount.Should().BeNull();
        result.Info.HasPreviousPage.Should().Be(expectedHasPrevPage);
        result.Info.HasNextPage.Should().Be(expectedHasNextPage);
        result.Info.StartCursor.Should().Be(expectedNodes.First().ToString());
        result.Info.EndCursor.Should().Be(expectedNodes.Last().ToString());
    }
    
    private class AscendingDataTarget : CursorPagingAlgorithmBase<int>
    {
        protected override IQueryable<int> ApplyAfterFilter(IQueryable<int> query, string serializedCursor)
        {
            var value = int.Parse(serializedCursor);
            return query.Where(x => x > value);
        }

        protected override IQueryable<int> ApplyBeforeFilter(IQueryable<int> query, string serializedCursor)
        {
            var value = int.Parse(serializedCursor);
            return query.Where(x => x < value);
        }

        protected override string GetSerializedCursor(int entity)
        {
            return entity.ToString();
        }
    }
    
    private class DescendingDataTarget : CursorPagingAlgorithmBase<int>
    {
        protected override IQueryable<int> ApplyAfterFilter(IQueryable<int> query, string serializedCursor)
        {
            var value = int.Parse(serializedCursor);
            return query.Where(x => x < value);
        }

        protected override IQueryable<int> ApplyBeforeFilter(IQueryable<int> query, string serializedCursor)
        {
            var value = int.Parse(serializedCursor);
            return query.Where(x => x > value);
        }

        protected override string GetSerializedCursor(int entity)
        {
            return entity.ToString();
        }
    }
}