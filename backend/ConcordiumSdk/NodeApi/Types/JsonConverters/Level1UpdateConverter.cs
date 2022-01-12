using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class Level1UpdateConverter : JsonConverter<Level1Update>
{
    private readonly IDictionary<Type, string> _serializeMap;
    
    public Level1UpdateConverter()
    {
        _serializeMap = new Dictionary<Type, string>
        {
            { typeof(Level2KeysLevel1Update), "level2KeysUpdate" },
            { typeof(Level1KeysLevel1Update), "level1KeysUpdate" }
        };
    }

    public override Level1Update? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);

        reader.Read();
        EnsurePropertyName(reader, "typeOfUpdate");

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.String);
        var payloadTypeKey = reader.GetString()!;
        
        reader.Read();
        EnsurePropertyName(reader, "updatePayload");

        reader.Read();
        Level1Update? result;
        switch (payloadTypeKey)
        {
            case "level1KeysUpdate":
            {
                var content = JsonSerializer.Deserialize<HigherLevelAccessStructureLevel1Keys>(ref reader, options)!;
                result = new Level1KeysLevel1Update(content);
                break;
            }
            case "level2KeysUpdate":
            {
                var content = JsonSerializer.Deserialize<Authorizations>(ref reader, options)!;
                result = new Level2KeysLevel1Update(content);
                break;
            }
            default:
                throw new NotImplementedException($"Deserialization of update type '{payloadTypeKey}' is not implemented.");
        }

        reader.Read();
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Level1Update value, JsonSerializerOptions options)
    {
        if (!_serializeMap.TryGetValue(value.GetType(), out var typeOfUpdateString))
            throw new NotImplementedException($"type of update '{value.GetType()}' is not in the serialize map.");
        
        writer.WriteStartObject();
        writer.WriteString("typeOfUpdate", typeOfUpdateString);
        writer.WritePropertyName("updatePayload");
        object payloadValue = value switch
        {
            Level1KeysLevel1Update payload => payload.Content,
            Level2KeysLevel1Update payload => payload.Content,
            _ => throw new NotImplementedException($"Serialization of type {value.GetType()} is not implemented.")
        };
        JsonSerializer.Serialize(writer, payloadValue, payloadValue.GetType(), options);
        writer.WriteEndObject();
    }

    private static void EnsurePropertyName(Utf8JsonReader reader, string expectedPropertyName)
    {
        EnsureTokenType(reader, JsonTokenType.PropertyName);
        if (reader.GetString() != expectedPropertyName)
            throw new JsonException($"Property must be {expectedPropertyName}.");
    }

    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType expectedTokenType)
    {
        if (expectedTokenType != reader.TokenType)
            throw new JsonException($"Must be {expectedTokenType}.");
    }
}