namespace ConcordiumSdk.NodeApi.Types;

public record UpdatesV1(
    UpdateKeysCollectionV1 Keys,
    ProtocolUpdate? ProtocolUpdate,
    ChainParametersV1 ChainParameters,
    PendingUpdatesV1 UpdateQueues);