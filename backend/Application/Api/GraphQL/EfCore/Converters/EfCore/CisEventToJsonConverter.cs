using System.Text.Json;
using Application.Api.GraphQL.Import.EventLogs;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public sealed class CisEventToJsonConverter : ObjectToJsonConverter<CisEvent>
{
    private static readonly JsonSerializerOptions SerializerOptions = EfCoreJsonSerializerOptionsFactory.Default;
    
    public CisEventToJsonConverter() : base(v => Serialize(v, SerializerOptions), v => Deserialize(v, SerializerOptions))
    {
    }
}
