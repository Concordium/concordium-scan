using Application.Api.GraphQL;
using FluentAssertions;

namespace Tests.Api.GraphQL;

public sealed class ChainParametersTests
{
    [Fact]
    public void GivenChainParametersWithDifferentCommissions_WhenCompare_ThenDifferent()
    {
        // Arrange
        var first = new ChainParametersV2
        {
            BakingCommissionRange = new CommissionRange{Min = 1, Max = 1},
            FinalizationCommissionRange = new CommissionRange(),
            TransactionCommissionRange = new CommissionRange(),
            EuroPerEnergy = new ExchangeRate(),
            MicroCcdPerEuro = new ExchangeRate(),
            LeverageBound = new LeverageFactor(),
            RewardParameters = new RewardParametersV2
            {
                GasRewards = new GasRewardsCpv2(),
                MintDistribution = new MintDistributionV1(),
                TransactionFeeDistribution = new TransactionFeeDistribution()
            }
        };
        var second = new ChainParametersV2
        {
            BakingCommissionRange = new CommissionRange{Min = 0.5m, Max = 1},
            FinalizationCommissionRange = new CommissionRange(),
            TransactionCommissionRange = new CommissionRange(),
            EuroPerEnergy = new ExchangeRate(),
            MicroCcdPerEuro = new ExchangeRate(),
            LeverageBound = new LeverageFactor(),
            RewardParameters = new RewardParametersV2
            {
                GasRewards = new GasRewardsCpv2(),
                MintDistribution = new MintDistributionV1(),
                TransactionFeeDistribution = new TransactionFeeDistribution()
            }
        };
        
        // Act
        var equals = first.Equals(second);

        // Assert
        equals.Should().BeFalse();
    }
    
    [Fact]
    public void GivenChainParametersWithPassiveCommissions_WhenTryGetPassiveCommissions_ThenPresent()
    {
        // Arrange
        const int actualPassiveBakingCommission = 1;
        const int actualPassiveFinalizationCommission = 2;
        const int actualPassiveTransactionCommission = 3;
        var chainParameters = new ChainParametersV1
        {
            PassiveBakingCommission = actualPassiveBakingCommission,
            PassiveFinalizationCommission = actualPassiveFinalizationCommission,
            PassiveTransactionCommission = actualPassiveTransactionCommission 
        };
        
        // Act
        var expected = ChainParameters.TryGetPassiveCommissions(
            chainParameters,
            out var passiveFinalizationCommission,
            out var passiveBakingCommission,
            out var passiveTransactionCommission);
        
        // Assert
        expected.Should().BeTrue();
        passiveFinalizationCommission.Should().Be(actualPassiveFinalizationCommission);
        passiveBakingCommission.Should().Be(actualPassiveBakingCommission);
        passiveTransactionCommission.Should().Be(actualPassiveTransactionCommission);
    }
    
    [Fact]
    public void GivenChainParametersWithoutPassiveCommissions_WhenTryGetPassiveCommissions_ThenNotPresent()
    {
        // Arrange
        var chainParameters = new ChainParametersV0();
        
        // Act
        var expected = ChainParameters.TryGetPassiveCommissions(
            chainParameters,
            out var passiveFinalizationCommission,
            out var passiveBakingCommission,
            out var passiveTransactionCommission);
        
        // Assert
        expected.Should().BeFalse();
        passiveFinalizationCommission.Should().BeNull();
        passiveBakingCommission.Should().BeNull();;
        passiveTransactionCommission.Should().BeNull();;
    }
}
