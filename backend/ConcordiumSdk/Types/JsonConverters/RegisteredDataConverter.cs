using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class RegisteredDataConverter : JsonConverter<RegisteredData>
{
    public override RegisteredData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("Amount cannot be null.");
        return RegisteredData.FromHexString(value);
    }

    public override void Write(Utf8JsonWriter writer, RegisteredData value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsHex);
    }
}