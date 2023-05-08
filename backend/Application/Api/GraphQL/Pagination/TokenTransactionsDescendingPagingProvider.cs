using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Tokens;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class TokenTransactionsDescendingPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new DescendingValueCursorPagingAlgorithm<TokenTransaction>(cursorSerializer, x => x.TransactionId);

        return new GenericCursorPagingHandler<TokenTransaction>(options, algorithm);
    }
}