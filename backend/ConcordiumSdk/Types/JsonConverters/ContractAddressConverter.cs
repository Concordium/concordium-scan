using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.Types.JsonConverters;

public class ContractAddressConverter : JsonConverter<ContractAddress>
{
    public override ContractAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.StartObject);
        reader.Read();

        ulong? index = null;
        ulong? subIndex = null;
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            reader.EnsureTokenType(JsonTokenType.PropertyName);
            var propertyName = reader.GetString();

            reader.Read();
            reader.EnsureTokenType(JsonTokenType.Number);
            var propertyValue = reader.GetUInt64();

            if (propertyName == "index") index = propertyValue;
            else if (propertyName == "subindex") subIndex = propertyValue;
            else throw new JsonException("Unexpected property in contract address");

            reader.Read();
        }

        if (!(index.HasValue && subIndex.HasValue))
            throw new JsonException("Both index and subindex must have a value.");

        return new ContractAddress(index.Value, subIndex.Value);
    }

    public override void Write(Utf8JsonWriter writer, ContractAddress value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("index", value.Index);
        writer.WriteNumber("subindex", value.SubIndex);
        writer.WriteEndObject();
    }
}
