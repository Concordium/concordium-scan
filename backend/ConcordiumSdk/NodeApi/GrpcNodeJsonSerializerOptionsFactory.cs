using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.NodeApi.Types.JsonConverters;
using ConcordiumSdk.Types.JsonConverters;

namespace ConcordiumSdk.NodeApi;

public static class GrpcNodeJsonSerializerOptionsFactory 
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new UnixTimeSecondsConverter(),
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new SpecialEventJsonConverter(),
                new BlockHashConverter(),
                new AddressConverter(),
                new AccountAddressConverter(),
                new ContractAddressConverter(),
                new TransactionHashConverter(),
                new CcdAmountConverter(),
                new NonceConverter(),
                new TransactionTypeConverter(),
                new TransactionResultConverter(),
                new TransactionResultEventConverter(),
                new TimestampedAmountConverter(),
                new RegisteredDataConverter(),
                new MemoConverter(),
                new ModuleRefConverter(),
                new BinaryDataConverter(),
                new UpdatePayloadConverter(),
                new RootUpdateConverter(),
                new Level1UpdateConverter(),
                new TransactionRejectReasonConverter(),
                new InvalidInitMethodConverter(),
                new InvalidReceiveMethodConverter(),
                new AmountTooLargeConverter(),
                new AccountBakerPendingChangeConverter(),
                new AccountDelegationPendingChangeConverter(),
                new BlockSummaryConverter(),
                new DelegationTargetConverter(),
                new RewardStatusConverter(),
                new BakerParametersConverter(),
                new LegacyBakerParametersConverter()
            }
        };
    }
}