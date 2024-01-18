using Application.Aggregates.Contract.Entities;
using FluentAssertions;

namespace Tests.Aggregates.Contract.Entities;

public sealed class TokenExtensionsTest
{
    [Theory]
    [InlineData(0,0,"", "5Pxr5EUtU")]
    [InlineData(0,0,"aa", "LQMMu3bAg7")]
    [InlineData(1,0,"", "5QTdu98KF")]
    [InlineData(1,0,"aa", "LSYqgoQcb6")]
    [InlineData(1,0,"0a", "LSYXivPSWP")]
    public void WhenCreateTokenAddress_ThenCorrectOutput(
        ulong contractIndex,
        ulong contractSubindex,
        string tokenId,
        string expectedTokenAddress)
    {
        // Arrange
        var tokenExtensions = new Token.TokenExtensions();
        var token = new Token{
            ContractIndex = contractIndex, 
            ContractSubIndex = contractSubindex,
            TokenId = tokenId
        };
        
        // Act
        var tokenAddress = tokenExtensions.GetTokenAddress(token);
        
        // Assert
        tokenAddress.Should().Be(expectedTokenAddress);
    }
}
