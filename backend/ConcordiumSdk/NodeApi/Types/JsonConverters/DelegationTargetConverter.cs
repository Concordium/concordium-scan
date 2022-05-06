using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class DelegationTargetConverter : JsonConverter<DelegationTarget>
{
    private readonly IDictionary<string, Type> _deserializeMap;
    private readonly IDictionary<Type, string> _serializeMap;

    public DelegationTargetConverter()
    {
        _deserializeMap = new Dictionary<string, Type>()
        {
            { "L-Pool", typeof(LPoolDelegationTarget) },
            { "Baker", typeof(BakerDelegationTarget) },
        };
        
        _serializeMap = _deserializeMap
            .ToDictionary(x => x.Value, x => x.Key);
    }

    public override DelegationTarget? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var tagValue = reader.ReadString("delegateType")!;
        if (!_deserializeMap.TryGetValue(tagValue, out var tagType))
            throw new NotImplementedException($"Deserialization of '{tagValue}' is not implemented.");
        return (DelegationTarget)JsonSerializer.Deserialize(ref reader, tagType, options)!;
    }
    
    public override void Write(Utf8JsonWriter writer, DelegationTarget value, JsonSerializerOptions options)
    {
        var valueType = value.GetType();
        var serializedBytes = (Span<byte>)JsonSerializer.SerializeToUtf8Bytes(value, valueType, options);
        var serializedBytesWithoutStartAndEndObjectTag = serializedBytes.Slice(1, serializedBytes.Length - 2);

        if (!_serializeMap.TryGetValue(valueType, out var tagValue))
            throw new InvalidOperationException($"Type {valueType.Name} is not represented in mapping dictionary.");
        
        writer.WriteStartObject();
        writer.WritePropertyName("delegateType");
        writer.WriteStringValue(tagValue);
        if (!serializedBytesWithoutStartAndEndObjectTag.IsEmpty)
            writer.WriteRawValue(serializedBytesWithoutStartAndEndObjectTag, true);
        writer.WriteEndObject();
    }
}