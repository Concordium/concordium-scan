using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class MintSpecialEventBuilder
{
    public MintSpecialEvent Build()
    {
        return new MintSpecialEvent()
        {
            MintBakingReward = CcdAmount.FromMicroCcd(54518),
            MintFinalizationReward = CcdAmount.FromMicroCcd(77841),
            MintPlatformDevelopmentCharge = CcdAmount.FromMicroCcd(12566),
            FoundationAccount = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")
        };
    }
}