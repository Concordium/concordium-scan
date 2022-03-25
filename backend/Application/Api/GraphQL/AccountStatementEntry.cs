namespace Application.Api.GraphQL;

public class AccountStatementEntry
{
    public long AccountId { get; set; }
    public int Index { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public EntryType EntryType { get; set; }
    public long Amount { get; set; }
    // TODO: Account balance (kontosaldo efter postering)
    // TODO: Add reference to transaction where available (tx-fee or transfers)
    public long BlockId { get; set; }
}