using Application.Api.GraphQL.Bakers;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class PendingBakerChangeConverter : PolymorphicJsonConverter<PendingBakerChange>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(PendingBakerRemoval), 1 },
        { typeof(PendingBakerReduceStake), 2 },
    };

    public PendingBakerChangeConverter() : base(SerializeMap)
    {
    }
}