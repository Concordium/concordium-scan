namespace Application.Api.GraphQL.Pagination;

public class AccountTransactionRelationByDescendingIndexCursorPagingAlgorithm : CursorPagingAlgorithmBase<AccountTransactionRelation>
{
    private readonly ICursorSerializer _cursorSerializer;

    public AccountTransactionRelationByDescendingIndexCursorPagingAlgorithm(ICursorSerializer cursorSerializer)
    {
        _cursorSerializer = cursorSerializer;
    }

    protected override IQueryable<AccountTransactionRelation> ApplyAfterFilter(IQueryable<AccountTransactionRelation> query, string serializedCursor) =>
        query.Where(x => x.Index < _cursorSerializer.Deserialize(serializedCursor));
    
    protected override IQueryable<AccountTransactionRelation> ApplyBeforeFilter(IQueryable<AccountTransactionRelation> query, string serializedCursor) =>
        query.Where(x => x.Index > _cursorSerializer.Deserialize(serializedCursor));

    protected override string GetSerializedCursor(AccountTransactionRelation entity) => _cursorSerializer.Serialize(entity.Index);
}