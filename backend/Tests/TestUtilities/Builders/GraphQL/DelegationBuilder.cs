using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders.GraphQL;

public class DelegationBuilder
{
    private bool _restakeEarnings = true;
    private PendingDelegationChange? _pendingChange = null;
    private DelegationTarget _delegationTarget = new PassiveDelegationTarget();
    private ulong _stakedAmount = 0;

    public Delegation Build()
    {
        var result = new Delegation(_stakedAmount, _restakeEarnings, _delegationTarget);
        result.PendingChange = _pendingChange;
        return result;
    }

    public DelegationBuilder WithRestakeEarnings(bool value)
    {
        _restakeEarnings = value;
        return this;
    }

    public DelegationBuilder WithStakedAmount(ulong value)
    {
        _stakedAmount = value;
        return this;
    }

    public DelegationBuilder WithPendingChange(PendingDelegationChange? value)
    {
        _pendingChange = value;
        return this;
    }

    public DelegationBuilder WithDelegationTarget(DelegationTarget value)
    {
        _delegationTarget = value;
        return this;
    }
}