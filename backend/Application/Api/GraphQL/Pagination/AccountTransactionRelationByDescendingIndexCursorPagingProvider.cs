using Application.Api.GraphQL.Accounts;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class AccountTransactionRelationByDescendingIndexCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new AccountTransactionRelationByDescendingIndexCursorPagingAlgorithm(cursorSerializer);
        return new GenericCursorPagingHandler<AccountTransactionRelation>(options, algorithm);
    }
}