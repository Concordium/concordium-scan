using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Import.ConcordiumNode.GrpcClient;

public class SpecialEventJsonConverter : JsonConverter<SpecialEvent>
{
    public override SpecialEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readerClone = reader;
            
        if (readerClone.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a start object");
        readerClone.Read();
        if (readerClone.TokenType != JsonTokenType.PropertyName)
            throw new JsonException("Expected property name after start object");
        var propertyName = readerClone.GetString();
        if (propertyName != "tag")
            throw new JsonException("Expected property name to be 'tag'");
        readerClone.Read();
        var tagValue = readerClone.GetString();

        SpecialEvent result = tagValue switch
        {
            "Mint" => JsonSerializer.Deserialize<MintSpecialEvent>(ref reader, options),
            "FinalizationRewards" => JsonSerializer.Deserialize<FinalizationRewardsSpecialEvent>(ref reader, options),
            "BlockReward" => JsonSerializer.Deserialize<BlockRewardSpecialEvent>(ref reader, options),
            _ => JsonSerializer.Deserialize<UnknownSpecialEvent>(ref reader, options)
        };

        return result;
    }

    private static string GetJsonString(Utf8JsonReader reader)
    {
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        using var stream = new MemoryStream();
        var writerOptions = new JsonWriterOptions
        {
            Indented = false
        };
        var writer = new Utf8JsonWriter(stream, writerOptions);
        jsonDocument.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public override void Write(Utf8JsonWriter writer, SpecialEvent value, JsonSerializerOptions options)
    {
        // Currently only deserialize needed!
        throw new NotImplementedException();
    }
}