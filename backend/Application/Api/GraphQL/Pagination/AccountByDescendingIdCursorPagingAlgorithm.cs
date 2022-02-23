namespace Application.Api.GraphQL.Pagination;

public class AccountByDescendingIdCursorPagingAlgorithm : CursorPagingAlgorithmBase<Account>
{
    private readonly ICursorSerializer _cursorSerializer;

    public AccountByDescendingIdCursorPagingAlgorithm(ICursorSerializer cursorSerializer)
    {
        _cursorSerializer = cursorSerializer;
    }

    protected override IQueryable<Account> ApplyAfterFilter(IQueryable<Account> query, string serializedCursor) =>
        query.Where(x => x.Id < _cursorSerializer.Deserialize(serializedCursor));
    
    protected override IQueryable<Account> ApplyBeforeFilter(IQueryable<Account> query, string serializedCursor) =>
        query.Where(x => x.Id > _cursorSerializer.Deserialize(serializedCursor));

    protected override string GetSerializedCursor(Account entity) => _cursorSerializer.Serialize(entity.Id);
}