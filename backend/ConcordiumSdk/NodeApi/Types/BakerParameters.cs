using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record BakerParameters(
    CcdAmount MinimumThresholdForBaking);

/// <summary>
/// This type can be removed once Concordium Nodes have been upgraded to at least version 4.0
/// on both test- and mainnet.
/// </summary>
public record LegacyBakerParameters(
    CcdAmount MinimumThresholdForBaking) : BakerParameters(MinimumThresholdForBaking);