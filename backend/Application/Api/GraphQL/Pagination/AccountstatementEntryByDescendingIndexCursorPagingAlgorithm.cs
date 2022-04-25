using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.Pagination;

public class AccountstatementEntryByDescendingIndexCursorPagingAlgorithm : CursorPagingAlgorithmBase<AccountStatementEntry>
{
    private readonly ICursorSerializer _cursorSerializer;

    public AccountstatementEntryByDescendingIndexCursorPagingAlgorithm(ICursorSerializer cursorSerializer)
    {
        _cursorSerializer = cursorSerializer;
    }

    protected override IQueryable<AccountStatementEntry> ApplyAfterFilter(IQueryable<AccountStatementEntry> query, string serializedCursor) =>
        query.Where(x => x.Index < _cursorSerializer.Deserialize(serializedCursor));
    
    protected override IQueryable<AccountStatementEntry> ApplyBeforeFilter(IQueryable<AccountStatementEntry> query, string serializedCursor) =>
        query.Where(x => x.Index > _cursorSerializer.Deserialize(serializedCursor));

    protected override string GetSerializedCursor(AccountStatementEntry entity) => _cursorSerializer.Serialize(entity.Index);
}