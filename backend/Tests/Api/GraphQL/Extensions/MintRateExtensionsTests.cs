using Concordium.Sdk.Types;
using FluentAssertions;
using Application.Api.GraphQL.Extensions;

namespace Tests.Api.GraphQL.Extensions;

public class MintRateExtensionsTests
{
    [Theory]
    [InlineData(2506, 0)]
    [InlineData(256, 0)]
    [InlineData(256, 5)]
    [InlineData(2, 5)]
    [InlineData(2, 28)]
    public void GivenDecimal_WhenCalculateMintRate_ThenCorrectMantissaAndExponent(
        uint mantissa, byte exponent)
    {
        // Arrange
        _ = MintRate.TryParse(exponent, mantissa, out var mintRate);
        var calculated = mintRate!.Value.AsDecimal();

        // Act
        var parsed = MintRateExtensions.TryParse(calculated, out var actualMintRate);

        // Assert
        parsed.Should().BeTrue();
        actualMintRate.Should().NotBeNull();
        var (actualExponent, actualMantissa) = actualMintRate!.Value.GetValues();
        actualMantissa.Should().Be(mantissa);
        actualExponent.Should().Be(exponent);
    }

    [Fact]
    public void GivenMantissaBecoming_WhenCalculateMintRate_ThenFailParse()
    {
        // Arrange
        // Create input which will loop for two iterations and then exceed mantissa max size. 
        var input = (((decimal)uint.MaxValue) + 1) / 100;
        
        // Act
        var parsed = MintRateExtensions.TryParse(input, out var actualMintRate);
        
        // Assert
        parsed.Should().BeFalse();
        actualMintRate.Should().BeNull();
    }
}