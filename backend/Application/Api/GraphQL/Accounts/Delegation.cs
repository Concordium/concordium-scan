namespace Application.Api.GraphQL.Accounts;

public class Delegation
{
    /// <summary>
    /// EF-core constructor!
    /// </summary>
    private Delegation() : this(false, null!) {}

    public Delegation(bool restakeEarnings, DelegationTarget delegationTarget)
    {
        RestakeEarnings = restakeEarnings;
        DelegationTarget = delegationTarget;
        PendingChange = null;
    }

    public bool RestakeEarnings { get; set; }
    public DelegationTarget DelegationTarget { get; set; }
    public PendingDelegationChange? PendingChange { get; set; }
}
