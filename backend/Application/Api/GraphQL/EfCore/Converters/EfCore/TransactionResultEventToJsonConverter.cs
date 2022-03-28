using System.Text.Json;
using Application.Api.GraphQL.EfCore.Converters.Json;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class TransactionResultEventToJsonConverter : ObjectToJsonConverter<TransactionResultEvent>
{
    private static readonly JsonSerializerOptions SerializerOptions;
    
    static TransactionResultEventToJsonConverter()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            IgnoreReadOnlyProperties = true,
            Converters =
            {
                new TransactionResultEventConverter(),
                new AddressConverter(),
                new Json.AccountAddressConverter(),
                new ContractAddressConverter(),
                new ChainUpdatePayloadConverter()
            }
        };
    }

    public TransactionResultEventToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}