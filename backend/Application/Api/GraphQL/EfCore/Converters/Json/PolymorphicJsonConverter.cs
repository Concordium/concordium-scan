using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public abstract class PolymorphicJsonConverter<T> : JsonConverter<T> 
{
    private readonly IDictionary<Type,int> _serializeMap;
    private readonly IDictionary<int, Type> _deserializeMap;

    protected PolymorphicJsonConverter(IDictionary<Type,int> serializeMap)
    {
        _serializeMap = serializeMap;
        _deserializeMap = _serializeMap.ToDictionary(x => x.Value, x => x.Key);
    }
    
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);
        
        reader.Read();
        EnsurePropertyName(reader, "tag");

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.Number);
        var tagValue = reader.GetInt32();
        if (!_deserializeMap.TryGetValue(tagValue, out var targetType))
            throw new NotImplementedException($"Cannot deserialize data with tag value '{tagValue}' as it is not configured in the serialize map of '{GetType()}'.");
        
        reader.Read();
        EnsurePropertyName(reader, "data");

        reader.Read();
        var result = (T)JsonSerializer.Deserialize(ref reader, targetType, options)!;
        
        reader.Read();
        return result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null) throw new ArgumentNullException();
            
        var type = value.GetType();
        if (!_serializeMap.TryGetValue(type, out var tagValue))
            throw new NotImplementedException($"Cannot serialize data with type '{type}' as it is not configured in the serialize map of '{GetType()}'.");
        
        writer.WriteStartObject();
        writer.WriteNumber("tag", tagValue);
        writer.WritePropertyName("data");
        writer.WriteRawValue(JsonSerializer.Serialize(value, value.GetType(), options));
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
            throw new JsonException($"Must be {expectedTokenType} but was {reader.TokenType}.");
    }

}