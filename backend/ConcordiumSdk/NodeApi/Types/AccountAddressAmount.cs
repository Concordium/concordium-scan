using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class AccountAddressAmount
{
    public CcdAmount Amount { get; init; }
    public AccountAddress Address { get; init; }
}