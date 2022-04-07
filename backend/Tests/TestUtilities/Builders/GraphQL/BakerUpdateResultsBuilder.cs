using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerUpdateResultsBuilder
{
    private ulong _totalAmountStaked = 0;

    public BakerUpdateResultsBuilder WithTotalAmountStaked(ulong value)
    {
        _totalAmountStaked = value;
        return this;
    }

    public BakerUpdateResults Build()
    {
        return new BakerUpdateResults(_totalAmountStaked, 0, 0);
    }
}