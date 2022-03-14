namespace ConcordiumSdk.NodeApi.Types;

public class UnknownSpecialEvent : SpecialEvent
{
    public string Tag { get; init; }
    
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return Array.Empty<AccountBalanceUpdate>();
    }
}