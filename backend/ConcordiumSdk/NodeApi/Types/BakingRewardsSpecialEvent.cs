using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class BakingRewardsSpecialEvent : SpecialEvent
{
    public AccountAddressAmount[] BakerRewards { get; init; }
    public CcdAmount Remainder { get; init; }
}