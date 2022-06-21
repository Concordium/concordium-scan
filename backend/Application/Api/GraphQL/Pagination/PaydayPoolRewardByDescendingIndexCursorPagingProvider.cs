using Application.Api.GraphQL.Bakers;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL.Pagination;

public class PaydayPoolRewardByDescendingIndexCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source) => false;

    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        var cursorSerializer = new OpaqueCursorSerializer();
        var algorithm = new DescendingValueCursorPagingAlgorithm<PaydayPoolReward>(cursorSerializer, x => x.Index);
        return new GenericCursorPagingHandler<PaydayPoolReward>(options, algorithm);
    }
}