using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class BlockSummaryConverter : JsonConverter<BlockSummaryBase>
{
    public override BlockSummaryBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var protocolVersion = reader.ReadInt32("protocolVersion", false);
        return protocolVersion switch
        {
            null or 1 or 2 or 3 => JsonSerializer.Deserialize<BlockSummaryV0>(ref reader, options)!,
            >= 4 => JsonSerializer.Deserialize<BlockSummaryV1>(ref reader, options)!,
            _ => throw new NotImplementedException($"Deserialization of block summary with protocol version {protocolVersion} not implemented.")
        };
    }

    public override void Write(Utf8JsonWriter writer, BlockSummaryBase value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialize not implemented.");
    }
}