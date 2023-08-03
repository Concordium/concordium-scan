using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class ChainParametersConfigurationTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public ChainParametersConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_chain_parameters");
    }

    [Fact]
    public async Task ReadWrite_V0()
    {
        var entity = new ChainParametersV0Builder()
            .WithElectionDifficulty(0.1m)
            .WithEuroPerEnergy(1, 3)
            .WithMicroCcdPerEuro(2, 5)
            .WithBakerCooldownEpochs(4)
            .WithAccountCreationLimit(6)
            .WithRewardParameters(new RewardParametersV0Builder()
                .WithMintDistribution(0.2m, 0.3m, 0.4m)
                .WithTransactionFeeDistribution(0.5m, 0.6m)
                .WithGasRewards(0.7m, 0.8m, 0.9m, 0.95m)
                .Build())
            .WithFoundationAccountAddress(new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"))
            .WithMinimumThresholdForBaking(848482929)
            .Build();

        await WriteEntity(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var read = await dbContext.ChainParameters.SingleAsync();
        read.Id.Should().BeGreaterThan(0);
        
        var typed = Assert.IsType<ChainParametersV0>(read);
        typed.ElectionDifficulty.Should().Be(0.1m);
        typed.EuroPerEnergy.Should().Be(new ExchangeRate { Numerator = 1, Denominator = 3 });
        typed.MicroCcdPerEuro.Should().Be(new ExchangeRate { Numerator = 2, Denominator = 5 });
        typed.BakerCooldownEpochs.Should().Be(4);
        typed.AccountCreationLimit.Should().Be(6);
        typed.RewardParameters.MintDistribution.MintPerSlot.Should().Be(0.2m);
        typed.RewardParameters.MintDistribution.BakingReward.Should().Be(0.3m);
        typed.RewardParameters.MintDistribution.FinalizationReward.Should().Be(0.4m);
        typed.RewardParameters.TransactionFeeDistribution.Baker.Should().Be(0.5m);
        typed.RewardParameters.TransactionFeeDistribution.GasAccount.Should().Be(0.6m);
        typed.RewardParameters.GasRewards.Baker.Should().Be(0.7m);
        typed.RewardParameters.GasRewards.FinalizationProof.Should().Be(0.8m);
        typed.RewardParameters.GasRewards.AccountCreation.Should().Be(0.9m);
        typed.RewardParameters.GasRewards.ChainUpdate.Should().Be(0.95m);
        typed.FoundationAccountAddress.AsString.Should().Be("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        typed.MinimumThresholdForBaking.Should().Be(848482929);
    }
    
    /// <summary>
    /// Values that cannot be stored in (signed) bigint are spotted in the wild.
    /// Therefore we use the less effective "numeric" data type to store these! 
    /// </summary>
    [Fact]
    public async Task ReadWrite_V0_MaxValues()
    {
        var entity = new ChainParametersV0Builder()
            .WithEuroPerEnergy(ulong.MaxValue, ulong.MaxValue)
            .WithMicroCcdPerEuro(ulong.MaxValue, ulong.MaxValue)
            .Build();

        await WriteEntity(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var read = await dbContext.ChainParameters.SingleAsync();
        var typed = Assert.IsType<ChainParametersV0>(read);
        typed.EuroPerEnergy.Should().Be(new ExchangeRate { Numerator = ulong.MaxValue, Denominator = ulong.MaxValue });
        typed.MicroCcdPerEuro.Should().Be(new ExchangeRate { Numerator = ulong.MaxValue, Denominator = ulong.MaxValue });
    }

    [Fact]
    public async Task ReadWrite_V1()
    {
        var entity = new ChainParametersV1Builder()
            .WithElectionDifficulty(0.1m)
            .WithEuroPerEnergy(1, 3)
            .WithMicroCcdPerEuro(2, 5)
            .WithPoolOwnerCooldown(170)
            .WithDelegatorCooldown(150)
            .WithRewardPeriodLength(4)
            .WithMintPerPayday(0.0001443m)
            .WithAccountCreationLimit(6)
            .WithRewardParameters(new RewardParametersV1Builder()
                .WithMintDistribution(0.3m, 0.4m)
                .WithTransactionFeeDistribution(0.5m, 0.6m)
                .WithGasRewards(0.7m, 0.8m, 0.9m, 0.95m)
                .Build())
            .WithFoundationAccountAddress(new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"))
            .WithPassiveFinalizationCommission(0.1m)
            .WithPassiveBakingCommission(0.2m)
            .WithPassiveTransactionCommission(0.3m)
            .WithFinalizationCommissionRange(1.0m, 1.3m)
            .WithBakingCommissionRange(0.05m, 0.07m)
            .WithTransactionCommissionRange(0.01m, 0.02m)
            .WithMinimumEquityCapital(1400000)
            .WithCapitalBound(0.25m)
            .WithLeverageBound(3, 1)
            .Build();

        await WriteEntity(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var read = await dbContext.ChainParameters.SingleAsync();
        read.Id.Should().BeGreaterThan(0);
        
        var typed = Assert.IsType<ChainParametersV1>(read);
        typed.ElectionDifficulty.Should().Be(0.1m);
        typed.EuroPerEnergy.Should().Be(new ExchangeRate { Numerator = 1, Denominator = 3 });
        typed.MicroCcdPerEuro.Should().Be(new ExchangeRate { Numerator = 2, Denominator = 5 });
        typed.PoolOwnerCooldown.Should().Be(170);
        typed.DelegatorCooldown.Should().Be(150);
        typed.RewardPeriodLength.Should().Be(4);
        typed.MintPerPayday.Should().Be(0.0001443m);
        typed.AccountCreationLimit.Should().Be(6);
        typed.RewardParameters.MintDistribution.BakingReward.Should().Be(0.3m);
        typed.RewardParameters.MintDistribution.FinalizationReward.Should().Be(0.4m);
        typed.RewardParameters.TransactionFeeDistribution.Baker.Should().Be(0.5m);
        typed.RewardParameters.TransactionFeeDistribution.GasAccount.Should().Be(0.6m);
        typed.RewardParameters.GasRewards.Baker.Should().Be(0.7m);
        typed.RewardParameters.GasRewards.FinalizationProof.Should().Be(0.8m);
        typed.RewardParameters.GasRewards.AccountCreation.Should().Be(0.9m);
        typed.RewardParameters.GasRewards.ChainUpdate.Should().Be(0.95m);
        typed.FoundationAccountAddress.AsString.Should().Be("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        typed.PassiveFinalizationCommission.Should().Be(0.1m);
        typed.PassiveBakingCommission.Should().Be(0.2m);
        typed.PassiveTransactionCommission.Should().Be(0.3m);
        typed.FinalizationCommissionRange.Min.Should().Be(1.0m);
        typed.FinalizationCommissionRange.Max.Should().Be(1.3m);
        typed.BakingCommissionRange.Min.Should().Be(0.05m);
        typed.BakingCommissionRange.Max.Should().Be(0.07m);
        typed.TransactionCommissionRange.Min.Should().Be(0.01m);
        typed.TransactionCommissionRange.Max.Should().Be(0.02m);
        typed.MinimumEquityCapital.Should().Be(1400000);
        typed.CapitalBound.Should().Be(0.25m);
        typed.LeverageBound.Numerator.Should().Be(3);
        typed.LeverageBound.Denominator.Should().Be(1);
    }

    private async Task WriteEntity(ChainParameters entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.ChainParameters.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}