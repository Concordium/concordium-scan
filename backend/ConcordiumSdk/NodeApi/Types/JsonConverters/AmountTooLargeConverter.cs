using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class AmountTooLargeConverter : JsonConverter<AmountTooLarge>
{
    public override AmountTooLarge? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.StartObject);
        
        var startDepth = reader.CurrentDepth;
        
        reader.ForwardReaderToPropertyValue("contents");
        reader.EnsureTokenType(JsonTokenType.StartArray);
        reader.Read(); 

        reader.EnsureTokenType(JsonTokenType.StartObject);
        var address = JsonSerializer.Deserialize<Address>(ref reader, options)!;
        reader.Read(); 
        
        reader.EnsureTokenType(JsonTokenType.String);
        var amount = JsonSerializer.Deserialize<CcdAmount>(ref reader, options)!;
        
        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, startDepth);
        
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