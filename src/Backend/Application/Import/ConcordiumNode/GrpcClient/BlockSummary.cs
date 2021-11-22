namespace Application.Import.ConcordiumNode.GrpcClient;

public class BlockSummary
{
    public TransactionStatus[] TransactionSummaries { get; init; }
    public SpecialEvent[] SpecialEvents { get; init; }
    // public FinalizationData FinalizationData { get; init; }
    // public Updates Updates { get; init; }
}