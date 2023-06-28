using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class FinalizationRewardsSpecialEventBuilder
{
    private CcdAmount _remainder = CcdAmount.FromMicroCcd(52);
    private AccountAddressAmount[] _finalizationRewards = {
        new()
        {
            Amount = CcdAmount.FromMicroCcd(55511115),
            Address = AccountAddress.From("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi")
        },
        new()
        {
            Amount = CcdAmount.FromMicroCcd(91425373),
            Address = AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")
        }
    };

    public FinalizationRewardsSpecialEvent Build()
    {
        return new FinalizationRewardsSpecialEvent
        {
            Remainder = _remainder,
            FinalizationRewards = _finalizationRewards
        };
    }

    public FinalizationRewardsSpecialEventBuilder WithRemainder(CcdAmount value)
    {
        _remainder = value;
        return this;
    }

    public FinalizationRewardsSpecialEventBuilder WithFinalizationRewards(params AccountAddressAmount[] value)
    {
        _finalizationRewards = value;
        return this;
    }
}