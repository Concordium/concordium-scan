using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class TimestampedAmountConverter : JsonConverter<TimestampedAmount>
{
    public override TimestampedAmount? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartArray);
        
        reader.Read();
        EnsureTokenType(reader, JsonTokenType.Number);
        var epochMs = reader.GetInt64();
        
        reader.Read();
        EnsureTokenType(reader, JsonTokenType.String);
        var amountAsString = reader.GetString();
        
        reader.Read();
        EnsureTokenType(reader, JsonTokenType.EndArray);

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
    
    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (reader.TokenType != tokenType)
            throw new JsonException($"Must be {tokenType}.");
    }
}