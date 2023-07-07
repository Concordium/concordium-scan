using Application.Api.GraphQL.Extensions;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tests.TestUtilities.Stubs;
using ExchangeRate = Concordium.Sdk.Types.ExchangeRate;
using GasRewards = Concordium.Sdk.Types.GasRewards;
using TransactionFeeDistribution = Concordium.Sdk.Types.TransactionFeeDistribution;

namespace Tests.TestUtilities.Builders;

public class ChainParametersV0Builder
{
    private AmountFraction _electionDifficulty = AmountFraction.From(0.5m);
    private ExchangeRate _euroPerEnergy = new ExchangeRate(1, 3);
    private ExchangeRate _microCcdPerEuro = new ExchangeRate(2, 5);
    private Epoch _bakerCooldownEpochs = new Epoch(12);
    private CredentialsPerBlockLimit _credentialsPerBlockLimit = new CredentialsPerBlockLimit(7);
    private MintDistributionCpv0 _mintDistribution;
    private TransactionFeeDistribution _transactionFeeDistribution = new (
        AmountFraction.From(0.5m),
        AmountFraction.From(0.6m));
    private GasRewards _gasRewards = new(
        AmountFraction.From(0.21m),
        AmountFraction.From(0.22m),
        AmountFraction.From(0.23m),
        AmountFraction.From(0.24m));
    private AccountAddress _foundationAccount = AccountAddress.From(new byte[32]);
    private CcdAmount _minimumThresholdForBaking = CcdAmount.FromCcd(15000);
    private readonly RootKeys _rootKeys = SimpleStubs.RootKeysStub();
    private readonly Level1Keys _higherLevelKeys = SimpleStubs.Level1KeysStub();
    private readonly AuthorizationsV0 _authorizationsV0 = SimpleStubs.AuthorizationsV0Stub();

    public ChainParametersV0Builder()
    {
        var _ = MintRateExtensions.TryParse(0.2m, out var mintPerSlot);
        _mintDistribution = new MintDistributionCpv0(
             mintPerSlot!.Value,
            AmountFraction.From(0.3m),
            AmountFraction.From(0.4m));
    }
    
    public Concordium.Sdk.Types.ChainParametersV0 Build()
    {
        return new Concordium.Sdk.Types.ChainParametersV0(
            _electionDifficulty,
            _euroPerEnergy, 
            _microCcdPerEuro, 
            _bakerCooldownEpochs,
            _credentialsPerBlockLimit,
            _mintDistribution,
            _transactionFeeDistribution,
            _gasRewards,
            _foundationAccount,
            _minimumThresholdForBaking,
            SimpleStubs.RootKeysStub(),
            SimpleStubs.Level1KeysStub(),
            SimpleStubs.AuthorizationsV0Stub());
    }

    public ChainParametersV0Builder WithElectionDifficulty(AmountFraction value)
    {
        _electionDifficulty = value;
        return this;
    }

    public ChainParametersV0Builder WithEuroPerEnergy(ulong numerator, ulong denominator)
    {
        _euroPerEnergy = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV0Builder WithMicroCcdPerEuro(ulong numerator, ulong denominator)
    {
        _microCcdPerEuro = new ExchangeRate(numerator, denominator);
        return this;
    }

    public ChainParametersV0Builder WithBakerCooldownEpochs(Epoch value)
    {
        _bakerCooldownEpochs = value;
        return this;
    }

    public ChainParametersV0Builder WithCredentialsPerBlockLimit(CredentialsPerBlockLimit value)
    {
        _credentialsPerBlockLimit = value;
        return this;
    }

    public ChainParametersV0Builder WithMintDistributionCpv0(MintDistributionCpv0 mintDistributionCpv0)
    {
        _mintDistribution = mintDistributionCpv0;
        return this;
    }
    
    public ChainParametersV0Builder WithTransactionFeeDistribution(TransactionFeeDistribution transactionFeeDistribution)
    {
        _transactionFeeDistribution = transactionFeeDistribution;
        return this;
    }
    
    public ChainParametersV0Builder WithGasRewards(GasRewards gasRewards)
    {
        _gasRewards = gasRewards;
        return this;
    }

    public ChainParametersV0Builder WithFoundationAccount(AccountAddress accountAddress)
    {
        _foundationAccount = accountAddress;
        return this;
    }
    
    public ChainParametersV0Builder WithMinimumThresholdForBaking(CcdAmount value)
    {
        _minimumThresholdForBaking = value;
        return this;
    }
}