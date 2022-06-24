using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class RootUpdateConverter : JsonConverter<RootUpdate>
{
    private readonly IDictionary<Type, string> _serializeMap;
    
    public RootUpdateConverter()
    {
        _serializeMap = new Dictionary<Type, string>
        {
            { typeof(RootKeysRootUpdate), "rootKeysUpdate" },
            { typeof(Level1KeysRootUpdate), "level1KeysUpdate" },
            { typeof(Level2KeysRootUpdate), "level2KeysUpdate" },
            { typeof(Level2KeysV1RootUpdate), "level2KeysUpdateV1" },
        };
    }

    public override RootUpdate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        RootUpdate? result;
        switch (payloadTypeKey)
        {
            case "rootKeysUpdate":
            {
                var content = JsonSerializer.Deserialize<HigherLevelAccessStructureRootKeys>(ref reader, options)!;
                result = new RootKeysRootUpdate(content);
                break;
            }
            case "level1KeysUpdate":
            {
                var content = JsonSerializer.Deserialize<HigherLevelAccessStructureLevel1Keys>(ref reader, options)!;
                result = new Level1KeysRootUpdate(content);
                break;
            }
            case "level2KeysUpdate":
            {
                var content = JsonSerializer.Deserialize<AuthorizationsV0>(ref reader, options)!;
                result = new Level2KeysRootUpdate(content);
                break;
            }
            case "level2KeysUpdateV1":
            {
                var content = JsonSerializer.Deserialize<AuthorizationsV1>(ref reader, options)!;
                result = new Level2KeysV1RootUpdate(content);
                break;
            }
            default:
                throw new NotImplementedException($"Deserialization of update type '{payloadTypeKey}' is not implemented.");
        }

        reader.Read();
        return result;
    }

    public override void Write(Utf8JsonWriter writer, RootUpdate value, JsonSerializerOptions options)
    {
        if (!_serializeMap.TryGetValue(value.GetType(), out var typeOfUpdateString))
            throw new NotImplementedException($"type of update '{value.GetType()}' is not in the serialize map.");
        
        writer.WriteStartObject();
        writer.WriteString("typeOfUpdate", typeOfUpdateString);
        writer.WritePropertyName("updatePayload");
        object payloadValue = value switch
        {
            RootKeysRootUpdate payload => payload.Content,
            Level1KeysRootUpdate payload => payload.Content,
            Level2KeysRootUpdate payload => payload.Content,
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