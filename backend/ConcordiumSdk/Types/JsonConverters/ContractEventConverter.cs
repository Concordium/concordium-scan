using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class ContractEventConverter : JsonConverter<ContractEvent>
{
    public override ContractEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("Amount cannot be null.");
        return ContractEvent.FromHexString(value);
    }

    public override void Write(Utf8JsonWriter writer, ContractEvent value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsHex);
    }
}