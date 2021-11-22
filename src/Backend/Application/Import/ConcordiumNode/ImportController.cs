using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;

namespace Application.Import.ConcordiumNode;

public class ImportController : BackgroundService
{
    private readonly ConcordiumNodeGrpcClient _client;
    private readonly BlockRepository _repository;
    private readonly ILogger<ImportController> _logger;

    public ImportController(ConcordiumNodeGrpcClient client, BlockRepository repository, ILogger<ImportController> logger)
    {
        _client = client;
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting import from Concordium Node...");

        // Hardcoded start and end - will be removed later
        var startingBlockHeight = 0;
        var endHeight = 50000;

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
            // _repository.Insert(blockInfo, blockSummary);

            nextHeight++;
        }
        
        _logger.LogInformation("Import from Concordium Node stopped...");
    }
}