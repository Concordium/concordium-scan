namespace ConcordiumSdk.NodeApi;

public class BlockSummary
{
    public TransactionSummary[] TransactionSummaries { get; init; }
    public SpecialEvent[] SpecialEvents { get; init; }
    // public FinalizationData FinalizationData { get; init; }
    // public Updates Updates { get; init; }
}