using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BlockStatisticsBuilder
{
    private double _blockTime = 10.2d;

    public BlockStatistics Build()
    {
        return new BlockStatistics
        {
            BlockTime = _blockTime
        };
    }

    public BlockStatisticsBuilder WithBlockTime(double value)
    {
        _blockTime = value;
        return this;
    }
}