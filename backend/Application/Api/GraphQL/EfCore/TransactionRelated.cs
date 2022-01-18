namespace Application.Api.GraphQL.EfCore;

public class TransactionRelated<T> 
{
    public long TransactionId { get; init; }
    public int Index { get; init; }
    public T Entity { get; init; }
}