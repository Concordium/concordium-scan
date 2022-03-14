using System.Linq;

namespace ConcordiumSdk.NodeApi.Types;

public class BlockSummary
{
    public TransactionSummary[] TransactionSummaries { get; init; }
    public SpecialEvent[] SpecialEvents { get; init; }
    public FinalizationData? FinalizationData { get; init; }
    public Updates Updates { get; init; }

    public IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        IEnumerable<AccountBalanceUpdate> result = Array.Empty<AccountBalanceUpdate>();
        
        foreach (var transactionSummary in TransactionSummaries)
            result = result.Concat(transactionSummary.GetAccountBalanceUpdates());
        
        foreach (var specialEvent in SpecialEvents)
            result = result.Concat(specialEvent.GetAccountBalanceUpdates());
        
        return result;
    }
}