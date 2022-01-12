namespace ConcordiumSdk.NodeApi.Types;

/// <summary>
/// Description either of an anonymity revoker or identity provider.
/// </summary>
public record ArOrIpDescription(
    string Name,
    string Url,
    string Description);