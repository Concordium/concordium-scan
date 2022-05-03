using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class BlockSummaryConverter : JsonConverter<BlockSummaryBase>
{
    public override BlockSummaryBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var protocolVersion = ReadInt32IfExists(reader, "protocolVersion");
        return protocolVersion switch
        {
            null => JsonSerializer.Deserialize<BlockSummaryV0>(ref reader, options)!,
            4 => JsonSerializer.Deserialize<BlockSummaryV1>(ref reader, options)!,
            _ => throw new NotImplementedException()
        };
    }

    public override void Write(Utf8JsonWriter writer, BlockSummaryBase value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialize not implemented.");
    }
    
    private int? ReadInt32IfExists(Utf8JsonReader readerClone, string propertyName)
    {
        if (readerClone.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a start object");

        readerClone.Read();
        var startDepth = readerClone.CurrentDepth;
        
        while (!(readerClone.TokenType == JsonTokenType.PropertyName
                 && readerClone.CurrentDepth == startDepth
                 && readerClone.GetString() == propertyName)
               && !(readerClone.TokenType == JsonTokenType.EndObject 
                    && readerClone.CurrentDepth == startDepth))
            readerClone.Read();

        if (readerClone.TokenType == JsonTokenType.EndObject
            && readerClone.CurrentDepth == startDepth)
            return null;
        
        readerClone.Read();
        return readerClone.GetInt32();
    }
}