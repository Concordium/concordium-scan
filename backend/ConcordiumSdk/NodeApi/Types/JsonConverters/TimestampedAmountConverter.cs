using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class TimestampedAmountConverter : JsonConverter<TimestampedAmount>
{
    public override TimestampedAmount? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.StartArray);
        
        reader.Read();
        reader.EnsureTokenType(JsonTokenType.Number);
        var epochMs = reader.GetInt64();
        
        reader.Read();
        reader.EnsureTokenType(JsonTokenType.String);
        var amountAsString = reader.GetString();

        reader.Read();
        reader.EnsureTokenType(JsonTokenType.EndArray);

        if (!UInt64.TryParse(amountAsString, out var amount))
            throw new JsonException("Could not read amount from JSON.");
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(epochMs);
        var result = new TimestampedAmount(timestamp, CcdAmount.FromMicroCcd(amount));
        return result;
    }

    public override void Write(Utf8JsonWriter writer, TimestampedAmount value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Timestamp.ToUnixTimeMilliseconds());
        writer.WriteStringValue(value.Amount.MicroCcdValue.ToString());
        writer.WriteEndArray();
    }
}