
using Application.Api.GraphQL.Transactions;
using FluentAssertions;

namespace Tests.Api.GraphQL.Transactions;

public class DecodedTransferMemoTests
{
    [Theory]
    [InlineData("1a000186b0", "100016")]
    public async Task Should_Decode_As_Cbor(string hex, string cbor)
    {
        var res = DecodedTransferMemo.CreateFromHex(hex);
        res.DecodeType.Should().Be(TextDecodeType.Cbor);
        res.Text.Should().Be(cbor);
    }
}