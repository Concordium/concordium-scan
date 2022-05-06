using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class SpecialEventJsonConverter : JsonConverter<SpecialEvent>
{
    public override SpecialEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var tagValue = reader.ReadString("tag")!;

        SpecialEvent result = tagValue switch
        {
            "Mint" => JsonSerializer.Deserialize<MintSpecialEvent>(ref reader, options)!,
            "FinalizationRewards" => JsonSerializer.Deserialize<FinalizationRewardsSpecialEvent>(ref reader, options)!,
            "BlockReward" => JsonSerializer.Deserialize<BlockRewardSpecialEvent>(ref reader, options)!,
            "BakingRewards" => JsonSerializer.Deserialize<BakingRewardsSpecialEvent>(ref reader, options)!,
            "PaydayFoundationReward" => JsonSerializer.Deserialize<PaydayFoundationRewardSpecialEvent>(ref reader, options)!,
            "PaydayPoolReward" => JsonSerializer.Deserialize<PaydayPoolRewardSpecialEvent>(ref reader, options)!,
            "PaydayAccountReward" => JsonSerializer.Deserialize<PaydayAccountRewardSpecialEvent>(ref reader, options)!,
            "BlockAccrueReward" => JsonSerializer.Deserialize<BlockAccrueRewardSpecialEvent>(ref reader, options)!,
            _ => throw new NotImplementedException($"Can not deserialize {tagValue}")
        };

        return result;
    }

    public override void Write(Utf8JsonWriter writer, SpecialEvent value, JsonSerializerOptions options)
    {
        // Currently only deserialize needed!
        throw new NotImplementedException();
    }
}