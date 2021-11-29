namespace Concordium.NodeApi;

/// <summary>
/// See https://github.com/Concordium/concordium-node/blob/main/docs/grpc.md#getconsensusstatus--consensusstatus
/// </summary>
public class ConsensusStatus
{
    // General
    public BlockHash BestBlock { get; init; }
    public BlockHash GenesisBlock { get; init; }
    public DateTimeOffset GenesisTime { get; init; }
    public BlockHash LastFinalizedBlock { get; init; }
    public int BestBlockHeight { get; init; }
    public int LastFinalizedBlockHeight { get; init; }
    public int ProtocolVersion { get; init; }
    public BlockHash CurrentEraGenesisBlock { get; init; }
    public DateTimeOffset CurrentEraGenesisTime { get; init; }
    public int SlotDuration { get; init; }
    public int EpochDuration { get; init; } // Not mentioned in docs
    public int GenesisIndex { get; init; }

    // Received block statistics
    public int BlocksReceivedCount { get; init; }
    public DateTimeOffset BlockLastReceivedTime { get; init; }
    public double BlockReceiveLatencyEma { get; init; }
    public double BlockReceiveLatencyEmsd { get; init; }
    public double? BlockReceivePeriodEma { get; init; }
    public double? BlockReceivePeriodEmsd { get; init; }
        
    // Verified block statistics
    public int BlocksVerifiedCount { get; init; }
    public DateTimeOffset? BlockLastArrivedTime { get; init; }
    public double BlockArriveLatencyEma { get; init; }
    public double BlockArriveLatencyEmsd { get; init; }
    public double? BlockArrivePeriodEma { get; init; }
    public double? BlockArrivePeriodEmsd { get; init; }
    public double TransactionsPerBlockEma { get; init; }
    public double TransactionsPerBlockEmsd { get; init; }

    // Finalization statistics
    public int FinalizationCount { get; init; }
    public DateTimeOffset? LastFinalizedTime { get; init; }
    public double? FinalizationPeriodEma { get; init; }
    public double? FinalizationPeriodEmsd { get; init; }
}