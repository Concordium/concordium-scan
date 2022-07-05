using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class UnixTimeSecondsConverter : JsonConverter<UnixTimeSeconds>
{
    public override UnixTimeSeconds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.Number);
        return new UnixTimeSeconds(reader.GetInt64());
    }

    public override void Write(Utf8JsonWriter writer, UnixTimeSeconds value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.AsLong);
    }
}