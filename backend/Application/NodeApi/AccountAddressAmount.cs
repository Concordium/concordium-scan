using Concordium.Sdk.Types;

namespace Application.NodeApi;

public class AccountAddressAmount
{
    public CcdAmount Amount { get; init; }
    public AccountAddress Address { get; init; }
}