using Application.Api.GraphQL.Accounts;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class AccountTokensDescendingPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new DescendingValueCursorPagingAlgorithm<AccountToken>(cursorSerializer, x => x.Index);

        return new GenericCursorPagingHandler<AccountToken>(options, algorithm);
    }
}