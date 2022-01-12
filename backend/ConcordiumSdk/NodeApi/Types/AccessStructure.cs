namespace ConcordiumSdk.NodeApi.Types;

public record AccessStructure(
    ushort[] AuthorizedKeys,
    ushort Threshold);