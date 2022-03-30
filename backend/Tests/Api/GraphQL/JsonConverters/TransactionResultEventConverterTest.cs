using System.Text.Json;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Api.GraphQL.JsonConverters;

public class TransactionResultEventConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions = EfCoreJsonSerializerOptionsFactory.Create();

    [Fact]
    public void ChainUpdateEnqueued_RootKeysChainUpdatePayload()
    {
        var start = new ChainUpdateEnqueued(new DateTimeOffset(2020, 06, 01, 12, 31, 42, 123, TimeSpan.Zero), new RootKeysChainUpdatePayload());

        var serialized = JsonSerializer.Serialize(start, _serializerOptions);
        JsonAssert.Equivalent(@"{""EffectiveTime"":""2020-06-01T12:31:42.123+00:00"",""Payload"":{""tag"":10,""data"":{}}}", serialized);

        var deserialized = JsonSerializer.Deserialize<ChainUpdateEnqueued>(serialized, _serializerOptions);
        deserialized.Should().Be(start);
    }
    
    [Fact]
    public void ChainUpdateEnqueued_Level1KeysChainUpdatePayload()
    {
        var start = new ChainUpdateEnqueued(new DateTimeOffset(2020, 06, 01, 12, 31, 42, 123, TimeSpan.Zero), new Level1KeysChainUpdatePayload());

        var serialized = JsonSerializer.Serialize(start, _serializerOptions);
        JsonAssert.Equivalent(@"{""EffectiveTime"":""2020-06-01T12:31:42.123+00:00"",""Payload"":{""tag"":11,""data"":{}}}", serialized);

        var deserialized = JsonSerializer.Deserialize<ChainUpdateEnqueued>(serialized, _serializerOptions);
        deserialized.Should().Be(start);
    }
}