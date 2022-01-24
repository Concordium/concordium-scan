using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class BlockPagingAlgorithm
{
    public async ValueTask<Connection<Block>> ApplyPaginationAsync(
        IQueryable<Block> query,
        CursorPagingArguments arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.After != null)
            query = query.Where(x => x.Id < long.Parse(arguments.After));
        if (arguments.Before != null)
            query = query.Where(x => x.Id > long.Parse(arguments.Before));

        var strategy = Strategy.Create(arguments);
        return await strategy.ApplyPaginationAsync(query, arguments, cancellationToken);
    }

    private abstract class Strategy
    {
        public static Strategy Create(CursorPagingArguments arguments)
        {
            if (arguments.First != null && arguments.Last != null)
                throw new ArgumentException("Only one of paging parameters 'first' and 'last' is allowed.");
            if (arguments.First == null && arguments.Last == null)
                throw new ArgumentException("Must specify one of paging parameters 'first' or 'last'.");

            if (arguments.First.HasValue) return new FirstStrategy();
            return new LastStrategy();
        }

        public async ValueTask<Connection<Block>> ApplyPaginationAsync(IQueryable<Block> query,
            CursorPagingArguments arguments, CancellationToken cancellationToken)
        {
            var requestedPageSize = GetRequestedPageSize(arguments);
            
            query = PreProcess(query);
            query = query.Take(requestedPageSize + 1);
            
            Block[] retrieved;
            if (query is IAsyncEnumerable<Block>) retrieved = await query.ToArrayAsync(cancellationToken);
            else retrieved = query.ToArray();

            var extraRowRetrieved = retrieved.Length == requestedPageSize + 1;
            var requestedBlocks = extraRowRetrieved ? retrieved.SkipLast(1) : retrieved;
            requestedBlocks = PostProcess(requestedBlocks);
            
            var edges = new List<Edge<Block>>();
            foreach (var block in requestedBlocks)
                edges.Add(new Edge<Block>(block, block.Id.ToString()));

            var hasNextPage = GetHasNextPage(arguments, extraRowRetrieved);
            var hasPrevPage = GetHasPrevPage(arguments, extraRowRetrieved);
            var pageInfo = new ConnectionPageInfo(hasNextPage, hasPrevPage, edges[0].Cursor, edges[^1].Cursor);
            return new Connection<Block>(edges, pageInfo, ct => throw new NotImplementedException());
        }

        protected abstract int GetRequestedPageSize(CursorPagingArguments arguments);
        protected abstract IQueryable<Block> PreProcess(IQueryable<Block> query);
        protected abstract IEnumerable<Block> PostProcess(IEnumerable<Block> blocks);
        protected abstract bool GetHasNextPage(CursorPagingArguments arguments, bool extraRowRetrieved);
        protected abstract bool GetHasPrevPage(CursorPagingArguments arguments, bool extraRowRetrieved);
    }

    private class FirstStrategy : Strategy
    {
        protected override int GetRequestedPageSize(CursorPagingArguments arguments) => arguments.First!.Value;
        protected override IQueryable<Block> PreProcess(IQueryable<Block> query) => query;
        protected override IEnumerable<Block> PostProcess(IEnumerable<Block> blocks) => blocks;
        protected override bool GetHasNextPage(CursorPagingArguments arguments, bool extraRowRetrieved) => extraRowRetrieved;
        protected override bool GetHasPrevPage(CursorPagingArguments arguments, bool extraRowRetrieved) => arguments.After != null;
    }

    private class LastStrategy : Strategy
    {
        protected override int GetRequestedPageSize(CursorPagingArguments arguments) => arguments.Last!.Value;
        protected override IQueryable<Block> PreProcess(IQueryable<Block> query) => query.Reverse();
        protected override IEnumerable<Block> PostProcess(IEnumerable<Block> blocks) => blocks.Reverse();
        protected override bool GetHasNextPage(CursorPagingArguments arguments, bool extraRowRetrieved) => arguments.Before != null;
        protected override bool GetHasPrevPage(CursorPagingArguments arguments, bool extraRowRetrieved) => extraRowRetrieved;
    }
}