using Concordium.Sdk.Types;
using FluentAssertions;
using Application.Api.GraphQL.Extensions;
using Application.Exceptions;
using VerifyTests;

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
        var actualMintRate = MintRateExtensions.From(calculated);

        // Assert
        actualMintRate.Should().NotBeNull();
        var (actualExponent, actualMantissa) = actualMintRate.GetValues();
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
        var action = () => MintRateExtensions.From(input);

        // Assert
        action.Should().Throw<MintRateCalculationException>();
    }
}