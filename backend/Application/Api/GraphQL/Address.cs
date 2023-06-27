using Concordium.Sdk.Types;
using HotChocolate.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

namespace Application.Api.GraphQL;

[UnionType("Address")]
public abstract class Address
{
    internal static Address From(IAddress address) =>
        address switch
        {
            Concordium.Sdk.Types.AccountAddress x => AccountAddress.From(x),
            Concordium.Sdk.Types.ContractAddress x => ContractAddress.From(x),
            _ => throw new NotSupportedException("Cannot map this address type")
        };
}