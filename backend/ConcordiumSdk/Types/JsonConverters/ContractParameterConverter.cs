using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class ContractParameterConverter : JsonConverter<ContractParameter>
{
    public override ContractParameter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("Parameter cannot be null.");
        return ContractParameter.FromHexString(value);
    }

    public override void Write(Utf8JsonWriter writer, ContractParameter value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsHex);
    }
}