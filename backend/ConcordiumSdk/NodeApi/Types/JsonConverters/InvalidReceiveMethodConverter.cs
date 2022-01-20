using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class InvalidReceiveMethodConverter : JsonConverter<InvalidReceiveMethod>
{
    public override InvalidReceiveMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // --> tag property name
        reader.Read(); // --> tag property value
        reader.Read(); // --> contents property name
        reader.Read(); // --> [
        reader.Read(); // --> ModuleRef
        var moduleRefString = reader.GetString()!;
        reader.Read(); // --> Receive name
        var receiveName = reader.GetString()!;
        reader.Read(); // --> ]
        reader.Read(); // --> end object
        return new InvalidReceiveMethod(new ModuleRef(moduleRefString), receiveName);
    }

    public override void Write(Utf8JsonWriter writer, InvalidReceiveMethod value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contents");
        writer.WriteStartArray();
        writer.WriteStringValue(value.ModuleRef.AsString);
        writer.WriteStringValue(value.ReceiveName);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}