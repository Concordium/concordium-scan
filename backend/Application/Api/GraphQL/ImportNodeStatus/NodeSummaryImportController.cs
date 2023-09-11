using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Network;
using Application.Import.NodeCollector;
using Application.Observability;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Application.Api.GraphQL.ImportNodeStatus;

public class NodeSummaryImportController : BackgroundService
{
    private readonly NodeCollectorClient _client;
    private readonly NodeStatusRepository _repository;
    private readonly ILogger _logger;

    public NodeSummaryImportController(NodeCollectorClient client, NodeStatusRepository repository)
    {
        _client = client;
        _repository = repository;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = TraceContext.StartActivity(nameof(NodeSummaryImportController));
        
        _logger.Information("Starting reading data from Concordium Collector backend...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var policyResult = await Policy
                .Handle<Exception>() 
                .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(5), (exception, span) => OnGetRetry(exception))
                .ExecuteAndCaptureAsync(_ => _client.GetNodeSummaries(stoppingToken), stoppingToken);

            if (policyResult.Outcome == OutcomeType.Successful)
            {
                _repository.AllNodeStatuses = policyResult.Result
                    .Select(MapToNodeStatus)
                    .OrderBy(x => x.NodeName)
                    .ToArray();
            }
            else if (_repository.AllNodeStatuses != null)
            {
                stoppingToken.ThrowIfCancellationRequested();

                _logger.Error("Did not succeed in getting node summaries from Concordium Collector backend within a reasonable time. Will reset cached node summaries now and then try again!.");
                _repository.AllNodeStatuses = Array.Empty<NodeStatus>();
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        
        _logger.Information("Stopped!");
    }
    
    private void OnGetRetry(Exception exception)
    {
        _logger.Warning("Error while getting node summaries from Concordium Collector backend. Will wait a while and then try again! [message={errorMessage}] [exception-type={exceptionType}]", exception.Message, exception.GetType());
    }
    
    private static NodeStatus MapToNodeStatus(NodeSummary arg)
    {
        return new NodeStatus(
            arg.NodeName, arg.NodeId, arg.PeerType, arg.Uptime, arg.Client, arg.AveragePing, arg.PeersCount,
            arg.PeersList, arg.BestBlock, arg.BestBlockHeight, arg.BestBlockBakerId, arg.BestArrivedTime,
            arg.BlockArrivePeriodEMA, arg.BlockArrivePeriodEMSD, arg.BlockArriveLatencyEMA, arg.BlockArriveLatencyEMSD,
            arg.BlockReceivePeriodEMA, arg.BlockReceivePeriodEMSD, arg.BlockReceiveLatencyEMA,
            arg.BlockReceiveLatencyEMSD, arg.FinalizedBlock, arg.FinalizedBlockHeight, arg.FinalizedTime,
            arg.FinalizationPeriodEMA, arg.FinalizationPeriodEMSD, arg.PacketsSent, arg.PacketsReceived,
            arg.ConsensusRunning, arg.BakingCommitteeMember, arg.ConsensusBakerId, arg.FinalizationCommitteeMember,
            arg.TransactionsPerBlockEMA, arg.TransactionsPerBlockEMSD, arg.BestBlockTransactionsSize,
            arg.BestBlockTotalEncryptedAmount, arg.BestBlockTotalAmount, arg.BestBlockTransactionCount,
            arg.BestBlockTransactionEnergyCost, arg.BestBlockExecutionCost, arg.BestBlockCentralBankAmount,
            arg.BlocksReceivedCount, arg.BlocksVerifiedCount, arg.GenesisBlock, arg.FinalizationCount,
            arg.FinalizedBlockParent, arg.AverageBytesPerSecondIn, arg.AverageBytesPerSecondOut);
    }
}