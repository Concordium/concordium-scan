using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class BakingRewardsSpecialEventBuilder
{
    public BakingRewardsSpecialEvent Build()
    {
        return new BakingRewardsSpecialEvent
        {
            Remainder = CcdAmount.FromMicroCcd(12),
            BakerRewards = new AccountAddressAmount[]
            {
                new()
                {
                    Amount = CcdAmount.FromMicroCcd(122211),
                    Address = new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi")
                },
                new()
                {
                    Amount = CcdAmount.FromMicroCcd(324111123),
                    Address = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")
                }
            }
        };
    }
}