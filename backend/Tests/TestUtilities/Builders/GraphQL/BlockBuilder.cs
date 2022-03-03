using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BlockBuilder
{
    private long _id = 0;
    private int _blockHeight = 47;
    private string _blockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";

    public Block Build()
    {
        return new Block
        {
            Id = _id,
            BlockHash = _blockHash,
            BlockHeight = _blockHeight,
            BlockSlotTime = new DateTimeOffset(2010, 10, 10, 12, 0, 0, TimeSpan.Zero),
            BakerId = 7,
            Finalized = true,
            TransactionCount = 0,
            SpecialEvents = new SpecialEvents(),
            BalanceStatistics = new BalanceStatistics(0, 0, 0, 0, 0, 0),
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
}