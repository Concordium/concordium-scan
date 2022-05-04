using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class AccountBakerPendingChangeConverter : JsonConverter<AccountBakerPendingChange>
{
    private readonly IDictionary<Type, string> _serializeMap;

    public AccountBakerPendingChangeConverter()
    {
        _serializeMap = new Dictionary<Type, string>()
        {
            { typeof(AccountBakerRemovePendingV0), "RemoveBaker" },
            { typeof(AccountBakerRemovePendingV1), "RemoveStake" },
            { typeof(AccountBakerReduceStakePendingV0), "ReduceStake" },
            { typeof(AccountBakerReduceStakePendingV1), "ReduceStake" },
        };
    }

    public override AccountBakerPendingChange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var targetType = GetTargetType(reader);
        return (AccountBakerPendingChange)JsonSerializer.Deserialize(ref reader, targetType, options)!;
    }

    private Type GetTargetType(Utf8JsonReader reader)
    {
        var changeType = ReadChangeTypeValue(reader);
        return changeType switch
        {
            "RemoveBaker" => typeof(AccountBakerRemovePendingV0),
            "RemoveStake" => typeof(AccountBakerRemovePendingV1),
            "ReduceStake" => GetReduceStakeType(reader),
            _ => throw new NotImplementedException($"Deserialization of '{changeType}' is not implemented.")
        };
    }

    private Type GetReduceStakeType(Utf8JsonReader reader)
    {
        if (HasPropertyNamed(reader, "effectiveTime"))
            return typeof(AccountBakerReduceStakePendingV1);
        if (HasPropertyNamed(reader, "epoch"))
            return typeof(AccountBakerReduceStakePendingV0);
        throw new NotImplementedException($"Target type cannot be determined for this reduce stake pending change.");
    }

    private bool HasPropertyNamed(Utf8JsonReader readerClone, string propertyName)
    {
        if (readerClone.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a start object");

        var startDepth = readerClone.CurrentDepth;
        readerClone.Read();
        
        var found = false;
        while (!(found = readerClone.TokenType == JsonTokenType.PropertyName
                 && readerClone.CurrentDepth == startDepth + 1
                 && readerClone.GetString() == propertyName)
               && !(readerClone.TokenType == JsonTokenType.EndObject 
                    && readerClone.CurrentDepth == startDepth))
            readerClone.Read();

        return found;
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