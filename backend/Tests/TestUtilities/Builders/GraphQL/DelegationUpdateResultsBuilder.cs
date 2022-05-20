using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class DelegationUpdateResultsBuilder
{
    private ulong _totalAmountStaked = 0;
    private DelegationTargetDelegatorCountDelta[] _delegatorCountDeltas = Array.Empty<DelegationTargetDelegatorCountDelta>();

    public DelegationUpdateResults Build()
    {
        return new DelegationUpdateResults(_totalAmountStaked, _delegatorCountDeltas);
    }

    public DelegationUpdateResultsBuilder WithTotalAmountStaked(ulong value)
    {
        _totalAmountStaked = value;
        return this;
    }
}