using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class AccountDelegationPendingChangeConverter : JsonConverter<AccountDelegationPendingChange>
{
    private readonly IDictionary<string, Type> _deserializeMap;
    private readonly IDictionary<Type, string> _serializeMap;

    public AccountDelegationPendingChangeConverter()
    {
        _deserializeMap = new Dictionary<string, Type>()
        {
            { "RemoveStake", typeof(AccountDelegationRemovePending) },
            { "ReduceStake", typeof(AccountDelegationReduceStakePending) },
        };
        
        _serializeMap = _deserializeMap
            .ToDictionary(x => x.Value, x => x.Key);
    }

    public override AccountDelegationPendingChange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var changeType = reader.ReadString("change")!;
        if (!_deserializeMap.TryGetValue(changeType, out var tagType))
            throw new NotImplementedException($"Deserialization of '{changeType}' is not implemented.");
        return (AccountDelegationPendingChange)JsonSerializer.Deserialize(ref reader, tagType, options)!;
    }

    public override void Write(Utf8JsonWriter writer, AccountDelegationPendingChange value, JsonSerializerOptions options)
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