using System.Threading;
using System.Threading.Tasks;
using Application.Persistence;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Microsoft.Extensions.Hosting;

namespace Application.Import.ConcordiumNode;

public class ImportController : BackgroundService
{
    private readonly GrpcNodeClient _client;
    private readonly BlockRepository _repository;
    private readonly ILogger _logger;

    public ImportController(GrpcNodeClient client, BlockRepository repository)
    {
        _client = client;
        _repository = repository;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Starting import from Concordium Node...");

        var consensusStatus = await _client.GetConsensusStatusAsync();
        var importedMaxBlockHeight = _repository.GetMaxBlockHeight();
        var importedGenesisBlockHash = _repository.GetGenesisBlockHash();
        EnsurePreconditions(consensusStatus, importedMaxBlockHeight, importedGenesisBlockHash);

        var importStartBlockHeight = importedMaxBlockHeight.HasValue ? importedMaxBlockHeight.Value + 1 : 0;
        _logger.Information("Starting import at block height {height}", importStartBlockHeight);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!importedMaxBlockHeight.HasValue || consensusStatus.LastFinalizedBlockHeight > importedMaxBlockHeight)
            {
                await ImportBatch(importStartBlockHeight, consensusStatus.LastFinalizedBlockHeight, stoppingToken);
                
                importedMaxBlockHeight = consensusStatus.LastFinalizedBlockHeight;
                importStartBlockHeight = importedMaxBlockHeight.Value + 1;
            }

            else if (consensusStatus.LastFinalizedBlockHeight == importedMaxBlockHeight)
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            
            else
            {
                _logger.Warning("Looks like the Concordium node is catching up, will wait a while and then check again... [NodeLastFinalizedBlockHeight: {lastFinalizedBlockHeight}] [DatabaseMaxImportedBlockHeight: {maxImportedBlockHeight}]", consensusStatus.LastFinalizedBlockHeight, importedMaxBlockHeight);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            consensusStatus = await _client.GetConsensusStatusAsync();
        }

        _logger.Information("Import from Concordium Node stopped...");
    }

    private async Task ImportBatch(int startBlockHeight, int endBlockHeight, CancellationToken stoppingToken)
    {
        var nextHeight = startBlockHeight;
        while (endBlockHeight >= nextHeight && !stoppingToken.IsCancellationRequested)
        {
            var blocksAtHeight = await _client.GetBlocksAtHeightAsync((ulong)nextHeight);
            if (blocksAtHeight.Length != 1)
                throw new InvalidOperationException("Unexpected with more than one block at a given height."); // TODO: consider how/if this should be handled
            var blockHash = blocksAtHeight.Single();

            var blockInfoTask = _client.GetBlockInfoAsync(blockHash);
            var blockSummaryStringTask = _client.GetBlockSummaryStringAsync(blockHash);
            var blockSummaryTask = _client.GetBlockSummaryAsync(blockHash);

            var blockInfo = await blockInfoTask;
            var blockSummaryString = await blockSummaryStringTask;
            var blockSummary = await blockSummaryTask;

            // TODO: Publish result - for now just write directly to db
            _repository.Insert(blockInfo, blockSummaryString, blockSummary);

            _logger.Information("Imported block {blockhash} at block height {blockheight}", blockHash.AsString, nextHeight);

            nextHeight++;
        }
    }

    private void EnsurePreconditions(ConsensusStatus consensusStatus, int? importedMaxBlockHeight, BlockHash? importedGenesisBlockHash)
    {
        var nodeNetworkId = ConcordiumNetworkId.GetFromGenesisBlockHash(consensusStatus.GenesisBlock);
        _logger.Information("Consensus status read from Concordium Node. Genesis block hash is '{genesisBlockHash}' indicating network is {concordiumNetwork}", consensusStatus.GenesisBlock.AsString, nodeNetworkId.NetworkName);

        if (importedMaxBlockHeight.HasValue != importedGenesisBlockHash.HasValue)
            throw new InvalidOperationException("Null-state of both state values from database must match!");
        
        if (importedMaxBlockHeight.HasValue && importedGenesisBlockHash.HasValue)
        {
            var databaseNetworkId = ConcordiumNetworkId.GetFromGenesisBlockHash(importedGenesisBlockHash.Value);
            _logger.Information(
                "Database import state read. Max block height is '{maxBlockHeight}' and genesis block hash is '{genesisBlockHash}' indicating network is {concordiumNetwork}",
                importedMaxBlockHeight, importedGenesisBlockHash.Value.AsString, databaseNetworkId.NetworkName);

            if (consensusStatus.GenesisBlock != importedGenesisBlockHash)
                throw new InvalidOperationException("Genesis block hash of Concordium Node and Database are not identical.");
            _logger.Information("Genesis block hash of Concordium Node and Database are identical");
        }
        else
            _logger.Information("Import status from database read. No data was previously imported.");
    }
}