namespace ConcordiumSdk.NodeApi.Types;

public record UpdateKeysCollection(
    HigherLevelAccessStructureRootKeys RootKeys,
    HigherLevelAccessStructureLevel1Keys Level1Keys,
    Authorizations Level2Keys);