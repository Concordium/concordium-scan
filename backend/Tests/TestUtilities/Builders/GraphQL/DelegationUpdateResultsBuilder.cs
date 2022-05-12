using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class DelegationUpdateResultsBuilder
{
    private ulong _totalAmountStaked = 0;

    public DelegationUpdateResults Build()
    {
        return new DelegationUpdateResults(_totalAmountStaked);
    }

    public DelegationUpdateResultsBuilder WithTotalAmountStaked(ulong value)
    {
        _totalAmountStaked = value;
        return this;
    }
}