using System.Text.Json;
using Application.Api.GraphQL.EfCore.Converters.Json;
using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class TransactionRejectReasonToJsonConverter : ObjectToJsonConverter<TransactionRejectReason>
{
    private static readonly JsonSerializerOptions SerializerOptions;
    
    static TransactionRejectReasonToJsonConverter()
    {
        SerializerOptions = EfCoreJsonSerializerOptionsFactory.Create();
    }

    public TransactionRejectReasonToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}