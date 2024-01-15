using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL.EfCore.Converters.Json;

namespace Application.Aggregates.Contract.Configurations;

public sealed class CisEventConverter : PolymorphicJsonConverter<CisEvent>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(CisBurnEvent), 1 },
        { typeof(CisTokenMetadataEvent), 2 },
        { typeof(CisMintEvent), 3 },
        { typeof(CisTransferEvent), 4 }
    };
    
    public CisEventConverter() : base(SerializeMap)
    {
    }
}
