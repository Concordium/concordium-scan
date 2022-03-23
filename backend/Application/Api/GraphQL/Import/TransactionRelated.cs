namespace Application.Api.GraphQL.Import;

public class TransactionRelated<T> 
{
    public long TransactionId { get; init; }
    public int Index { get; init; }
    public T Entity { get; init; }
}