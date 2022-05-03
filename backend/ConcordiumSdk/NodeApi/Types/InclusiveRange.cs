namespace ConcordiumSdk.NodeApi.Types;

public record InclusiveRange<T>(
    T Min, 
    T Max);