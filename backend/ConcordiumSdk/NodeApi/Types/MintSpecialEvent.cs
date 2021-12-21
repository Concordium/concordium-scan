using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class MintSpecialEvent : SpecialEvent
{
    public CcdAmount MintBakingReward { get; init; }
    public CcdAmount MintFinalizationReward { get; init; }
    public CcdAmount MintPlatformDevelopmentCharge { get; init; }
    public AccountAddress FoundationAccount { get; init; }
}