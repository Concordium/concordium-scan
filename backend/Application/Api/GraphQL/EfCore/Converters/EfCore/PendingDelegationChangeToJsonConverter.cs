using System.Text.Json;
using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class PendingDelegationChangeToJsonConverter : ObjectToJsonConverter<PendingDelegationChange>
{
    private static readonly JsonSerializerOptions SerializerOptions;

    static PendingDelegationChangeToJsonConverter()
    {
        SerializerOptions = EfCoreJsonSerializerOptionsFactory.Create();
    }
    
    public PendingDelegationChangeToJsonConverter() : base(
        v => Serialize(v, SerializerOptions),
        v => Deserialize(v, SerializerOptions))
    {
    }
}