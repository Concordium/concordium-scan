using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.Pagination;

public class TransactionByDescendingIdCursorPagingAlgorithm : CursorPagingAlgorithmBase<Transaction>
{
    private readonly ICursorSerializer _cursorSerializer;

    public TransactionByDescendingIdCursorPagingAlgorithm(ICursorSerializer cursorSerializer)
    {
        _cursorSerializer = cursorSerializer;
    }

    protected override IQueryable<Transaction> ApplyAfterFilter(IQueryable<Transaction> query, string serializedCursor) =>
        query.Where(x => x.Id < _cursorSerializer.Deserialize(serializedCursor));
    
    protected override IQueryable<Transaction> ApplyBeforeFilter(IQueryable<Transaction> query, string serializedCursor) =>
        query.Where(x => x.Id > _cursorSerializer.Deserialize(serializedCursor));

    protected override string GetSerializedCursor(Transaction entity) => _cursorSerializer.Serialize(entity.Id);
}