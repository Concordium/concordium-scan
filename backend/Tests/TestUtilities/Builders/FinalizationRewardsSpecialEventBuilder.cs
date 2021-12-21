using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class FinalizationRewardsSpecialEventBuilder
{
    public FinalizationRewardsSpecialEvent Build()
    {
        return new FinalizationRewardsSpecialEvent
        {
            Remainder = CcdAmount.FromMicroCcd(52),
            FinalizationRewards = new AccountAddressAmount[]
            {
                new()
                {
                    Amount = CcdAmount.FromMicroCcd(55511115),
                    Address = new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi")
                },
                new()
                {
                    Amount = CcdAmount.FromMicroCcd(91425373),
                    Address = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")
                }
            }
        };
    }
}