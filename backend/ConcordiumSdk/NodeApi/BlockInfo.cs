using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

public class BlockInfo
{
    public BlockHash BlockHash { get; init; }    
    public BlockHash BlockParent { get; init; }    
    public BlockHash BlockLastFinalized { get; init; }    
    public int BlockHeight { get; init; }    
    public int GenesisIndex { get; init; }    
    public int EraBlockHeight { get; init; }    
    public DateTimeOffset BlockReceiveTime { get; init; }    
    public DateTimeOffset BlockArriveTime { get; init; }
    public int BlockSlot { get; init; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public int? BlockBaker { get; init; }
    public bool Finalized { get; init; }
    public int TransactionCount { get; init; }
    public int TransactionEnergyCost { get; init; }
    public int TransactionSize { get; init; }
    public string BlockStateHash { get; init; }
}