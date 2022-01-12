using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class BinaryDataConverter : JsonConverter<BinaryData>
{
    public override BinaryData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("Amount cannot be null.");
        return BinaryData.FromHexString(value);
    }

    public override void Write(Utf8JsonWriter writer, BinaryData value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsHexString);
    }
}