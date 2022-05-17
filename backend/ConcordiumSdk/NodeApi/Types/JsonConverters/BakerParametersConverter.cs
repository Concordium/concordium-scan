using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;
using ConcordiumSdk.Types.JsonConverters;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

/// <summary>
/// Both converters in this file can be removed once Concordium Nodes have been upgraded to at least version 4.0
/// on both test- and mainnet.
/// </summary>
public class BakerParametersConverter : JsonConverter<BakerParameters>
{
    private readonly JsonSerializerOptions? _localOptions;

    public BakerParametersConverter()
    {
        _localOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new CcdAmountConverter()
            }
        };
    }

    public override BakerParameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var content = JsonSerializer.Deserialize<CcdAmount>(ref reader, _localOptions)!;
            return new LegacyBakerParameters(content);
        }
        var result = JsonSerializer.Deserialize<BakerParameters>(ref reader, _localOptions)!;
        return result;
    }

    public override void Write(Utf8JsonWriter writer, BakerParameters value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, _localOptions);
    }
}

public class LegacyBakerParametersConverter : JsonConverter<LegacyBakerParameters>
{
    private readonly JsonSerializerOptions? _localOptions;

    public LegacyBakerParametersConverter()
    {
        _localOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new CcdAmountConverter()
            }
        };
    }

    public override LegacyBakerParameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, LegacyBakerParameters value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.MinimumThresholdForBaking, _localOptions);

    }
}
