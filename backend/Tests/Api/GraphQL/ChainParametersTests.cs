using Application.Api.GraphQL;
using FluentAssertions;

namespace Tests.Api.GraphQL;

public sealed class ChainParametersTests
{
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