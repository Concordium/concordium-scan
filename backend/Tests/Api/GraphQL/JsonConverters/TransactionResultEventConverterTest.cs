using System.Text.Json;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.Api.GraphQL.JsonConverters;

public class TransactionResultEventConverterTest
{
    private readonly JsonSerializerOptions _serializerOptions = EfCoreJsonSerializerOptionsFactory.Create();
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2020, 06, 01, 12, 31, 42, 123, TimeSpan.Zero);

    [Fact]
    public void ChainUpdateEnqueued_RootKeysChainUpdatePayload()
    {
        var start = new ChainUpdateEnqueued(_anyDateTimeOffset, true, new RootKeysChainUpdatePayload());

        var serialized = JsonSerializer.Serialize(start, _serializerOptions);
        JsonAssert.Equivalent(@"{""EffectiveTime"":""2020-06-01T12:31:42.123+00:00"",""EffectiveImmediately"":true,""Payload"":{""tag"":10,""data"":{}}}", serialized);

        var deserialized = JsonSerializer.Deserialize<ChainUpdateEnqueued>(serialized, _serializerOptions);
        deserialized.Should().Be(start);
    }
    
    [Fact]
    public void ChainUpdateEnqueued_Level1KeysChainUpdatePayload()
    {
        var start = new ChainUpdateEnqueued(_anyDateTimeOffset, true, new Level1KeysChainUpdatePayload());

        var serialized = JsonSerializer.Serialize(start, _serializerOptions);
        JsonAssert.Equivalent(@"{""EffectiveTime"":""2020-06-01T12:31:42.123+00:00"",""EffectiveImmediately"":true,""Payload"":{""tag"":11,""data"":{}}}", serialized);

        var deserialized = JsonSerializer.Deserialize<ChainUpdateEnqueued>(serialized, _serializerOptions);
        deserialized.Should().Be(start);
    }
}