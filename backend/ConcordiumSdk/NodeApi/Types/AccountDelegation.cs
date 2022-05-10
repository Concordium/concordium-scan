using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class AccountDelegation
{
    public bool RestakeEarnings { get; init; }
    public CcdAmount StakedAmount { get; init; }
    public DelegationTarget DelegationTarget { get; init; }
    public AccountDelegationPendingChange? PendingChange { get; init; }
}