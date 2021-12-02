using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class AccountAddressConverter : JsonConverter<AccountAddress>
{
    public override AccountAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("BlockHash cannot be null.");
        return new AccountAddress(value);
    }

    public override void Write(Utf8JsonWriter writer, AccountAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsString);
    }
}