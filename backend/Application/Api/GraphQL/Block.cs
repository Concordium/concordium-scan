namespace Application.Api.GraphQL;

public class Block
{
    public string BlockHash { get; set; }
    public int BlockHeight { get; set; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public bool Finalized { get; init; }
    public int TransactionCount { get; init; }
}