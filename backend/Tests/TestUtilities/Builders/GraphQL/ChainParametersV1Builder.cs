using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders.GraphQL;

public class ChainParametersV1Builder
{
    private decimal _electionDifficulty = 0.5m;
    private ExchangeRate _euroPerEnergy = new() { Numerator = 1, Denominator = 3 };
    private ExchangeRate _microCcdPerEuro = new() { Numerator = 2, Denominator = 5 };
    private ulong _poolOwnerCooldown = 150;
    private ulong _delegatorCooldown = 140;
    private ulong _rewardPeriodLength = 5;
    private decimal _mintPerPayday = 0.02m;
    private ushort _accountCreationLimit = 7;
    private RewardParametersV1 _rewardParameters = new RewardParametersV1Builder().Build();
    private AccountAddress _foundationAccountAddress = new("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
    private decimal _passiveFinalizationCommission = 0.01m;
    private decimal _passiveBakingCommission = 0.01m;
    private decimal _passiveTransactionCommission = 0.01m;
    private CommissionRange _finalizationCommissionRange = new() { Min = 1.0m, Max = 1.2m };
    private CommissionRange _bakingCommissionRange = new() { Min = 1.5m, Max = 1.7m };
    private CommissionRange _transactionCommissionRange = new() { Min = 0.7m, Max = 0.9m };
    private ulong _minimumEquityCapital = 14_000_000_000;
    private decimal _capitalBound = 0.25m;
    private LeverageFactor _leverageBound = new() { Numerator = 3, Denominator = 1 };

    public ChainParametersV1 Build()
    {
        return new ChainParametersV1
        {
            ElectionDifficulty = _electionDifficulty,
            EuroPerEnergy = _euroPerEnergy,
            MicroCcdPerEuro = _microCcdPerEuro,
            PoolOwnerCooldown = _poolOwnerCooldown,
            DelegatorCooldown = _delegatorCooldown,
            RewardPeriodLength = _rewardPeriodLength,
            MintPerPayday = _mintPerPayday,
            AccountCreationLimit = _accountCreationLimit,
            RewardParameters = _rewardParameters,
            FoundationAccountAddress = _foundationAccountAddress,
            PassiveFinalizationCommission = _passiveFinalizationCommission,
            PassiveBakingCommission = _passiveBakingCommission,
            PassiveTransactionCommission = _passiveTransactionCommission,
            FinalizationCommissionRange = _finalizationCommissionRange,
            BakingCommissionRange = _bakingCommissionRange,
            TransactionCommissionRange = _transactionCommissionRange,
            MinimumEquityCapital = _minimumEquityCapital,
            CapitalBound = _capitalBound,
            LeverageBound = _leverageBound
        };
    }

    public ChainParametersV1Builder WithElectionDifficulty(decimal value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersV1Builder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate { Numerator = numerator, Denominator = denominator };
        return this;
    }

    public ChainParametersV1Builder WithMicroCcdPerEuro(ulong numerator, ulong denominator)
    {
        _microCcdPerEuro = new ExchangeRate { Numerator = numerator, Denominator = denominator };
        return this;
    }

    public ChainParametersV1Builder WithPoolOwnerCooldown(ulong value)
    {
        _poolOwnerCooldown = value;
        return this;
    }

    public ChainParametersV1Builder WithDelegatorCooldown(ulong value)
    {
        _delegatorCooldown = value;
        return this;
    }

    public ChainParametersV1Builder WithRewardPeriodLength(ulong value)
    {
        _rewardPeriodLength = value;
        return this;
    }

    public ChainParametersV1Builder WithMintPerPayday(decimal value)
    {
        _mintPerPayday = value;
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

    public ChainParametersV1Builder WithFoundationAccountAddress(AccountAddress value)
    {
        _foundationAccountAddress = value;
        return this;
    }
    
    public ChainParametersV1Builder WithPassiveFinalizationCommission(decimal value)
    {
        _passiveFinalizationCommission = value;
        return this;
    }
    
    public ChainParametersV1Builder WithPassiveBakingCommission(decimal value)
    {
        _passiveBakingCommission = value;
        return this;
    }
    
    public ChainParametersV1Builder WithPassiveTransactionCommission(decimal value)
    {
        _passiveTransactionCommission = value;
        return this;
    }
    
    public ChainParametersV1Builder WithFinalizationCommissionRange(CommissionRange value)
    {
        _finalizationCommissionRange = value;
        return this;
    }
    
    public ChainParametersV1Builder WithFinalizationCommissionRange(decimal min, decimal max)
    {
        _finalizationCommissionRange = new() { Min = min, Max = max };
        return this;
    }
    
    public ChainParametersV1Builder WithBakingCommissionRange(CommissionRange value)
    {
        _bakingCommissionRange = value;
        return this;
    }
    
    public ChainParametersV1Builder WithBakingCommissionRange(decimal min, decimal max)
    {
        _bakingCommissionRange = new() { Min = min, Max = max };
        return this;
    }
    
    public ChainParametersV1Builder WithTransactionCommissionRange(CommissionRange value)
    {
        _transactionCommissionRange = value;
        return this;
    }
    
    public ChainParametersV1Builder WithTransactionCommissionRange(decimal min, decimal max)
    {
        _transactionCommissionRange = new() { Min = min, Max = max };
        return this;
    }
    
    public ChainParametersV1Builder WithMinimumEquityCapital(ulong value)
    {
        _minimumEquityCapital = value;
        return this;
    }

    public ChainParametersV1Builder WithCapitalBound(decimal value)
    {
        _capitalBound = value;
        return this;
    }
    
    public ChainParametersV1Builder WithLeverageBound(LeverageFactor value)
    {
        _leverageBound = value;
        return this;
    }
    
    public ChainParametersV1Builder WithLeverageBound(ulong numerator, ulong denominator)
    {
        _leverageBound = new() { Numerator = numerator, Denominator = denominator };
        return this;
    }
}
