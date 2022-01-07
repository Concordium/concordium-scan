using System.Text.Json;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.NodeApi.Types.JsonConverters;
using ConcordiumSdk.Types;
using ConcordiumSdk.Types.JsonConverters;
using FluentAssertions;

namespace Tests.ConcordiumSdk.NodeApi.JsonConverters;

public class TransactionResultEventConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions;

    public TransactionResultEventConverterTest()
    {
        _serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _serializerOptions.Converters.Add(new TransactionResultEventConverter());
        _serializerOptions.Converters.Add(new AddressConverter());
        _serializerOptions.Converters.Add(new CcdAmountConverter());
    }

    [Fact]
    public void RoundTrip_Transferred()
    {
        var json = "{\"to\": {\"type\": \"AddressAccount\", \"address\": \"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\"}, \"tag\": \"Transferred\", \"from\": {\"type\": \"AddressContract\", \"address\": \"3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH\"}, \"amount\": \"500000000\"}";
        
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent>(json, _serializerOptions);
        var typed = Assert.IsType<Transferred>(deserialized);
        var toAddress = Assert.IsType<AccountAddress>(typed.To);
        Assert.Equal("43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN", toAddress.AsString);
        var fromAddress = Assert.IsType<ContractAddress>(typed.From);
        Assert.Equal("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH", fromAddress.AsString);
        Assert.Equal<ulong>(500000000, typed.Amount.MicroCcdValue);

        var expected = "{\"tag\":\"Transferred\",\"amount\":\"500000000\",\"to\":{\"type\":\"AddressAccount\",\"address\":\"43CspTCybsaWqH7sU17GVbBBXDtAHEKcDyxGcmwvbF67VEcPnN\"},\"from\":{\"type\":\"AddressContract\",\"address\":\"3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH\"}}";
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        serialized.Should().Be(expected);
    }
    
    [Fact]
    public void FactMethodName()
    {
        var json = "[{\"tag\":\"bar\"},{\"tag\":\"1\"}]";
        var deserialized = JsonSerializer.Deserialize<TransactionResultEvent[]>(json, _serializerOptions);
        var typed = Assert.IsType<TransactionResultEvent[]>(deserialized);
        Assert.Equal(2, typed.Length);
        var first = Assert.IsType<JsonTransactionResultEvent>(typed[0]);
        Assert.Equal("{\"tag\":\"bar\"}", first.Data.ToString());
        var serialized = JsonSerializer.Serialize(deserialized, _serializerOptions);
        Assert.Equal(json, serialized);
    }
}