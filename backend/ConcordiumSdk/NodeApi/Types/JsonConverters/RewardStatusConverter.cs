using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class RewardStatusConverter : JsonConverter<RewardStatusBase>
{
    public override RewardStatusBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var protocolVersion = reader.ReadInt32("protocolVersion", false);
        return protocolVersion switch
        {
            null or 1 or 2 or 3 => JsonSerializer.Deserialize<RewardStatusV0>(ref reader, options)!,
            4 => JsonSerializer.Deserialize<RewardStatusV1>(ref reader, options)!,
            _ => throw new NotImplementedException()
        };
    }

    public override void Write(Utf8JsonWriter writer, RewardStatusBase value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialize not implemented.");
    }
}