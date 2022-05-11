namespace Application.Api.GraphQL.Accounts;

public class Delegation
{
    /// <summary>
    /// EF-core constructor!
    /// </summary>
    private Delegation() : this(0, false, null!) {}

    public Delegation(ulong stakedAmount, bool restakeEarnings, DelegationTarget delegationTarget)
    {
        StakedAmount = stakedAmount;
        RestakeEarnings = restakeEarnings;
        DelegationTarget = delegationTarget;
        PendingChange = null;
    }

    public ulong StakedAmount { get; set; } 
    public bool RestakeEarnings { get; set; }
    public DelegationTarget DelegationTarget { get; set; }
    public PendingDelegationChange? PendingChange { get; set; }
}
