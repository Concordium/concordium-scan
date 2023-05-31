using Application.NodeApi;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class MintSpecialEventBuilder
{
    private CcdAmount _bakingReward = CcdAmount.FromMicroCcd(54518);
    private CcdAmount _finalizationReward = CcdAmount.FromMicroCcd(77841);
    private CcdAmount _platformDevelopmentCharge = CcdAmount.FromMicroCcd(12566);
    private AccountAddress _foundationAccount = AccountAddress.From("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");

    public MintSpecialEvent Build()
    {
        return new MintSpecialEvent()
        {
            MintBakingReward = _bakingReward,
            MintFinalizationReward = _finalizationReward,
            MintPlatformDevelopmentCharge = _platformDevelopmentCharge,
            FoundationAccount = _foundationAccount
        };
    }

    public MintSpecialEventBuilder WithBakingReward(CcdAmount value)
    {
        _bakingReward = value;
        return this;
    }

    public MintSpecialEventBuilder WithFinalizationReward(CcdAmount value)
    {
        _finalizationReward = value;
        return this;
    }

    public MintSpecialEventBuilder WithPlatformDevelopmentCharge(CcdAmount value)
    {
        _platformDevelopmentCharge = value;
        return this;
    }

    public MintSpecialEventBuilder WithFoundationAccount(AccountAddress value)
    {
        _foundationAccount = value;
        return this;
    }
}