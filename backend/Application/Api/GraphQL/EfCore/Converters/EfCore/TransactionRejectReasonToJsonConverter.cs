using System.Text.Json;
using Application.Api.GraphQL.EfCore.Converters.Json;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class TransactionRejectReasonToJsonConverter : ObjectToJsonConverter<TransactionRejectReason>
{
    private static readonly JsonSerializerOptions SerializerOptions;
    
    static TransactionRejectReasonToJsonConverter()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            IgnoreReadOnlyProperties = true,
            Converters =
            {
                new TransactionRejectReasonConverter(),
                new AddressConverter(),
                new Json.AccountAddressConverter(),
                new ContractAddressConverter()
            }
        };
    }

    public TransactionRejectReasonToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}