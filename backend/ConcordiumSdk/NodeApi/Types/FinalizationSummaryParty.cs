namespace ConcordiumSdk.NodeApi.Types;

public class FinalizationSummaryParty
{
    public long BakerId { get; init; } // Haskell data type: BakerId : AccountIndex : Word64
    public long Weight { get; init; }
    public bool Signed { get; init; }
    
}