using System.Threading;
using System.Threading.Tasks;
using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;
using Microsoft.Extensions.Hosting;

namespace Application.Import.ConcordiumNode;

public class ImportController : BackgroundService
{
    private readonly ConcordiumNodeGrpcClient _client;
    private readonly BlockRepository _repository;
    private readonly ILogger _logger;

    public ImportController(ConcordiumNodeGrpcClient client, BlockRepository repository)
    {
        _client = client;
        _repository = repository;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Starting import from Concordium Node...");

        var maxBlockHeight = _repository.GetMaxBlockHeight();
        var startingBlockHeight = maxBlockHeight.HasValue ? maxBlockHeight.Value + 1 : 0;
        var endHeight = startingBlockHeight + 10000; // Hardcoded end will be removed later

        _logger.Information("Starting at block height {height}", startingBlockHeight);
        
        var nextHeight = startingBlockHeight;
        var consensusStatus = await _client.GetConsensusStatusAsync();
        while (consensusStatus.BestBlockHeight >= nextHeight && nextHeight <= endHeight && !stoppingToken.IsCancellationRequested)
        {
            var blocksAtHeight = await _client.GetBlocksAtHeightAsync((ulong)nextHeight);
            if (blocksAtHeight.Length != 1)
                throw new InvalidOperationException("Unexpected with more than one block at a given height."); // TODO: consider how/if this should be handled
            var blockHash = blocksAtHeight.Single();
            
            var blockInfoTask = _client.GetBlockInfoAsync(blockHash);
            var blockSummaryTask = _client.GetBlockSummaryStringAsync(blockHash);

            var blockInfo = await blockInfoTask;
            var blockSummary = await blockSummaryTask;

            // TODO: Publish result - for now just write directly to db
            _repository.Insert(blockInfo, blockSummary);

            _logger.Information("Imported block {blockhash} at block height {blockheight}", blockHash.AsString, nextHeight);

            nextHeight++;
        }
        
        _logger.Information("Import from Concordium Node stopped...");
    }
}