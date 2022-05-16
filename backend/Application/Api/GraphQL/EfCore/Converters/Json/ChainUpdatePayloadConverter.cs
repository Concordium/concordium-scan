using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class ChainUpdatePayloadConverter : PolymorphicJsonConverter<ChainUpdatePayload>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(ProtocolChainUpdatePayload), 1 },
        { typeof(ElectionDifficultyChainUpdatePayload), 2 },
        { typeof(EuroPerEnergyChainUpdatePayload), 3 },
        { typeof(MicroCcdPerEuroChainUpdatePayload), 4 },
        { typeof(FoundationAccountChainUpdatePayload), 5 },
        { typeof(MintDistributionChainUpdatePayload), 6 },
        { typeof(TransactionFeeDistributionChainUpdatePayload), 7 },
        { typeof(GasRewardsChainUpdatePayload), 8 },
        { typeof(BakerStakeThresholdChainUpdatePayload), 9 },
        { typeof(RootKeysChainUpdatePayload), 10 },
        { typeof(Level1KeysChainUpdatePayload), 11 },
        { typeof(AddAnonymityRevokerChainUpdatePayload), 12 },
        { typeof(AddIdentityProviderChainUpdatePayload), 13 },
        { typeof(CooldownParametersChainUpdatePayload), 14 },
        { typeof(PoolParametersChainUpdatePayload), 15 },
        { typeof(TimeParametersChainUpdatePayload), 16 },
        { typeof(MintDistributionV1ChainUpdatePayload), 17 },
    };
        
    public ChainUpdatePayloadConverter() : base(SerializeMap)
    {
    }
}