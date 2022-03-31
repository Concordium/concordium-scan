using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class AccountBakerPendingChangeConverter : JsonConverter<AccountBakerPendingChange>
{
    private readonly IDictionary<string, Type> _deserializeMap;
    private readonly IDictionary<Type, string> _serializeMap;

    public AccountBakerPendingChangeConverter()
    {
        _deserializeMap = new Dictionary<string, Type>()
        {
            { "RemoveBaker", typeof(AccountBakerRemovePending) },
            { "ReduceStake", typeof(AccountBakerReduceStakePending) },
        };
                
        _serializeMap = _deserializeMap
            .ToDictionary(x => x.Value, x => x.Key);
    }

    public override AccountBakerPendingChange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var changeType = ReadChangeTypeValue(reader);
        if (!_deserializeMap.TryGetValue(changeType, out var targetType))
            throw new NotImplementedException($"Deserialization of '{changeType}' is not implemented.");
        return (AccountBakerPendingChange)JsonSerializer.Deserialize(ref reader, targetType, options)!;
    }

    private string ReadChangeTypeValue(Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a start object");

        reader.Read();
        while (!(reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "change"))
            reader.Read();
        
        reader.Read();
        return reader.GetString()!;
    }

    public override void Write(Utf8JsonWriter writer, AccountBakerPendingChange value, JsonSerializerOptions options)
    {
        var valueType = value.GetType();
        var serializedBytes = (Span<byte>)JsonSerializer.SerializeToUtf8Bytes(value, valueType, options);
        var serializedBytesWithoutStartAndEndObjectTag = serializedBytes.Slice(1, serializedBytes.Length - 2);

        if (!_serializeMap.TryGetValue(valueType, out var changeTypeValue))
            throw new InvalidOperationException($"Type {valueType.Name} is not represented in mapping dictionary.");
        
        writer.WriteStartObject();
        writer.WritePropertyName("change");
        writer.WriteStringValue(changeTypeValue);
        if (!serializedBytesWithoutStartAndEndObjectTag.IsEmpty)
            writer.WriteRawValue(serializedBytesWithoutStartAndEndObjectTag, true);
        writer.WriteEndObject();
    }
}