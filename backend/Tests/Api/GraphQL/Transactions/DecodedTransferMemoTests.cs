
using Application.Api.GraphQL.Transactions;
using FluentAssertions;

namespace Tests.Api.GraphQL.Transactions;

public class DecodedTransferMemoTests
{
    [Theory]
    [InlineData("66313030303136", "100016")]
    public void Should_Decode_As_Cbor(string hex, string cbor)
    {
        var res = DecodedText.CreateFromHex(hex);
        res.DecodeType.Should().Be(TextDecodeType.Cbor);
        res.Text.Should().Be(cbor);
    }
}