namespace Application.Api.GraphQL;

public class BlockRelated<T>
{
    public long BlockId { get; init; }
    public int Index { get; init; }
    public T Entity { get; init; }
}