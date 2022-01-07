using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class TransactionResultConverter : JsonConverter<TransactionResult>
{
    public override TransactionResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);

        JsonElement? events = null;
        string? outcome = null;
        JsonElement? rejectReason = null;
        
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            EnsureTokenType(reader, JsonTokenType.PropertyName);
            var key = reader.GetString()!;

            reader.Read();
            if (key == "events")
            {
                EnsureTokenType(reader, JsonTokenType.StartArray);
                events = JsonElement.ParseValue(ref reader);
            }
            else if (key == "outcome")
            {
                EnsureTokenType(reader, JsonTokenType.String);
                outcome = reader.GetString();
            }
            else if (key == "rejectReason")
            {
                EnsureTokenType(reader, JsonTokenType.StartObject);
                rejectReason = JsonElement.ParseValue(ref reader);
            }
            
            reader.Read();
        }

        if (outcome == "success")
        {
            if (events == null)
                throw new InvalidOperationException("events were null when trying to deserialize a successful outcome!");
            return new TransactionSuccessResult { Events = events.Value.Deserialize<TransactionResultEvent[]>(options)! };
        }

        if (outcome == "reject")
        {
            if (!rejectReason.HasValue)
                throw new InvalidOperationException("rejectReason was null when trying to deserialized a reject outcome!");
            var tagValue = rejectReason.Value.GetProperty("tag").GetString();
            if (tagValue == null) throw new InvalidOperationException("value of tag was null.");
            return new TransactionRejectResult { Tag = tagValue };
        }

        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, TransactionResult value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
    
    
    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (reader.TokenType != tokenType)
            throw new JsonException($"Must be {tokenType}.");
    }
}