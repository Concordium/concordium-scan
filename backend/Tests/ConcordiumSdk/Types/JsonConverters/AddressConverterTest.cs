using System.Text.Json;
using ConcordiumSdk.Types;
using ConcordiumSdk.Types.JsonConverters;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.Types.JsonConverters;

public class AddressConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public AddressConverterTest()
    {
        _serializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
        _serializerOptions.Converters.Add(new AddressConverter());
        _serializerOptions.Converters.Add(new AccountAddressConverter());
        _serializerOptions.Converters.Add(new ContractAddressConverter());
    }

    [Fact]
    public void RoundTrip_AccountAddress()
    {
        var json = "{\"type\": \"AddressAccount\", \"address\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\"}";
        var deserialized = JsonSerializer.Deserialize<Address>(json, _serializerOptions);
        var result = Assert.IsType<AccountAddress>(deserialized);
        Assert.Equal("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN", result.AsString);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
    
    [Fact]
    public void RoundTrip_ContractAddress()
    {
        var json = "{\"type\": \"AddressContract\", \"address\": {\"index\": 16, \"subindex\": 2}}";
        var deserialized = JsonSerializer.Deserialize<Address>(json, _serializerOptions);   
        var result = deserialized.Should().BeOfType<ContractAddress>().Subject!;
        result.Index.Should().Be(16);
        result.SubIndex.Should().Be(2);
            
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        JsonAssert.Equivalent(json, serialized);
    }
}