using System.Text.Json;
using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL.EfCore.Converters.EfCore;

namespace Application.Aggregates.Contract.Configurations;

public sealed class CisEventToJsonConverter : ObjectToJsonConverter<CisEvent>
{
    private static readonly JsonSerializerOptions SerializerOptions = EfCoreJsonSerializerOptionsFactory.Default;
    
    public CisEventToJsonConverter() : base(v => Serialize(v, SerializerOptions), v => Deserialize(v, SerializerOptions))
    {
    }
}
