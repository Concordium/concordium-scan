using System.Text.Json;
using ConcordiumSdk.Types;
using ConcordiumSdk.Types.JsonConverters;

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
    }

    [Fact]
    public void RoundTrip_AccountAddress()
    {
        var json = "{\"type\": \"AddressAccount\", \"address\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\"}";
        var deserialized = JsonSerializer.Deserialize<Address>(json, _serializerOptions);
        var result = Assert.IsType<AccountAddress>(deserialized);
        Assert.Equal("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN", result.AsString);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        Assert.Equal(json.Replace(" ", ""), serialized);
    }
    
    [Fact]
    public void RoundTrip_ContractAddress()
    {
        var json = "{\"type\": \"AddressContract\", \"address\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\"}";
        var deserialized = JsonSerializer.Deserialize<Address>(json, _serializerOptions);
        var result = Assert.IsType<ContractAddress>(deserialized);
        Assert.Equal("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN", result.AsString);

        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        Assert.Equal(json.Replace(" ", ""), serialized);
    }
}