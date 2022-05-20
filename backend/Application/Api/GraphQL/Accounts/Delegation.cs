using HotChocolate;

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

    /// <summary>
    /// This property is intentionally not part of the GraphQL schema.
    /// Only here as a back reference to the owning block so that child data can be loaded.
    /// </summary>
    [GraphQLIgnore]
    public Account Owner { get; init; }
    public long DelegatorId => Owner.Id;
    public ulong StakedAmount { get; set; }
    public bool RestakeEarnings { get; set; }
    public DelegationTarget DelegationTarget { get; set; }
    public PendingDelegationChange? PendingChange { get; set; }
}
