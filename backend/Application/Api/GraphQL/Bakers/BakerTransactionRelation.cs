namespace Application.Api.GraphQL.Bakers;

public class BakerTransactionRelation
{
    public long BakerId { get; set; }
    public long Index { get; set; }
    public long TransactionId { get; set; }
}