namespace ConcordiumSdk.NodeApi.Types;

public record UpdateKeysCollectionV0(
    HigherLevelAccessStructureRootKeys RootKeys,
    HigherLevelAccessStructureLevel1Keys Level1Keys,
    AuthorizationsV0 Level2Keys);