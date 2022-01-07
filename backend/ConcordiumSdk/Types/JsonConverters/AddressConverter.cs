using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class AddressConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);

        string? type = null;
        string? address = null;
        
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            EnsureTokenType(reader, JsonTokenType.PropertyName);
            var key = reader.GetString()!;

            reader.Read();
            EnsureTokenType(reader, JsonTokenType.String);
            if (key == "type")
                type = reader.GetString()!;
            else if (key == "address")
                address = reader.GetString()!;
            
            reader.Read();
        }

        if (type == null || address == null)
            throw new JsonException("Unexpected JSON for a generic address.");

        return type switch
        {
            "AddressAccount" => new AccountAddress(address),
            "AddressContract" => new ContractAddress(address),
            _ => throw new JsonException($"Unexpected type '{type}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        var typeValue = GetTypeValue(value);

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteStringValue(typeValue);
        writer.WritePropertyName("address");
        writer.WriteStringValue(value.AsString);
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