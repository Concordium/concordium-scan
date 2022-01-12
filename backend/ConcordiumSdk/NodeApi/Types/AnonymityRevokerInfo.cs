namespace ConcordiumSdk.NodeApi.Types;

public record AnonymityRevokerInfo(
    uint ArIdentity,
    ArOrIpDescription ArDescription,
    string ArPublicKey);