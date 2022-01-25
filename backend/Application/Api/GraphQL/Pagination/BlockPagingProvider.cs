using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class BlockPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
        => new BlockPagingHandler(options);
}