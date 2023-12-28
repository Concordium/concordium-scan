using System.Text.Json;
using Application.Api.GraphQL.Tokens;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public sealed class CisEventDataToJsonConverter : ObjectToJsonConverter<CisEventData>
{
    private static readonly JsonSerializerOptions SerializerOptions = EfCoreJsonSerializerOptionsFactory.Default;
    
    public CisEventDataToJsonConverter() : base(v => Serialize(v, SerializerOptions), v => Deserialize(v, SerializerOptions))
    {
    }
}
