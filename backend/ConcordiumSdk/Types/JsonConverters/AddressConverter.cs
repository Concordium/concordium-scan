using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class AddressConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);
        var startDepth = reader.CurrentDepth;
        
        var typeValue = ReadStringValue("type", reader);
        
        ForwardReaderToPropertyValue("address", ref reader);
        Address result = typeValue switch
        {
            "AddressAccount" => JsonSerializer.Deserialize<AccountAddress>(ref reader, options)!,
            "AddressContract" => JsonSerializer.Deserialize<ContractAddress>(ref reader, options)!,
            _ => throw new JsonException($"Unexpected tag value '{typeValue}'.")
        };

        ForwardReaderToTokenTypeAtDepth(ref reader, JsonTokenType.EndObject, startDepth);
        
        return result;
    }

    private static void ForwardReaderToTokenTypeAtDepth(ref Utf8JsonReader reader, JsonTokenType tokenType, int depth)
    {
        var success = true;
        while (!(reader.TokenType == tokenType && reader.CurrentDepth == depth) && success)
            reader.Read();
        
        if (!success) 
            throw new InvalidOperationException($"Did not find token type '{tokenType}' at depth '{depth}' in this reader.");
    }

    private void ForwardReaderToPropertyValue(string propertyName, ref Utf8JsonReader reader)
    {
        var success = true;
        while (!(reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == propertyName) && success)
            success = reader.Read();

        if (!success) 
            throw new InvalidOperationException($"Did not find a property named '{propertyName}' in this reader.");

        reader.Read(); // Forward reader to the property value
    }

    private string ReadStringValue(string propertyName, Utf8JsonReader reader)
    {
        ForwardReaderToPropertyValue(propertyName, ref reader);
        return reader.GetString()!;
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