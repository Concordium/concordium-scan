namespace ConcordiumSdk.NodeApi.Types;

public record Updates(
    UpdateKeysCollection Keys,
    ProtocolUpdate? ProtocolUpdate,
    ChainParameters ChainParameters,
    PendingUpdates UpdateQueues);