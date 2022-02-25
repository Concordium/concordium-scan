using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Api.GraphQL.JsonConverters;

public class ContractAddressConverter : JsonConverter<ContractAddress> 
{
    public override ContractAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new InvalidOperationException("Expected a string token type.");

        var value = reader.GetString()!.Split(",");
        if (value.Length != 2) throw new JsonException("string content is not a contract address.");
        return new ContractAddress(ulong.Parse(value[0]), ulong.Parse(value[1]));
    }

    public override void Write(Utf8JsonWriter writer, ContractAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.Index},{value.SubIndex}");
    }
}