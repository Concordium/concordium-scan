using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class AddressConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);
        
        reader.Read();
        EnsureTokenType(reader, JsonTokenType.PropertyName);
        if (reader.GetString() != "type") throw new JsonException("Expected first property to be 'type'");

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.String);
        var typeValue = reader.GetString();

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.PropertyName);
        if (reader.GetString() != "address") throw new JsonException("Expected first property to be 'type'");

        reader.Read();
        Address result = typeValue switch
        {
            "AddressAccount" => JsonSerializer.Deserialize<AccountAddress>(ref reader, options)!,
            "AddressContract" => JsonSerializer.Deserialize<ContractAddress>(ref reader, options)!,
            _ => throw new JsonException($"Unexpected tag value '{typeValue}'.")
        };
        
        reader.Read(); // Read EndObject (Contract) or String (Account)
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

    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (reader.TokenType != tokenType)
            throw new JsonException($"Must be {tokenType}.");
    }
}