using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class UpdatePayloadConverter : JsonConverter<UpdatePayload>
{
    private readonly IDictionary<Type, string> _serializeUpdateTypeMap;

    public UpdatePayloadConverter()
    {
        _serializeUpdateTypeMap = new Dictionary<Type, string>
        {
            { typeof(AddAnonymityRevokerUpdatePayload), "addAnonymityRevoker" },
            { typeof(AddIdentityProviderUpdatePayload), "addIdentityProvider" },
            { typeof(BakerStakeThresholdUpdatePayload), "bakerStakeThreshold" },
            { typeof(ElectionDifficultyUpdatePayload), "electionDifficulty" },
            { typeof(EuroPerEnergyUpdatePayload), "euroPerEnergy" },
            { typeof(FoundationAccountUpdatePayload), "foundationAccount" },
            { typeof(GasRewardsUpdatePayload), "gASRewards" },
            { typeof(Level1UpdatePayload), "level1" },
            { typeof(MicroGtuPerEuroUpdatePayload), "microGTUPerEuro" },
            { typeof(MintDistributionUpdatePayload), "mintDistribution" },
            { typeof(ProtocolUpdatePayload), "protocol" },
            { typeof(TransactionFeeDistributionUpdatePayload), "transactionFeeDistribution" },
        };
    }

    public override UpdatePayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.PropertyName);
        if (reader.GetString() != "updateType")
            throw new JsonException("First property must be updateType.");

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.String);
        var updateTypeString = reader.GetString()!;

        reader.Read();
        EnsureTokenType(reader, JsonTokenType.PropertyName);
        if (reader.GetString() != "update")
            throw new JsonException("Second property must be update.");

        reader.Read();
        UpdatePayload result;
        
        switch (updateTypeString)
        {
            case "addAnonymityRevoker":
            {
                var content = JsonSerializer.Deserialize<AnonymityRevokerInfo>(ref reader, options)!;
                result = new AddAnonymityRevokerUpdatePayload(content);
                break;
            }
            case "addIdentityProvider":
            {
                var content = JsonSerializer.Deserialize<IdentityProviderInfo>(ref reader, options)!;
                result = new AddIdentityProviderUpdatePayload(content);
                break;
            }
            case "bakerStakeThreshold":
            {
                var content = JsonSerializer.Deserialize<CcdAmount>(ref reader, options);
                result = new BakerStakeThresholdUpdatePayload(content);
                break;
            }
            case "electionDifficulty":
            {
                var content = reader.GetDecimal();
                result = new ElectionDifficultyUpdatePayload(content);
                break;
            }
            case "euroPerEnergy":
            {
                var content = JsonSerializer.Deserialize<ExchangeRate>(ref reader, options)!;
                result = new EuroPerEnergyUpdatePayload(content);
                break;
            }
            case "foundationAccount":
            {
                var content = JsonSerializer.Deserialize<AccountAddress>(ref reader, options)!;
                result = new FoundationAccountUpdatePayload(content);
                break;
            }
            case "gASRewards":
            {
                var content = JsonSerializer.Deserialize<GasRewards>(ref reader, options)!;
                result = new GasRewardsUpdatePayload(content);
                break;
            }
            case "level1":
            {
                var content = JsonSerializer.Deserialize<Level1Update>(ref reader, options)!;
                result = new Level1UpdatePayload(content);
                break;
            }
            case "microGTUPerEuro":
            {
                var content = JsonSerializer.Deserialize<ExchangeRate>(ref reader, options)!;
                result = new MicroGtuPerEuroUpdatePayload(content);
                break;
            }
            case "mintDistribution":
            {
                var content = JsonSerializer.Deserialize<MintDistribution>(ref reader, options)!;
                result = new MintDistributionUpdatePayload(content);
                break;
            }
            case "protocol":
            {
                var content = JsonSerializer.Deserialize<ProtocolUpdate>(ref reader, options)!;
                result = new ProtocolUpdatePayload(content);
                break;
            }
            case "transactionFeeDistribution":
            {
                var content = JsonSerializer.Deserialize<TransactionFeeDistribution>(ref reader, options)!;
                result = new TransactionFeeDistributionUpdatePayload(content);
                break;
            }
            default:
                throw new NotImplementedException($"Deserialization of update type '{updateTypeString}' is not implemented.");
        }

        reader.Read();
        return result;
    }

    public override void Write(Utf8JsonWriter writer, UpdatePayload value, JsonSerializerOptions options)
    {
        if (!_serializeUpdateTypeMap.TryGetValue(value.GetType(), out var updateType))
            throw new NotImplementedException($"{value.GetType()} type is not present in map.");

        writer.WriteStartObject();
        writer.WriteString("updateType", updateType);
        writer.WritePropertyName("update");
        (object payloadValue, Type payloadType) = value switch
        {
            AddAnonymityRevokerUpdatePayload payload => ((object)payload.Content, payload.Content.GetType()),
            AddIdentityProviderUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            BakerStakeThresholdUpdatePayload payload => (payload.Amount, payload.Amount.GetType()),
            ElectionDifficultyUpdatePayload payload => (payload.ElectionDifficulty, payload.ElectionDifficulty.GetType()),
            EuroPerEnergyUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            FoundationAccountUpdatePayload payload => (payload.Account, payload.Account.GetType()),
            GasRewardsUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            Level1UpdatePayload payload => (payload.Content, typeof(Level1Update)),
            MicroGtuPerEuroUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            MintDistributionUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            ProtocolUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            TransactionFeeDistributionUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            _ => throw new NotImplementedException($"Serialization of type {value.GetType()} is not implemented.")
        };
        JsonSerializer.Serialize(writer, payloadValue, payloadType, options);
        writer.WriteEndObject();
    }
    
    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType expectedTokenType)
    {
        if (expectedTokenType != reader.TokenType)
            throw new JsonException($"Must be {expectedTokenType}.");
    }
}