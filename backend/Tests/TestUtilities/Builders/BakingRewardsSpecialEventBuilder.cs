using Application.Api.GraphQL;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class BakingRewardsSpecialEventBuilder
{
    private CcdAmount _remainder = CcdAmount.FromMicroCcd(12);
    private AccountAddressAmount[] _bakerRewards = {
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
    };

    public BakingRewardsSpecialEvent Build()
    {
        return new BakingRewardsSpecialEvent
        {
            Remainder = _remainder,
            BakerRewards = _bakerRewards
        };
    }

    public BakingRewardsSpecialEventBuilder WithRemainder(CcdAmount value)
    {
        _remainder = value;
        return this;
    }

    public BakingRewardsSpecialEventBuilder WithBakerRewards(params AccountAddressAmount[] value)
    {
        _bakerRewards = value;
        return this;
    }
}