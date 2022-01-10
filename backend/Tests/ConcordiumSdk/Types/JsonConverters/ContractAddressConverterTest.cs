using System.Text.Json;
using ConcordiumSdk.Types;
using ConcordiumSdk.Types.JsonConverters;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.Types.JsonConverters;

public class ContractAddressConverterTest
{
    [Fact]
    public void RoundTrip()
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new ContractAddressConverter() }
        };

        var json = "{\"index\": 16, \"subindex\": 2}";
        var deserialized = JsonSerializer.Deserialize<ContractAddress>(json, serializerOptions)!;
        deserialized.Index.Should().Be(16);
        deserialized.SubIndex.Should().Be(2);
        
        var serialized = JsonSerializer.Serialize(deserialized, serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}