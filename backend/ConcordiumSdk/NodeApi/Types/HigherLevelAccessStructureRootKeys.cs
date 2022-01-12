namespace ConcordiumSdk.NodeApi.Types;

public record HigherLevelAccessStructureRootKeys(
    UpdatePublicKey[] Keys,
    ushort Threshold);