using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class ChainParametersBuilder
{
    private decimal _electionDifficulty = 0.5m;
    private ExchangeRate _euroPerEnergy = new ExchangeRate(1, 3);
    private ExchangeRate _microGtuPerEuro = new ExchangeRate(2, 5);
    private ulong _bakerCooldownEpochs = 12;
    private ushort _credentialsPerBlockLimit = 7;
    private RewardParameters _rewardParameters = new RewardParametersBuilder().Build();
    private ulong _foundationAccountIndex = 1;
    private CcdAmount _minimumThresholdForBaking = CcdAmount.FromCcd(15000);

    public ChainParameters Build()
    {
        return new ChainParameters(_electionDifficulty, _euroPerEnergy, _microGtuPerEuro, _bakerCooldownEpochs,
            _credentialsPerBlockLimit, _rewardParameters, _foundationAccountIndex, _minimumThresholdForBaking);
    }

    public ChainParametersBuilder WithElectionDifficulty(decimal value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersBuilder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersBuilder WithMicroGtuPerEuro(ulong numerator, ulong denominator)
    {
        _microGtuPerEuro = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersBuilder WithBakerCooldownEpochs(ulong value)
    {
        _bakerCooldownEpochs = value;
        return this;
    }

    public ChainParametersBuilder WithCredentialsPerBlockLimit(ushort value)
    {
        _credentialsPerBlockLimit = value;
        return this;
    }
    
    public ChainParametersBuilder WithRewardParameters(RewardParameters value)
    {
        _rewardParameters = value;
        return this;
    }

    public ChainParametersBuilder WithFoundationAccountIndex(ulong value)
    {
        _foundationAccountIndex = value;
        return this;
    }
    
    public ChainParametersBuilder WithMinimumThresholdForBaking(CcdAmount value)
    {
        _minimumThresholdForBaking = value;
        return this;
    }
}