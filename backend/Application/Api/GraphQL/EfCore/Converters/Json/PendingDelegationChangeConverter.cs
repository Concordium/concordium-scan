using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class PendingDelegationChangeConverter : PolymorphicJsonConverter<PendingDelegationChange>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(PendingDelegationRemoval), 1 },
        { typeof(PendingDelegationReduceStake), 2 },
    };

    public PendingDelegationChangeConverter() : base(SerializeMap)
    {
    }
}