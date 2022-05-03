using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class ChainParametersV0Builder
{
    private decimal _electionDifficulty = 0.5m;
    private ExchangeRate _euroPerEnergy = new ExchangeRate(1, 3);
    private ExchangeRate _microGtuPerEuro = new ExchangeRate(2, 5);
    private ulong _bakerCooldownEpochs = 12;
    private ushort _credentialsPerBlockLimit = 7;
    private RewardParametersV0 _rewardParameters = new RewardParametersV0Builder().Build();
    private ulong _foundationAccountIndex = 1;
    private CcdAmount _minimumThresholdForBaking = CcdAmount.FromCcd(15000);

    public ChainParametersV0 Build()
    {
        return new ChainParametersV0(_electionDifficulty, _euroPerEnergy, _microGtuPerEuro, _bakerCooldownEpochs,
            _credentialsPerBlockLimit, _rewardParameters, _foundationAccountIndex, _minimumThresholdForBaking);
    }

    public ChainParametersV0Builder WithElectionDifficulty(decimal value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersV0Builder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV0Builder WithMicroGtuPerEuro(ulong numerator, ulong denominator)
    {
        _microGtuPerEuro = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV0Builder WithBakerCooldownEpochs(ulong value)
    {
        _bakerCooldownEpochs = value;
        return this;
    }

    public ChainParametersV0Builder WithCredentialsPerBlockLimit(ushort value)
    {
        _credentialsPerBlockLimit = value;
        return this;
    }
    
    public ChainParametersV0Builder WithRewardParameters(RewardParametersV0 value)
    {
        _rewardParameters = value;
        return this;
    }

    public ChainParametersV0Builder WithFoundationAccountIndex(ulong value)
    {
        _foundationAccountIndex = value;
        return this;
    }
    
    public ChainParametersV0Builder WithMinimumThresholdForBaking(CcdAmount value)
    {
        _minimumThresholdForBaking = value;
        return this;
    }
}