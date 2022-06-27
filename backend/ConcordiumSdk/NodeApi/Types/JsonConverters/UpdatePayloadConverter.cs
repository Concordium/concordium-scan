using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Types;
using ConcordiumSdk.Utilities;

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
            { typeof(MintDistributionV0UpdatePayload), "mintDistribution" },
            { typeof(ProtocolUpdatePayload), "protocol" },
            { typeof(TransactionFeeDistributionUpdatePayload), "transactionFeeDistribution" },
            { typeof(CooldownParametersUpdatePayload), "cooldownParametersCPV1" },
            { typeof(PoolParametersUpdatePayload), "poolParametersCPV1" },
            { typeof(TimeParametersUpdatePayload), "timeParametersCPV1" },
            { typeof(MintDistributionV1UpdatePayload), "mintDistributionCPV1" },
        };
    }

    public override UpdatePayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.StartObject);
        var startDepth = reader.CurrentDepth;

        var updateTypeString = reader.ReadString("updateType");
        reader.ForwardReaderToPropertyValue("update");
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
                var content = JsonSerializer.Deserialize<BakerParameters>(ref reader, options)!;
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
                var content = JsonSerializer.Deserialize<MintDistributionV0>(ref reader, options)!;
                result = new MintDistributionV0UpdatePayload(content);
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
            case "cooldownParametersCPV1":
            {
                var content = JsonSerializer.Deserialize<CooldownParameters>(ref reader, options)!;
                result = new CooldownParametersUpdatePayload(content);
                break;
            }
            case "poolParametersCPV1":
            {
                var content = JsonSerializer.Deserialize<PoolParameters>(ref reader, options)!;
                result = new PoolParametersUpdatePayload(content);
                break;
            }
            case "timeParametersCPV1":
            {
                var content = JsonSerializer.Deserialize<TimeParameters>(ref reader, options)!;
                result = new TimeParametersUpdatePayload(content);
                break;
            }
            case "mintDistributionCPV1":
            {
                var content = JsonSerializer.Deserialize<MintDistributionV1>(ref reader, options)!;
                result = new MintDistributionV1UpdatePayload(content);
                break;
            }
            default:
                throw new NotImplementedException($"Deserialization of update type '{updateTypeString}' is not implemented.");
        }

        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, startDepth);
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
            BakerStakeThresholdUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            ElectionDifficultyUpdatePayload payload => (payload.ElectionDifficulty, payload.ElectionDifficulty.GetType()),
            EuroPerEnergyUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            FoundationAccountUpdatePayload payload => (payload.Account, payload.Account.GetType()),
            GasRewardsUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            Level1UpdatePayload payload => (payload.Content, typeof(Level1Update)),
            MicroGtuPerEuroUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            MintDistributionV0UpdatePayload payload => (payload.Content, payload.Content.GetType()),
            ProtocolUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            TransactionFeeDistributionUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            CooldownParametersUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            PoolParametersUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            TimeParametersUpdatePayload payload => (payload.Content, payload.Content.GetType()),
            MintDistributionV1UpdatePayload payload => (payload.Content, payload.Content.GetType()),
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