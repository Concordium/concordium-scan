using Application.Api.GraphQL.Extensions;
using Concordium.Sdk.Types;
using Tests.TestUtilities.Stubs;
using AccountAddress = Concordium.Sdk.Types.AccountAddress;
using AmountFraction = Concordium.Sdk.Types.AmountFraction;
using CapitalBound = Concordium.Sdk.Types.CapitalBound;
using ChainParametersV1 = Concordium.Sdk.Types.ChainParametersV1;
using CommissionRanges = Concordium.Sdk.Types.CommissionRanges;
using CredentialsPerBlockLimit = Concordium.Sdk.Types.CredentialsPerBlockLimit;
using Epoch = Concordium.Sdk.Types.Epoch;
using ExchangeRate = Concordium.Sdk.Types.ExchangeRate;
using GasRewards = Concordium.Sdk.Types.GasRewards;
using LeverageFactor = Concordium.Sdk.Types.LeverageFactor;
using MintDistributionCpv1 = Concordium.Sdk.Types.MintDistributionCpv1;
using MintRate = Concordium.Sdk.Types.MintRate;
using RewardPeriodLength = Concordium.Sdk.Types.RewardPeriodLength;
using TransactionFeeDistribution = Concordium.Sdk.Types.TransactionFeeDistribution;

namespace Tests.TestUtilities.Builders;

public class ChainParametersV1Builder
{
    private AmountFraction _electionDifficulty = AmountFraction.From(0.5m);
    private ExchangeRate _euroPerEnergy = new ExchangeRate(1, 3);
    private ExchangeRate _microCcdPerEuro = new ExchangeRate(2, 5);
    private CooldownParameters _cooldownParameters = new CooldownParameters(
        TimeSpan.FromSeconds(12),
        TimeSpan.FromSeconds(13));
    private TimeParameters _timeParameters;
    private CredentialsPerBlockLimit _accountCreationLimit = new CredentialsPerBlockLimit(7);
    private MintDistributionCpv1 _mintDistributionCpv1 = new MintDistributionCpv1(
        AmountFraction.From(0.3m),
        AmountFraction.From(0.4m)
    );
    private TransactionFeeDistribution _transactionFeeDistribution = new (
        AmountFraction.From(0.5m),
        AmountFraction.From(0.6m));
    private GasRewards _gasRewards = new(
        AmountFraction.From(0.21m),
        AmountFraction.From(0.22m),
        AmountFraction.From(0.23m),
        AmountFraction.From(0.24m));
    private AccountAddress _foundationAccount = AccountAddress.From(new byte[32]);
    private PoolParameters _poolParameters = new PoolParameters(
        AmountFraction.From(0.1m),
        AmountFraction.From(0.1m),
        AmountFraction.From(0.1m),
        new CommissionRanges(
            new InclusiveRange<AmountFraction>(AmountFraction.From(1.0m), AmountFraction.From(1.2m)),
            new InclusiveRange<AmountFraction>(AmountFraction.From(1.5m), AmountFraction.From(1.7m)),
            new InclusiveRange<AmountFraction>(AmountFraction.From(0.7m), AmountFraction.From(0.9m))
        ),
        CcdAmount.FromCcd(15000),
        new CapitalBound(AmountFraction.From(0.25m)),
        new LeverageFactor(3, 1)
    );

    public ChainParametersV1Builder()
    {
        var _ = MintRateExtensions.TryParse(0.25m, out var mintPrPayDay);
        _timeParameters = new TimeParameters(
            new RewardPeriodLength(new Epoch(4)),
            mintPrPayDay!.Value
        );
    }
    
    public ChainParametersV1 Build()
    {
        return new ChainParametersV1(
            _electionDifficulty,
            _euroPerEnergy, 
            _microCcdPerEuro,
            _cooldownParameters,
            _timeParameters,
            _accountCreationLimit,
            _mintDistributionCpv1,
            _transactionFeeDistribution,
            _gasRewards,
            _foundationAccount,
            _poolParameters,
            SimpleStubs.RootKeysStub(),
            SimpleStubs.Level1KeysStub(),
            SimpleStubs.AuthorizationsV1Stub());
    }

    public ChainParametersV1Builder WithElectionDifficulty(AmountFraction value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersV1Builder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV1Builder WithMicroCcdPerEuro(ulong numerator, ulong denominator)
    {
        _microCcdPerEuro = new ExchangeRate(numerator, denominator);
        return this;
    }
    
    public ChainParametersV1Builder WithCooldownParameters(CooldownParameters cooldownParameters)
    {
        _cooldownParameters = cooldownParameters;
        return this;
    }
    
    public ChainParametersV1Builder WithTimeParameters(TimeParameters timeParameters)
    {
        _timeParameters = timeParameters;
        return this;
    }
    
    public ChainParametersV1Builder WithMintDistributionCpv1(MintDistributionCpv1 mintDistributionCpv1)
    {
        _mintDistributionCpv1 = mintDistributionCpv1;
        return this;
    }
    
    public ChainParametersV1Builder WithTransactionFeeDistribution(TransactionFeeDistribution transactionFeeDistribution)
    {
        _transactionFeeDistribution = transactionFeeDistribution;
        return this;
    }
    
    public ChainParametersV1Builder WithFoundationAccount(AccountAddress accountAddress)
    {
        _foundationAccount = accountAddress;
        return this;
    }
    
    public ChainParametersV1Builder WithPoolParameters(PoolParameters poolParameters)
    {
        _poolParameters = poolParameters;
        return this;
    }

    public ChainParametersV1Builder WithAccountCreationLimit(CredentialsPerBlockLimit value)
    {
        _accountCreationLimit = value;
        return this;
    }
}