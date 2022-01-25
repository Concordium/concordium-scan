using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BlockBuilder
{
    private long _id = 1;
    private int _blockHeight = 47;

    public Block Build()
    {
        return new Block
        {
            Id = _id,
            BlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1",
            BlockHeight = _blockHeight,
            BlockSlotTime = new DateTimeOffset(2010, 10, 10, 12, 0, 0, TimeSpan.Zero),
            BakerId = 7,
            Finalized = true,
            TransactionCount = 0,
            SpecialEvents = new SpecialEvents(),

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
}