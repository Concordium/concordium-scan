using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Api.GraphQL.JsonConverters;

public class AccountAddressConverter : JsonConverter<AccountAddress> 
{
    public override AccountAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new InvalidOperationException("Expected a string token type.");

        return new AccountAddress(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, AccountAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Address);
    }
}