namespace ConcordiumSdk.NodeApi.Types;

public abstract record RootUpdate;

public record RootKeysRootUpdate(
    HigherLevelAccessStructureRootKeys Content) : RootUpdate;

public record Level1KeysRootUpdate(
    HigherLevelAccessStructureLevel1Keys Content) : RootUpdate;

public record Level2KeysRootUpdate(
    AuthorizationsV0 Content) : RootUpdate;

public record Level2KeysV1RootUpdate(
    AuthorizationsV1 Content) : RootUpdate;
