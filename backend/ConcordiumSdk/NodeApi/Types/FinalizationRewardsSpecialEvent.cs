using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class FinalizationRewardsSpecialEvent : SpecialEvent
{
    public AccountAddressAmount[] FinalizationRewards { get; init; }
    public CcdAmount Remainder { get; init; }
}