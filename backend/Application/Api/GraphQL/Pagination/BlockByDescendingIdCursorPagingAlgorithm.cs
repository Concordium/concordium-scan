namespace Application.Api.GraphQL.Pagination;

public class BlockByDescendingIdCursorPagingAlgorithm : CursorPagingAlgorithmBase<Block>
{
    private readonly ICursorSerializer _cursorSerializer;

    public BlockByDescendingIdCursorPagingAlgorithm(ICursorSerializer cursorSerializer)
    {
        _cursorSerializer = cursorSerializer;
    }

    protected override IQueryable<Block> ApplyAfterFilter(IQueryable<Block> query, string serializedCursor) =>
        query.Where(x => x.Id < _cursorSerializer.Deserialize(serializedCursor));
    
    protected override IQueryable<Block> ApplyBeforeFilter(IQueryable<Block> query, string serializedCursor) =>
        query.Where(x => x.Id > _cursorSerializer.Deserialize(serializedCursor));

    protected override string GetSerializedCursor(Block entity) => _cursorSerializer.Serialize(entity.Id);
}