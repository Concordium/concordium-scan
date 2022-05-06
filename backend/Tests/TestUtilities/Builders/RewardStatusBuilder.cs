using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class RewardStatusBuilder
{
    private CcdAmount _totalAmount = CcdAmount.Zero;
    private CcdAmount _totalEncryptedAmount = CcdAmount.Zero;
    private CcdAmount _bakingRewardAccount = CcdAmount.Zero;
    private CcdAmount _finalizationRewardAccount = CcdAmount.Zero;
    private CcdAmount _gasAccount = CcdAmount.Zero;

    public RewardStatusV0 Build()
    {
        return new RewardStatusV0(_totalAmount, _totalEncryptedAmount, _bakingRewardAccount, 
            _finalizationRewardAccount, _gasAccount);
    }

    public RewardStatusBuilder WithTotalAmount(CcdAmount value)
    {
        _totalAmount = value;
        return this;
    }

    public RewardStatusBuilder WithTotalEncryptedAmount(CcdAmount value)
    {
        _totalEncryptedAmount = value;
        return this;
    }

    public RewardStatusBuilder WithBakingRewardAccount(CcdAmount value)
    {
        _bakingRewardAccount = value;
        return this;
    }

    public RewardStatusBuilder WithFinalizationRewardAccount(CcdAmount value)
    {
        _finalizationRewardAccount = value;
        return this;
    }

    public RewardStatusBuilder WithGasAccount(CcdAmount value)
    {
        _gasAccount = value;
        return this;
    }
}