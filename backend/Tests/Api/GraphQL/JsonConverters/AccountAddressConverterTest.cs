using System.Text.Json;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore.Converters.Json;
using FluentAssertions;

namespace Tests.Api.GraphQL.JsonConverters;

public class AccountAddressConverterTest
{
    [Fact]
    public void RoundTripAddress()
    {
        var serializerOptions = new JsonSerializerOptions
        { 
            Converters = { new AccountAddressConverter() }
        };

        var start = new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        var serialized = JsonSerializer.Serialize(start, serializerOptions);
        serialized.Should().Be("\"44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy\"");

        var deserialized = JsonSerializer.Deserialize<AccountAddress>(serialized, serializerOptions);
        deserialized.Should().Be(start);

    }
}