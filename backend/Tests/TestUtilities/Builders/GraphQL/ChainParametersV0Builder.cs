using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders.GraphQL;

public class ChainParametersV0Builder
{
    private decimal _electionDifficulty = 0.5m;
    private ExchangeRate _euroPerEnergy = new() { Numerator = 1, Denominator = 3 };
    private ExchangeRate _microCcdPerEuro = new() { Numerator = 2, Denominator = 5 };
    private ulong _bakerCooldownEpochs = 12;
    private ushort _accountCreationLimit = 7;
    private RewardParametersV0 _rewardParameters = new RewardParametersV0Builder().Build();
    private AccountAddress _foundationAccountAddress = new("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
    private ulong _minimumThresholdForBaking = 15000UL;

    public ChainParametersV0 Build()
    {
        return new ChainParametersV0
        {
            ElectionDifficulty = _electionDifficulty,
            EuroPerEnergy = _euroPerEnergy,
            MicroCcdPerEuro = _microCcdPerEuro,
            BakerCooldownEpochs = _bakerCooldownEpochs,
            AccountCreationLimit = _accountCreationLimit,
            RewardParameters = _rewardParameters,
            FoundationAccountAddress = _foundationAccountAddress,
            MinimumThresholdForBaking = _minimumThresholdForBaking
        };
    }

    public ChainParametersV0Builder WithElectionDifficulty(decimal value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersV0Builder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate { Numerator = numerator, Denominator = denominator };
        return this;
    }

    public ChainParametersV0Builder WithMicroCcdPerEuro(ulong numerator, ulong denominator)
    {
        _microCcdPerEuro = new ExchangeRate { Numerator = numerator, Denominator = denominator };
        return this;
    }

    public ChainParametersV0Builder WithBakerCooldownEpochs(ulong value)
    {
        _bakerCooldownEpochs = value;
        return this;
    }

    public ChainParametersV0Builder WithAccountCreationLimit(ushort value)
    {
        _accountCreationLimit = value;
        return this;
    }

    public ChainParametersV0Builder WithRewardParameters(RewardParametersV0 value)
    {
        _rewardParameters = value;
        return this;
    }

    public ChainParametersV0Builder WithFoundationAccountAddress(AccountAddress value)
    {
        _foundationAccountAddress = value;
        return this;
    }

    public ChainParametersV0Builder WithMinimumThresholdForBaking(ulong value)
    {
        _minimumThresholdForBaking = value;
        return this;
    }
}