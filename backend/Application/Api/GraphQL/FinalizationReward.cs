using HotChocolate;

namespace Application.Api.GraphQL;

public class FinalizationReward
{
    public ulong Amount { get; init; }
    public AccountAddress Address { get; init; }
    [GraphQLDeprecated("Use 'address.asString' instead. This field will be removed in the near future.")]
    public string AddressString => Address.AsString;
}