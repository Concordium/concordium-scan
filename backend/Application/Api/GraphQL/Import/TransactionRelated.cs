namespace Application.Api.GraphQL.Import;

public class TransactionRelated<T> 
{
    public long TransactionId { get; init; }
    public int Index { get; init; }
    public T Entity { get; init; }

    public TransactionRelated(long transactionId, int index, T entity)
    {
        TransactionId = transactionId;
        Index = index;
        Entity = entity;
    }
}