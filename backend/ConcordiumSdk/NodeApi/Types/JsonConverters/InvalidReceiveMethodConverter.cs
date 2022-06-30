using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class InvalidReceiveMethodConverter : JsonConverter<InvalidReceiveMethod>
{
    public override InvalidReceiveMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        var receiveName = reader.GetString()!;
        
        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, startDepth);
        
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