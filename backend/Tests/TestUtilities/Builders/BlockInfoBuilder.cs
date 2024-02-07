using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class BlockInfoBuilder
{
    private ulong _blockHeight = 1;
    private DateTimeOffset _blockSlotTime = new(2010, 10, 1, 12, 03, 52, 123, TimeSpan.Zero);
    private BlockHash _blockHash = BlockHash.From("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc");
    private BlockHash _blockLastFinalized = BlockHash.From("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d");
    private BakerId? _blockBaker = new BakerId(new AccountIndex(1));
    private bool _finalized = true;
    private uint _transactionCount = 2;
    private ProtocolVersion _protocolVersion = ProtocolVersion.P4;

    public BlockInfoBuilder WithBlockHeight(ulong value)
    {
        _blockHeight = value;
        return this;
    }

    public BlockInfo Build()
    {
        return new BlockInfo
        (
            BlockHash: _blockHash,    
            BlockParent: BlockHash.From("b6078154d6717e909ce0da4a45a25151b592824f31624b755900a74429e3073d"),    
            BlockLastFinalized: _blockLastFinalized,    
            BlockHeight: _blockHeight,
            GenesisIndex: 0,
            EraBlockHeight: 1,    
            BlockReceiveTime: new DateTimeOffset(2010, 10, 1, 12, 03, 54, 123, TimeSpan.Zero),
            BlockArriveTime: new DateTimeOffset(2010, 10, 1, 12, 03, 53, 123, TimeSpan.Zero),
            BlockSlot: 790511,
            BlockSlotTime: _blockSlotTime,
            BlockBaker: _blockBaker,
            Finalized: _finalized,
            TransactionCount: _transactionCount,
            TransactionEnergyCost: new EnergyAmount(4),
            TransactionSize: 42,
            BlockStateHash: new StateHash("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b"),
            ProtocolVersion: _protocolVersion,
            Round: null,
            Epoch: null
        );
    }
    
    public BlockInfoBuilder WithBlockLastFinalized(BlockHash lastFinalized)
    {
        _blockLastFinalized = lastFinalized;
        return this;
    }
    
    public BlockInfoBuilder WithProtocolVersion(ProtocolVersion version)
    {
        _protocolVersion = version;
        return this;
    }

    public BlockInfoBuilder WithBlockSlotTime(DateTimeOffset value)
    {
        _blockSlotTime = value;
        return this;
    }

    public BlockInfoBuilder WithBlockHash(BlockHash value)
    {
        _blockHash = value;
        return this;
    }

    public BlockInfoBuilder WithBlockBaker(BakerId? value)
    {
        _blockBaker = value;
        return this;
    }

    public BlockInfoBuilder WithFinalized(bool value)
    {
        _finalized = value;
        return this;
    }

    public BlockInfoBuilder WithTransactionCount(uint value)
    {
        _transactionCount = value;
        return this;
    }
}
