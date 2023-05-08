using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Import.EventLogs;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

[UnionType("Address")]
public abstract class Address
{
    internal static Address from(BaseAddress fromAddress)
    {
        switch (fromAddress)
        {
            case CisEventAddressAccount account:
                return new AccountAddress(account.Address.AsString);
            case CisEventAddressContract contract:
                return new ContractAddress(contract.Index, contract.SubIndex);
            default:
                throw new ArgumentException($"Unknown address type {fromAddress.GetType()}");
        }
    }
}