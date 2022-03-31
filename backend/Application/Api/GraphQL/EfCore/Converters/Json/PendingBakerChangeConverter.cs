namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class PendingBakerChangeConverter : PolymorphicJsonConverter<PendingBakerChange>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(PendingBakerRemoval), 1 },
    };

    public PendingBakerChangeConverter() : base(SerializeMap)
    {
    }
}