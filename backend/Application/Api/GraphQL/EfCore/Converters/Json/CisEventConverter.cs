using Application.Api.GraphQL.Import.EventLogs;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

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
