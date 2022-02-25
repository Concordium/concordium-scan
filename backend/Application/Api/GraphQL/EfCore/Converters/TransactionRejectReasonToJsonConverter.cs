using System.Text.Json;
using Application.Api.GraphQL.JsonConverters;

namespace Application.Api.GraphQL.EfCore.Converters;

public class TransactionRejectReasonToJsonConverter : ObjectToJsonConverter<TransactionRejectReason>
{
    private static readonly JsonSerializerOptions SerializerOptions;
    
    static TransactionRejectReasonToJsonConverter()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new TransactionRejectReasonConverter(),
                new AddressConverter(),
                new AccountAddressConverter(),
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