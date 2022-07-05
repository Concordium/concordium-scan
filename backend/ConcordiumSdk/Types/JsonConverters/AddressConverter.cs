using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.Types.JsonConverters;

public class AddressConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.StartObject);
        var startDepth = reader.CurrentDepth;
        
        var typeValue = reader.ReadString("type");
        
        reader.ForwardReaderToPropertyValue("address");
        Address result = typeValue switch
        {
            "AddressAccount" => JsonSerializer.Deserialize<AccountAddress>(ref reader, options)!,
            "AddressContract" => JsonSerializer.Deserialize<ContractAddress>(ref reader, options)!,
            _ => throw new JsonException($"Unexpected tag value '{typeValue}'.")
        };

        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, startDepth);
        
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        var typeValue = GetTypeValue(value);

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteStringValue(typeValue);
        writer.WritePropertyName("address");
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        writer.WriteEndObject();
    }

    private string GetTypeValue(Address value)
    {
        if (value is AccountAddress) return "AddressAccount";
        if (value is ContractAddress) return "AddressContract";
        throw new ArgumentException("Unexpected address type");
    }
}