using System.Collections.Generic;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Blocks;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class BakingRewardsSpecialEventBuilder
{
    private CcdAmount _remainder = CcdAmount.FromMicroCcd(12);
    private IDictionary<AccountAddress, CcdAmount> _bakerRewards = new Dictionary<AccountAddress, CcdAmount>{
        {
            AccountAddress.From("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi"),
            CcdAmount.FromMicroCcd(122211)
        },
        {
            AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
            CcdAmount.FromMicroCcd(324111123)
        }
    };

    public Concordium.Sdk.Types.BakingRewards Build()
    {
        return new BakingRewards
        (
            Remainder: _remainder,
            Rewards: _bakerRewards
        );
    }

    public BakingRewardsSpecialEventBuilder WithRemainder(CcdAmount value)
    {
        _remainder = value;
        return this;
    }

    public BakingRewardsSpecialEventBuilder WithBakerRewards(params (AccountAddress, CcdAmount)[] value)
    {
        var ccdAmounts = value.ToDictionary(t => t.Item1, t => t.Item2);
        _bakerRewards = ccdAmounts;
        return this;
    }
}