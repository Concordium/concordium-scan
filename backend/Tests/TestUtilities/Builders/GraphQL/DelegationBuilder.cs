using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders.GraphQL;

public class DelegationBuilder
{
    private bool _restakeEarnings = true;

    public Delegation Build()
    {
        return new Delegation(_restakeEarnings);
    }

    public DelegationBuilder WithRestakeEarnings(bool value)
    {
        _restakeEarnings = value;
        return this;
    }
}