using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class AccountByDescendingIdCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new AccountByDescendingIdCursorPagingAlgorithm(cursorSerializer);
        return new GenericCursorPagingHandler<Account>(options, algorithm);
    }
}