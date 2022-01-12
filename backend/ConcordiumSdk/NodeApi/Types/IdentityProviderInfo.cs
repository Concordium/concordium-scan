namespace ConcordiumSdk.NodeApi.Types;

public record IdentityProviderInfo(
    uint IpIdentity,
    ArOrIpDescription IpDescription,
    string IpVerifyKey,
    string IpCdiVerifyKey);