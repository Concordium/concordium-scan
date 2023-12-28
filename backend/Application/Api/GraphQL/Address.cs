using Application.Api.GraphQL.Import.EventLogs;
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
            Concordium.Sdk.Types.AccountAddress accountAddress => AccountAddress.From(accountAddress),
            Concordium.Sdk.Types.ContractAddress contractAddress => ContractAddress.From(contractAddress),
            _ => throw new NotSupportedException("Cannot map this address type")
        };
    
    internal static Address From(BaseAddress fromAddress)
    {
        return fromAddress switch
        {
            CisEventAddressAccount account => new AccountAddress(account.Address.ToString()),
            CisEventAddressContract contract => new ContractAddress(contract.Index, contract.SubIndex),
            _ => throw new ArgumentException($"Unknown address type {fromAddress.GetType()}")
        };
    }    
}
