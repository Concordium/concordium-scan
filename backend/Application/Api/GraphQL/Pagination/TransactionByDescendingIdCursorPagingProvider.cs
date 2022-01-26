using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class TransactionByDescendingIdCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new TransactionByDescendingIdCursorPagingAlgorithm(cursorSerializer);
        return new GenericCursorPagingHandler<Transaction>(options, algorithm);
    }
}