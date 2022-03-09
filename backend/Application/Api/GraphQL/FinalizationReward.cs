using HotChocolate;

namespace Application.Api.GraphQL;

public class FinalizationReward
{
    public ulong Amount { get; init; }
    [GraphQLDeprecated("Use 'addressString' instead. Type of this field will be changed to AccountAddress in the near future.")]
    public string Address { get; init; }
    public string AddressString => Address;
}