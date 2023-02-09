using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Contracts;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class ContractTransactionRelationByDescendingTxnIdCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new DescendingValueCursorPagingAlgorithm<ContractTransactionRelation>(
            cursorSerializer,
            x => x.TransactionId
        );
        
        return new GenericCursorPagingHandler<ContractTransactionRelation>(options, algorithm);
    }
}
