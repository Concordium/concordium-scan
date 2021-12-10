using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class BlockInfoBuilder
{
    private int _blockHeight = 1;
    private DateTimeOffset _blockSlotTime = new(2010, 10, 1, 12, 03, 52, 123, TimeSpan.Zero);

    public BlockInfoBuilder WithBlockHeight(int value)
    {
        _blockHeight = value;
        return this;
    }

    public BlockInfo Build()
    {
        return new BlockInfo
        {
            BlockHash = new BlockHash("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc"),    
            BlockParent = new BlockHash("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d"),    
            BlockLastFinalized = new BlockHash("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d"),    
            BlockHeight = _blockHeight,
            GenesisIndex = 0,
            EraBlockHeight = 1,    
            BlockReceiveTime = new DateTimeOffset(2010, 10, 1, 12, 03, 54, 123, TimeSpan.Zero),
            BlockArriveTime = new DateTimeOffset(2010, 10, 1, 12, 03, 53, 123, TimeSpan.Zero),
            BlockSlot = 790511,
            BlockSlotTime = _blockSlotTime,
            BlockBaker = 5,
            Finalized = true,
            TransactionCount = 2,
            TransactionEnergyCost = 4,
            TransactionSize = 42,
            BlockStateHash = "42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b"
        };
    }

    public BlockInfoBuilder WithBlockSlotTime(DateTimeOffset value)
    {
        _blockSlotTime = value;
        return this;
    }
}