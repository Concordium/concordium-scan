using Application.Aggregates.Contract.Entities;
using FluentAssertions;

namespace Tests.Aggregates.Contract.Entities;

public sealed class TokenTestWithoutDatabase
{
    [Theory]
    [InlineData(0,0,"", "5Pxr5EUtU")]
    [InlineData(0,0,"aa", "LQMMu3bAg7")]
    [InlineData(1,0,"", "5QTdu98KF")]
    [InlineData(1,0,"aa", "LSYqgoQcb6")]
    [InlineData(1,0,"0a", "LSYXivPSWP")]
    [InlineData(1,0,"01", "LSYWgnCBmz")]
    [InlineData(2,0,"02", "LUjzdxXnte")]
    public void WhenCreateTokenAddress_ThenCorrectOutput(
        ulong contractIndex,
        ulong contractSubindex,
        string tokenId,
        string expectedTokenAddress)
    {
        // Act
        var tokenAddress = Token.EncodeTokenAddress(contractIndex, contractSubindex, tokenId);
        
        // Assert
        tokenAddress.Should().Be(expectedTokenAddress);
    }
}
