using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class DelegationTargetConverter : PolymorphicJsonConverter<DelegationTarget>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(LPoolDelegationTarget), 1 },
        { typeof(BakerDelegationTarget), 2 },
    };
        
    public DelegationTargetConverter() : base(SerializeMap)
    {
    }
}