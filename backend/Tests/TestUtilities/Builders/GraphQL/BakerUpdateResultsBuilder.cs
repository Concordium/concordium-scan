using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerUpdateResultsBuilder
{
    private ulong _totalAmountStaked = 0;
    private int _bakersAdded = 0;
    private int _bakersRemoved = 0;

    public BakerUpdateResultsBuilder WithTotalAmountStaked(ulong value)
    {
        _totalAmountStaked = value;
        return this;
    }

    public BakerUpdateResultsBuilder WithBakersAdded(int value)
    {
        _bakersAdded = value;
        return this;
    }

    public BakerUpdateResultsBuilder WithBakersRemoved(int value)
    {
        _bakersRemoved = value;
        return this;
    }

    public BakerUpdateResults Build()
    {
        return new BakerUpdateResults(_totalAmountStaked, _bakersAdded, _bakersRemoved);
    }
}