using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class AccountAddressTest
{
    [Theory]
    [InlineData("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", "4C80C54A05D675454710E5DD79338D07958644BF6637C25D1078EF80E4E16881")]
    [InlineData("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", "924B1EF8FD95A2719BB9157B7E6FEDB73B454F48344ECCAAE269E41AED019536")]
    public void CreateFromString_ValidAddress(string base58CheckEncodedString, string expectedBytesAsHex)
    {
        var target = new AccountAddress(base58CheckEncodedString);
        Assert.Equal(base58CheckEncodedString, target.AsString);
        Assert.Equal(expectedBytesAsHex, Convert.ToHexString(target.AsBytes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9Q")]  // correct length, but altered last char -> fail checksum 
    [InlineData("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9")]   // too short (removed last char) 
    [InlineData("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P2")] // too long (appended char) 
    public void CreateFromString_InvalidAddress(string invalidInput)
    {
        Assert.ThrowsAny<Exception>(() => new AccountAddress(invalidInput));
    }

    [Fact]
    public void CreateFromBytes_ValidAddress()
    {
        var bytes = Convert.FromHexString("4C80C54A05D675454710E5DD79338D07958644BF6637C25D1078EF80E4E16881");
        var target = new AccountAddress(bytes);
        Assert.Equal("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", target.AsString);
        Assert.Equal("4C80C54A05D675454710E5DD79338D07958644BF6637C25D1078EF80E4E16881", Convert.ToHexString(target.AsBytes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("4C80C54A05D675454710E5DD79338D07958644BF6637C25D1078EF80E4E168")]     // too short (removed last byte) 
    [InlineData("4C80C54A05D675454710E5DD79338D07958644BF6637C25D1078EF80E4E1688110")] // too long (appended byte)
    public void CreateFromBytes_InvalidAddress(string? inputAsHex)
    {
        var input = inputAsHex == null ? null : Convert.FromHexString(inputAsHex);
        Assert.ThrowsAny<Exception>(() => new AccountAddress(input));
    }
}