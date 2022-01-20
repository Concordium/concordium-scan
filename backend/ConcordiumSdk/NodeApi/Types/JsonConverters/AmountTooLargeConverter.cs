using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class AmountTooLargeConverter : JsonConverter<AmountTooLarge>
{
    public override AmountTooLarge? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // --> tag property name
        reader.Read(); // --> tag property value
        reader.Read(); // --> contents property name
        reader.Read(); // --> [
        reader.Read(); // --> ModuleRef
        var address = JsonSerializer.Deserialize<Address>(ref reader, options)!;
        reader.Read(); // --> Receive name
        var amount = JsonSerializer.Deserialize<CcdAmount>(ref reader, options)!;
        reader.Read(); // --> ]
        reader.Read(); // --> end object
        return new AmountTooLarge(address, amount);
    }

    public override void Write(Utf8JsonWriter writer, AmountTooLarge value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contents");
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Address, options);
        JsonSerializer.Serialize(writer, value.Amount, options);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}