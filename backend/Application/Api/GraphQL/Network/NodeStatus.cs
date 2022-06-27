using HotChocolate;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Network;

public record NodeStatus(
    string? NodeName,
    string NodeId,
    string PeerType,
    ulong Uptime,
    string ClientVersion,
    double? AveragePing,
    ulong PeersCount,
    [property: GraphQLIgnore] // Separate operation for getting a peers list with a reference to the peers status (if exists) 
    string[] PeersList,
    string BestBlock,
    ulong BestBlockHeight,
    ulong? BestBlockBakerId,
    DateTimeOffset? BestArrivedTime,
    double? BlockArrivePeriodEma,
    double? BlockArrivePeriodEmsd,
    double? BlockArriveLatencyEma,
    double? BlockArriveLatencyEmsd,
    double? BlockReceivePeriodEma,
    double? BlockReceivePeriodEmsd,
    double? BlockReceiveLatencyEma,
    double? BlockReceiveLatencyEmsd,
    string FinalizedBlock,
    ulong FinalizedBlockHeight,
    DateTimeOffset? FinalizedTime,
    double? FinalizationPeriodEma,
    double? FinalizationPeriodEmsd,
    ulong PacketsSent,
    ulong PacketsReceived,
    bool ConsensusRunning,
    string BakingCommitteeMember,
    ulong? ConsensusBakerId,
    bool FinalizationCommitteeMember,
    double? TransactionsPerBlockEma,
    double? TransactionsPerBlockEmsd,
    ulong? BestBlockTransactionsSize,
    ulong? BestBlockTotalEncryptedAmount,
    ulong? BestBlockTotalAmount,
    ulong? BestBlockTransactionCount,
    ulong? BestBlockTransactionEnergyCost,
    ulong? BestBlockExecutionCost,
    ulong? BestBlockCentralBankAmount,
    ulong? BlocksReceivedCount,
    ulong? BlocksVerifiedCount,
    string GenesisBlock,
    ulong? FinalizationCount,
    string FinalizedBlockParent,
    double AverageBytesPerSecondIn,
    double AverageBytesPerSecondOut)
{
    [ID]
    public string Id => NodeId;
    
    public IEnumerable<PeerReference> GetPeersList()
    {
        return PeersList.Select(x => new PeerReference(x));
    }
}
