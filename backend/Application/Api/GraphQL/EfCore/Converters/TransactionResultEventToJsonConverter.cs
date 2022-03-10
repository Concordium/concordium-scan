using System.Text.Json;
using Application.Api.GraphQL.JsonConverters;

namespace Application.Api.GraphQL.EfCore.Converters;

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
                new Application.Api.GraphQL.JsonConverters.AccountAddressConverter(),
                new ContractAddressConverter()
            }
        };
    }

    public TransactionResultEventToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}