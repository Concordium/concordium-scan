using System.Linq;

namespace ConcordiumSdk.NodeApi.Types;

public abstract class BlockSummaryBase
{
    public int? ProtocolVersion { get; init; }
    public TransactionSummary[] TransactionSummaries { get; init; }
    public SpecialEvent[] SpecialEvents { get; init; }
    public FinalizationData? FinalizationData { get; init; }

    public IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        foreach (var item in TransactionSummaries.SelectMany(x => x.GetAccountBalanceUpdates()))
            yield return item;

        foreach (var item in SpecialEvents.SelectMany(x => x.GetAccountBalanceUpdates()))
            yield return item;
    }
}