using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

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
        if (tagValue == null)
            throw new JsonException("Expected tag value to be non-null.");

        SpecialEvent result = tagValue switch
        {
            "Mint" => JsonSerializer.Deserialize<MintSpecialEvent>(ref reader, options)!,
            "FinalizationRewards" => JsonSerializer.Deserialize<FinalizationRewardsSpecialEvent>(ref reader, options)!,
            "BlockReward" => JsonSerializer.Deserialize<BlockRewardSpecialEvent>(ref reader, options)!,
            "BakingRewards" => JsonSerializer.Deserialize<BakingRewardsSpecialEvent>(ref reader, options)!,
            _ => JsonSerializer.Deserialize<UnknownSpecialEvent>(ref reader, options)!
        };

        return result;
    }

    public override void Write(Utf8JsonWriter writer, SpecialEvent value, JsonSerializerOptions options)
    {
        // Currently only deserialize needed!
        throw new NotImplementedException();
    }
}