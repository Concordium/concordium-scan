using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

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
        writer.WriteStringValue(value.AsString);
    }
}