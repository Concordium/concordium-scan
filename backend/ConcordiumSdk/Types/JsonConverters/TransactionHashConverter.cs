using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class TransactionHashConverter : JsonConverter<TransactionHash>
{
    public override TransactionHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("TransactionHash cannot be null.");
        return new TransactionHash(value);
    }

    public override void Write(Utf8JsonWriter writer, TransactionHash value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsString);
    }
}