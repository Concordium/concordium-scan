using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Application.Persistence;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Types;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Application.Import.ConcordiumNode;

public class ImportController : BackgroundService
{
    private readonly GrpcNodeClient _client;
    private readonly BlockRepository _repository;
    private readonly IFeatureFlags _featureFlags;
    private readonly DataUpdateController _dataUpdateController;
    private readonly ILogger _logger;

    public ImportController(GrpcNodeClient client, BlockRepository repository, IFeatureFlags featureFlags, DataUpdateController dataUpdateController)
    {
        _client = client;
        _repository = repository;
        _featureFlags = featureFlags;
        _dataUpdateController = dataUpdateController;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.IsEnabled("ccnode-import"))
        {
            _logger.Warning("Data import from Concordium node is disabled.");
            return;
        }
            
        _logger.Information("Execute started...");
        
        await Policy
            .Handle<RpcException>()
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), OnEnsurePreconditionRetry)
            .ExecuteAsync(EnsurePreconditions);

        await Policy
            .Handle<RpcException>()
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), OnImportDataRetry)
            .ExecuteAsync(() => ImportData(stoppingToken)); 

        _logger.Information("Executed finished!");
    }

    private async Task ImportData(CancellationToken stoppingToken)
    {
        _logger.Information("Starting data import...");
        
        var importedMaxBlockHeight = _repository.GetMaxBlockHeight();
        while (!stoppingToken.IsCancellationRequested)
        {
            var consensusStatus = await _client.GetConsensusStatusAsync();

            if (!importedMaxBlockHeight.HasValue || consensusStatus.LastFinalizedBlockHeight > importedMaxBlockHeight)
            {
                var startBlockHeight = importedMaxBlockHeight.HasValue ? importedMaxBlockHeight.Value + 1 : 0;
                await ImportBatch(startBlockHeight, consensusStatus.LastFinalizedBlockHeight, stoppingToken);
                importedMaxBlockHeight = consensusStatus.LastFinalizedBlockHeight;
            }

            else if (consensusStatus.LastFinalizedBlockHeight == importedMaxBlockHeight)
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);

            else
            {
                _logger.Warning(
                    "Looks like the Concordium node is catching up! Will wait a while and then check again... [NodeLastFinalizedBlockHeight: {lastFinalizedBlockHeight}] [DatabaseMaxImportedBlockHeight: {maxImportedBlockHeight}]",
                    consensusStatus.LastFinalizedBlockHeight, importedMaxBlockHeight);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
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
            var blockInfo = await blockInfoTask;

            var blockSummaryStringTask = _client.GetBlockSummaryStringAsync(blockHash);
            var blockSummaryString = await blockSummaryStringTask;

            var blockSummaryTask = _client.GetBlockSummaryAsync(blockHash);
            var blockSummary = await blockSummaryTask;

            // TODO: Publish result - for now just write directly to db
            _repository.Insert(blockInfo, blockSummaryString, blockSummary);
            await _dataUpdateController.BlockDataReceived(blockInfo, blockSummary);

            _logger.Information("Imported block {blockhash} at block height {blockheight}", blockHash.AsString, nextHeight);

            nextHeight++;
        }
    }

    private async Task EnsurePreconditions()
    {
        _logger.Information("Checking preconditions for importing data from Concordium node...");
        
        var importedGenesisBlockHash = _repository.GetGenesisBlockHash();
        if (importedGenesisBlockHash != null)
        {
            var databaseNetworkId = ConcordiumNetworkId.GetFromGenesisBlockHash(importedGenesisBlockHash);
            _logger.Information(
                "Database contains genesis block hash '{genesisBlockHash}' indicating network is {concordiumNetwork}",
                importedGenesisBlockHash.AsString, databaseNetworkId.NetworkName);

            var consensusStatus = await _client.GetConsensusStatusAsync();
            var nodeNetworkId = ConcordiumNetworkId.GetFromGenesisBlockHash(consensusStatus.GenesisBlock);
            _logger.Information("Concordium Node says genesis block hash is '{genesisBlockHash}' indicating network is {concordiumNetwork}", 
                consensusStatus.GenesisBlock.AsString, nodeNetworkId.NetworkName);

            if (consensusStatus.GenesisBlock != importedGenesisBlockHash)
                throw new InvalidOperationException("Genesis block hash of Concordium Node and Database are not identical.");
            _logger.Information("Genesis block hash of Concordium Node and Database are identical");
        }
        else
            _logger.Information("Database does not contain genesis blocks hash. No data was previously imported.");
    }

    private void OnEnsurePreconditionRetry(Exception exception, TimeSpan ts)
    {
        var message = exception is RpcException rpcException ? $"{rpcException.Status.StatusCode}: {rpcException.Status.Detail}" : exception.Message;
        _logger.Error("Error while ensuring preconditions for data import. Will retry shortly! [message={errorMessage}] [exception-type={exceptionType}]",  message, exception.GetType());
    }

    private void OnImportDataRetry(Exception exception, TimeSpan ts)
    {
        var message = exception is RpcException rpcException ? $"{rpcException.Status.StatusCode}: {rpcException.Status.Detail}" : exception.Message;
        _logger.Error("Error while importing data. Will wait a while and then start import again! [message={errorMessage}] [exception-type={exceptionType}]",  message, exception.GetType());
    }
}