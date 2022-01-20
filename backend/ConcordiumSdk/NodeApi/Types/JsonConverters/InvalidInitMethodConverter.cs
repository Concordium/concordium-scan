using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class InvalidInitMethodConverter : JsonConverter<InvalidInitMethod>
{
    public override InvalidInitMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // --> tag property name
        reader.Read(); // --> tag property value
        reader.Read(); // --> contents property name
        reader.Read(); // --> [
        reader.Read(); // --> ModuleRef
        var moduleRefString = reader.GetString()!;
        reader.Read(); // --> Init name
        var initName = reader.GetString()!;
        reader.Read(); // --> ]
        reader.Read(); // --> end object
        return new InvalidInitMethod(new ModuleRef(moduleRefString), initName);
    }

    public override void Write(Utf8JsonWriter writer, InvalidInitMethod value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contents");
        writer.WriteStartArray();
        writer.WriteStringValue(value.ModuleRef.AsString);
        writer.WriteStringValue(value.InitName);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}