using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class InvalidInitMethodConverter : JsonConverter<InvalidInitMethod>
{
    public override InvalidInitMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.StartObject);
        
        var startDepth = reader.CurrentDepth;
        
        reader.ForwardReaderToPropertyValue("contents");
        reader.EnsureTokenType(JsonTokenType.StartArray);
        reader.Read(); 

        reader.EnsureTokenType(JsonTokenType.String);
        var moduleRefString = reader.GetString()!;
        reader.Read(); 
        
        reader.EnsureTokenType(JsonTokenType.String);
        var initName = reader.GetString()!;
        
        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, startDepth);
        
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