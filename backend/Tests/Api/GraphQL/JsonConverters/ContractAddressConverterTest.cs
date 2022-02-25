using System.Text.Json;
using Application.Api.GraphQL;
using Application.Api.GraphQL.JsonConverters;
using FluentAssertions;

namespace Tests.Api.GraphQL.JsonConverters;

public class ContractAddressConverterTest
{
    [Fact]
    public void RoundTripAddress()
    {
        var serializerOptions = new JsonSerializerOptions
        { 
            Converters = { new ContractAddressConverter() }
        };

        var start = new ContractAddress(120, 0);
        var serialized = JsonSerializer.Serialize(start, serializerOptions);
        serialized.Should().Be("\"120,0\"");

        var deserialized = JsonSerializer.Deserialize<ContractAddress>(serialized, serializerOptions);
        deserialized.Should().Be(start);

    }
}