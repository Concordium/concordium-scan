using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders.GraphQL;

public class DelegationBuilder
{
    private bool _restakeEarnings = true;
    private PendingDelegationChange? _pendingChange = null;

    public Delegation Build()
    {
        var result = new Delegation(_restakeEarnings);
        result.PendingChange = _pendingChange;
        return result;
    }

    public DelegationBuilder WithRestakeEarnings(bool value)
    {
        _restakeEarnings = value;
        return this;
    }

    public DelegationBuilder WithPendingChange(PendingDelegationChange? value)
    {
        _pendingChange = value;
        return this;
    }
}