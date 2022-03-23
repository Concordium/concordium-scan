namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class AddressConverter : PolymorphicJsonConverter<Address>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(AccountAddress), 1 },
        { typeof(ContractAddress), 2 },
    };

    public AddressConverter() : base(SerializeMap)
    {
    }
}