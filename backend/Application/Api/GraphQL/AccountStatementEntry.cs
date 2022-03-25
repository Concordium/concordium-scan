namespace Application.Api.GraphQL;

public class AccountStatementEntry
{
    public long AccountId { get; set; }
    public int Index { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public EntryType EntryType { get; set; }
    public long Amount { get; set; }
    // TODO: Account balance (kontosaldo efter postering)
    
    /// <summary>
    /// Reference to the block containing the reward or the transaction that resulted in this entry. 
    /// </summary>
    /// 
    public long BlockId { get; set; }
    /// <summary>
    /// Reference to the transaction that resulted in this entry. Will be null for rewards. 
    /// </summary>
    public long? TransactionId { get; set; }
}