using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class GenericCursorPagingHandler<T> : CursorPagingHandler
{
    private readonly CursorPagingAlgorithmBase<T> _algorithm;

    public GenericCursorPagingHandler(PagingOptions options, CursorPagingAlgorithmBase<T> algorithm) : base(options)
    {
        if (IncludeTotalCount) 
            throw new NotSupportedException("Support for total count not implemented!");

        _algorithm = algorithm;
    }

    protected override async ValueTask<Connection> SliceAsync(IResolverContext context, object source, CursorPagingArguments arguments)
    {
        if (source is IQueryable<T> query)
            return await _algorithm.ApplyPaginationAsync(query, arguments, CancellationToken.None);
        throw new ArgumentException($"Can only do paging for IQueryable<{typeof(T).Name}>");
    }
}