using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerUpdateResultsBuilder
{
    private ulong _totalAmountStaked = 0;
    private int _bakersAddedCount = 0;
    private long[] _bakersRemoved = Array.Empty<long>();

    public BakerUpdateResultsBuilder WithTotalAmountStaked(ulong value)
    {
        _totalAmountStaked = value;
        return this;
    }

    public BakerUpdateResultsBuilder WithBakersAddedCount(int value)
    {
        _bakersAddedCount = value;
        return this;
    }

    public BakerUpdateResultsBuilder WithBakersRemovedCount(int value)
    {
        _bakersRemoved = new long[value];
        for (int i = 0; i < value; i++)
            _bakersRemoved[i] = i + 1;
        return this;
    }

    public BakerUpdateResults Build()
    {
        return new BakerUpdateResults(_totalAmountStaked, _bakersAddedCount, _bakersRemoved, Array.Empty<long>());
    }
}