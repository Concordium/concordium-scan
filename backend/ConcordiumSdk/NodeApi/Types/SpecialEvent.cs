namespace ConcordiumSdk.NodeApi.Types;

public abstract class SpecialEvent
{
    public abstract IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates();
}