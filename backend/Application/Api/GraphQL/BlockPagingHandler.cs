using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL;

public class BlockPagingHandler : CursorPagingHandler
{
    private readonly BlockPagingAlgorithm _algorithm;

    public BlockPagingHandler(PagingOptions options) : base(options)
    {
        if (IncludeTotalCount) 
            throw new NotSupportedException("Support for total count not implemented!");

        _algorithm = new BlockPagingAlgorithm();
    }

    protected override async ValueTask<Connection> SliceAsync(IResolverContext context, object source, CursorPagingArguments arguments)
    {
        if (source is IQueryable<Block> query)
            return await _algorithm.ApplyPaginationAsync(query, arguments, CancellationToken.None);
        throw new ArgumentException("Can only do paging for IQueryable<Block>");
    }
}