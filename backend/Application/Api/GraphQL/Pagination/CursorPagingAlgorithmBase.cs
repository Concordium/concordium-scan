using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Pagination;

public abstract class CursorPagingAlgorithmBase<T>
{
    protected abstract IQueryable<T> ApplyAfterFilter(IQueryable<T> query, string serializedCursor);
    protected abstract IQueryable<T> ApplyBeforeFilter(IQueryable<T> query, string serializedCursor);
    protected abstract string GetSerializedCursor(T entity);
    
    public async ValueTask<Connection<T>> ApplyPaginationAsync(IQueryable<T> query, CursorPagingArguments arguments, CancellationToken cancellationToken)
    {
        if (arguments.After != null)
            query = ApplyAfterFilter(query, arguments.After);
        if (arguments.Before != null)
            query = ApplyBeforeFilter(query, arguments.Before);

        var strategy = PagingStrategy.Create(arguments);
        return await strategy.ApplyPaginationAsync(query, arguments, GetSerializedCursor, cancellationToken);
    }

    private abstract class PagingStrategy
    {
        public static PagingStrategy Create(CursorPagingArguments arguments)
        {
            if (arguments.First != null && arguments.Last != null)
                throw new ArgumentException("Only one of paging parameters 'first' and 'last' is allowed.");
            if (arguments.First == null && arguments.Last == null)
                throw new ArgumentException("Must specify one of paging parameters 'first' or 'last'.");

            if (arguments.First.HasValue) return new TakeFirstPagingStrategy();
            return new TakeLastPagingStrategy();
        }

        public async ValueTask<Connection<T>> ApplyPaginationAsync(IQueryable<T> query,
            CursorPagingArguments arguments, Func<T, string> getSerializedCursor,
            CancellationToken cancellationToken)
        {
            var requestedPageSize = GetRequestedPageSize(arguments);
            
            query = PreProcess(query);
            query = query.Take(requestedPageSize + 1);
            
            T[] retrieved;
            if (query is IAsyncEnumerable<T>) retrieved = await query.ToArrayAsync(cancellationToken);
            else retrieved = query.ToArray();

            var extraRowRetrieved = retrieved.Length == requestedPageSize + 1;
            var requestedEntities = extraRowRetrieved ? retrieved.SkipLast(1) : retrieved;
            requestedEntities = PostProcess(requestedEntities);
            
            var edges = new List<Edge<T>>();
            foreach (var entity in requestedEntities)
                edges.Add(new Edge<T>(entity, getSerializedCursor(entity)));

            var hasNextPage = GetHasNextPage(arguments, extraRowRetrieved);
            var hasPrevPage = GetHasPrevPage(arguments, extraRowRetrieved);
            var startCursor = edges.Count > 0 ? edges[0].Cursor : null;
            var endCursor = edges.Count > 0 ? edges[^1].Cursor : null;
            var pageInfo = new ConnectionPageInfo(hasNextPage, hasPrevPage, startCursor, endCursor);
            return new Connection<T>(edges, pageInfo, ct => throw new NotImplementedException());
        }

        protected abstract int GetRequestedPageSize(CursorPagingArguments arguments);
        protected abstract IQueryable<T> PreProcess(IQueryable<T> query);
        protected abstract IEnumerable<T> PostProcess(IEnumerable<T> entities);
        protected abstract bool GetHasNextPage(CursorPagingArguments arguments, bool extraRowRetrieved);
        protected abstract bool GetHasPrevPage(CursorPagingArguments arguments, bool extraRowRetrieved);
    }

    private class TakeFirstPagingStrategy : PagingStrategy
    {
        protected override int GetRequestedPageSize(CursorPagingArguments arguments) => arguments.First!.Value;
        protected override IQueryable<T> PreProcess(IQueryable<T> query) => query;
        protected override IEnumerable<T> PostProcess(IEnumerable<T> entities) => entities;
        protected override bool GetHasNextPage(CursorPagingArguments arguments, bool extraRowRetrieved) => extraRowRetrieved;
        protected override bool GetHasPrevPage(CursorPagingArguments arguments, bool extraRowRetrieved) => arguments.After != null;
    }

    private class TakeLastPagingStrategy : PagingStrategy
    {
        protected override int GetRequestedPageSize(CursorPagingArguments arguments) => arguments.Last!.Value;
        protected override IQueryable<T> PreProcess(IQueryable<T> query) => query.Reverse();
        protected override IEnumerable<T> PostProcess(IEnumerable<T> entities) => entities.Reverse();
        protected override bool GetHasNextPage(CursorPagingArguments arguments, bool extraRowRetrieved) => arguments.Before != null;
        protected override bool GetHasPrevPage(CursorPagingArguments arguments, bool extraRowRetrieved) => extraRowRetrieved;
    }
}