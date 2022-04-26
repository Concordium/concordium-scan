using Application.Api.GraphQL;
using Application.Api.GraphQL.Blocks;
using Google.Protobuf.WellKnownTypes;
using Tests.Api.GraphQL.Import;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BlockBuilder
{
    private long _id = 0;
    private int _blockHeight = 47;
    private string _blockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";
    private DateTimeOffset _blockSlotTime = new DateTimeOffset(2010, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private FinalizationSummary? _finalizationSummary = null;
    private BalanceStatistics _balanceStatistics = new BalanceStatisticsBuilder().Build();
    private BlockStatistics _blockStatistics = new BlockStatisticsBuilder().Build();

    public Block Build()
    {
        return new Block
        {
            Id = _id,
            BlockHash = _blockHash,
            BlockHeight = _blockHeight,
            BlockSlotTime = _blockSlotTime,
            BakerId = 7,
            Finalized = true,
            TransactionCount = 0,
            SpecialEventsOld = new SpecialEvents(),
            FinalizationSummary = _finalizationSummary,
            BalanceStatistics = _balanceStatistics,
            BlockStatistics = _blockStatistics
        };
    }

    public BlockBuilder WithId(long value)
    {
        _id = value;
        return this;
    }

    public BlockBuilder WithBlockHeight(int value)
    {
        _blockHeight = value;
        return this;
    }

    public BlockBuilder WithBlockHash(string value)
    {
        _blockHash = value;
        return this;
    }

    public BlockBuilder WithBlockSlotTime(DateTimeOffset value)
    {
        _blockSlotTime = value;
        return this;
    }

    public BlockBuilder WithFinalizationSummary(FinalizationSummary? value)
    {
        _finalizationSummary = value;
        return this;
    }

    public BlockBuilder WithBalanceStatistics(BalanceStatistics value)
    {
        _balanceStatistics = value;
        return this;
    }

    public BlockBuilder WithBlockStatistics(BlockStatistics value)
    {
        _blockStatistics = value;
        return this;
    }
}