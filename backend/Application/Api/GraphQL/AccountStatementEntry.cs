using HotChocolate;

namespace Application.Api.GraphQL;

public class AccountStatementEntry
{
    [GraphQLIgnore]
    public long AccountId { get; set; }

    [GraphQLIgnore]
    public int Index { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public AccountStatementEntryType EntryType { get; set; }
    
    public long Amount { get; set; }
    
    // TODO: Account balance (kontosaldo efter postering)
    
    /// <summary>
    /// Reference to the block containing the reward or the transaction that resulted in this entry. 
    /// </summary>
    /// 
    [GraphQLIgnore]
    public long BlockId { get; set; }

    /// <summary>
    /// Reference to the transaction that resulted in this entry. Will be null for rewards. 
    /// </summary>
    [GraphQLIgnore]
    public long? TransactionId { get; set; }
}