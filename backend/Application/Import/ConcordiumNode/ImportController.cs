using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.Common.FeatureFlags;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Application.Import.ConcordiumNode;

public class ImportController : BackgroundService
{
    private readonly GrpcNodeClient _client;
    private readonly IFeatureFlags _featureFlags;
    private readonly ILogger _logger;
    private readonly ImportChannel _channel;

    public ImportController(GrpcNodeClient client, IFeatureFlags featureFlags, ImportChannel channel)
    {
        _client = client;
        _featureFlags = featureFlags;
        _channel = channel;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.IsEnabled("ccnode-import"))
        {
            _logger.Warning("Reading data from Concordium node is disabled. No data will be imported!");
            return;
        }

        try
        {
            _logger.Information("Awaiting initial import state...");
            var initialState = await _channel.GetInitialImportStateAsync();

            await Policy
                .Handle<RpcException>()
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), OnEnsurePreconditionRetry)
                .ExecuteAsync(() => EnsurePreconditions(initialState));

            await Policy
                .Handle<RpcException>()
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), OnImportDataRetry)
                .ExecuteAsync(() => ImportData(initialState, stoppingToken));
        }
        finally
        {
            _logger.Information("Stopped");
        } 
    }

    private async Task ImportData(ImportState initialState, CancellationToken stoppingToken)
    {
        _logger.Information("Starting reading data from Concordium node...");
        
        var importedMaxBlockHeight = initialState.MaxBlockHeight;
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

    private async Task ImportBatch(long startBlockHeight, long endBlockHeight, CancellationToken stoppingToken)
    {
        const int numberOfParallelTasks = 5;
        
        IEnumerable<long> range = new RangeOfLong(startBlockHeight, endBlockHeight);
        var hasMore = true;
        while (hasMore)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var blockHeights = range.Take(numberOfParallelTasks).ToArray();
            if (blockHeights.Length > 0)
            {
                var readTasks = blockHeights.Select(ReadBlockDataPayload);
                foreach (var readTask in readTasks)
                {
                    var payload = await readTask;
                    await _channel.Writer.WriteAsync(payload, stoppingToken);
                }
                
                range = range.Skip(numberOfParallelTasks);
            }
            else hasMore = false;
        }
    }

    private async Task<BlockDataPayload> ReadBlockDataPayload(long blockHeight)
    {
        var sw = Stopwatch.StartNew();

        var blocksAtHeight = await _client.GetBlocksAtHeightAsync((ulong)blockHeight);
        if (blocksAtHeight.Length != 1)
            throw new InvalidOperationException("Unexpected with more than one block at a given height."); 
        var blockHash = blocksAtHeight.Single();

        var blockInfoTask = _client.GetBlockInfoAsync(blockHash);
        var blockSummaryTask = _client.GetBlockSummaryAsync(blockHash);
        var rewardStatusTask = _client.GetRewardStatusAsync(blockHash);

        var blockInfo = await blockInfoTask;
        var blockSummary = await blockSummaryTask;
        var rewardStatus = await rewardStatusTask;

        var createdAccounts = await GetCreatedAccounts(blockInfo, blockSummary);

        BlockDataPayload payload;
        if (blockInfo.BlockHeight == 0)
        {
            var genesisIdentityProviders = await _client.GetIdentityProvidersAsync(blockHash);

            payload = new GenesisBlockDataPayload(blockInfo, blockSummary, createdAccounts, rewardStatus,
                genesisIdentityProviders);
        }
        else
        {
            payload = new BlockDataPayload(blockInfo, blockSummary, createdAccounts, rewardStatus);
        }

        var readDuration = sw.ElapsedMilliseconds;
        // _logger.Information("Data read for block {blockhash} at block height {blockheight} from node in {readDuration}ms",
        //     blockHash.AsString, blockHeight, readDuration);
        return payload;
    }

    private async Task<AccountInfo[]> GetCreatedAccounts(BlockInfo blockInfo, BlockSummary blockSummary)
    {
        if (blockInfo.BlockHeight == 0)
        {
            var accountList = await _client.GetAccountListAsync(blockInfo.BlockHash);
            var accountInfoTasks = accountList
                .Select(x => _client.GetAccountInfoAsync(x, blockInfo.BlockHash))
                .ToArray();
            
            return await Task.WhenAll(accountInfoTasks);
        }
        else
        {
            var accountsCreated = blockSummary.TransactionSummaries
                .Select(x => x.Result).OfType<TransactionSuccessResult>()
                .SelectMany(x => x.Events.OfType<AccountCreated>())
                .ToArray();

            if (accountsCreated.Length == 0) return Array.Empty<AccountInfo>();
            
            var accountInfoTasks = accountsCreated
                .Select(x => _client.GetAccountInfoAsync(x.Contents, blockInfo.BlockHash))
                .ToArray();
            return await Task.WhenAll(accountInfoTasks);
        }
    }

    private async Task EnsurePreconditions(ImportState initialState)
    {
        _logger.Information("Checking preconditions for importing data from Concordium node...");
        
        var importedGenesisBlockHash = initialState.GenesisBlockHash;
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
