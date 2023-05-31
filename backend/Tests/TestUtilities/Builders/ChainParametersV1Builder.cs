using Concordium.Sdk.Types;
using Concordium.Sdk.Types.New;

namespace Tests.TestUtilities.Builders;

public class ChainParametersV1Builder
{
    private decimal _electionDifficulty = 0.5m;
    private ExchangeRate _euroPerEnergy = new(1, 3);
    private ExchangeRate _microGtuPerEuro = new(2, 5);
    private ulong _poolOwnerCooldown = 12;
    private ulong _delegatorCooldown = 13;
    private ulong _rewardPeriodLength = 4;
    private decimal _mintPerPayday = 0.25m;
    private ushort _accountCreationLimit = 7;
    private RewardParametersV1 _rewardParameters = new RewardParametersV1Builder().Build();
    private ulong _foundationAccountIndex = 1;
    private decimal _passiveFinalizationCommission = 0.1m;
    private decimal _passiveBakingCommission = 0.1m;
    private decimal _passiveTransactionCommission = 0.1m;
    private InclusiveRange<decimal> _finalizationCommissionRange = new(1.0m, 1.2m);
    private InclusiveRange<decimal> _bakingCommissionRange = new(1.5m, 1.7m);
    private InclusiveRange<decimal> _transactionCommissionRange = new(0.7m, 0.9m);
    private CcdAmount _minimumEquityCapital = CcdAmount.FromCcd(15000);
    private decimal _capitalBound = 0.25m;
    private LeverageFactor _leverageBound = new(3, 1);
    
    public ChainParametersV1 Build()
    {
        return new ChainParametersV1(_electionDifficulty, _euroPerEnergy, _microGtuPerEuro, _poolOwnerCooldown,
            _delegatorCooldown, _rewardPeriodLength, _mintPerPayday, _accountCreationLimit, _rewardParameters,
            _foundationAccountIndex, _passiveFinalizationCommission, _passiveBakingCommission, _passiveTransactionCommission,
            _finalizationCommissionRange, _bakingCommissionRange, _transactionCommissionRange,
            _minimumEquityCapital, _capitalBound, _leverageBound);
    }

    public ChainParametersV1Builder WithElectionDifficulty(decimal value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersV1Builder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV1Builder WithMicroGtuPerEuro(ulong numerator, ulong denominator)
    {
        _microGtuPerEuro = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV1Builder WithBakerCooldownEpochs(ulong value)
    {
        _poolOwnerCooldown = value;
        return this;
    }

    public ChainParametersV1Builder WithAccountCreationLimit(ushort value)
    {
        _accountCreationLimit = value;
        return this;
    }
    
    public ChainParametersV1Builder WithRewardParameters(RewardParametersV1 value)
    {
        _rewardParameters = value;
        return this;
    }

    public ChainParametersV1Builder WithFoundationAccountIndex(ulong value)
    {
        _foundationAccountIndex = value;
        return this;
    }
}