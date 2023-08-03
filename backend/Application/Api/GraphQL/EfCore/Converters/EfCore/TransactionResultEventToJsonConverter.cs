using System.Text.Json;
using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class TransactionResultEventToJsonConverter : ObjectToJsonConverter<TransactionResultEvent>
{
    private static readonly JsonSerializerOptions SerializerOptions;
    
    static TransactionResultEventToJsonConverter()
    {
        SerializerOptions = EfCoreJsonSerializerOptionsFactory.Create();
    }

    public TransactionResultEventToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}