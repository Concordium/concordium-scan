using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class BlockByDescendingIdCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
        => new GenericCursorPagingHandler<Block>(options, new BlockByDescendingIdCursorPagingAlgorithm(new OpaqueCursorSerializer()));
}