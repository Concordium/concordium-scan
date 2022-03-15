using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.FeatureFlags;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Application.Import.ConcordiumNode;

public class ImportReadController : BackgroundService
{
    private readonly GrpcNodeClient _client;
    private readonly IFeatureFlags _featureFlags;
    private readonly ILogger _logger;
    private readonly ImportChannel _channel;

    public ImportReadController(GrpcNodeClient client, IFeatureFlags featureFlags, ImportChannel channel)
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

            await EnsurePreconditions(initialState, stoppingToken);
            await ImportData(initialState, stoppingToken);
        }
        finally
        {
            _logger.Information("Stopped");
        } 
    }

    private async Task ImportData(InitialImportState initialState, CancellationToken stoppingToken)
    {
        _logger.Information("Starting reading data from Concordium node...");
        
        var importedMaxBlockHeight = initialState.MaxBlockHeight;
        while (!stoppingToken.IsCancellationRequested)
        {
            var consensusStatus = await GetWithGrpcRetryAsync(() => _client.GetConsensusStatusAsync(stoppingToken), "GetConsensusStatus", stoppingToken);

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
        for (var blockHeight = startBlockHeight; blockHeight <= endBlockHeight; blockHeight++)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var readFromNodeTask = ReadBlockDataPayload(blockHeight, stoppingToken);
            await _channel.Writer.WriteAsync(readFromNodeTask, stoppingToken);
        }
    }

    private async Task<BlockDataEnvelope> ReadBlockDataPayload(long blockHeight, CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();

        var blocksAtHeight = await GetWithGrpcRetryAsync(() => _client.GetBlocksAtHeightAsync((ulong)blockHeight, stoppingToken), "GetBlocksAtHeight", stoppingToken);
        if (blocksAtHeight.Length != 1)
            throw new InvalidOperationException("Unexpected with more than one block at a given height."); 
        var blockHash = blocksAtHeight.Single();

        var blockInfoTask = GetWithGrpcRetryAsync(() => _client.GetBlockInfoAsync(blockHash, stoppingToken), "GetBlockInfo", stoppingToken);
        var blockSummaryTask = GetWithGrpcRetryAsync(() => _client.GetBlockSummaryAsync(blockHash, stoppingToken), "GetBlockSummary", stoppingToken);
        var rewardStatusTask = GetWithGrpcRetryAsync(() => _client.GetRewardStatusAsync(blockHash, stoppingToken), "GetRewardStatus", stoppingToken);

        var blockInfo = await blockInfoTask;
        var blockSummary = await blockSummaryTask;
        var rewardStatus = await rewardStatusTask;

        var createdAccounts = await GetWithGrpcRetryAsync(() => GetCreatedAccountsAsync(blockInfo, blockSummary, stoppingToken), "GetCreatedAccounts", stoppingToken);

        BlockDataPayload payload;
        if (blockInfo.BlockHeight == 0)
        {
            var genesisIdentityProviders = await GetWithGrpcRetryAsync(() => _client.GetIdentityProvidersAsync(blockHash, stoppingToken), "GetIdentityProviders", stoppingToken);

            payload = new GenesisBlockDataPayload(blockInfo, blockSummary, createdAccounts, rewardStatus,
                genesisIdentityProviders);
        }
        else
        {
            payload = new BlockDataPayload(blockInfo, blockSummary, createdAccounts, rewardStatus);
        }

        var readDuration = sw.Elapsed;
        return new BlockDataEnvelope(payload, readDuration);
    }

    private async Task<AccountInfo[]> GetCreatedAccountsAsync(BlockInfo blockInfo, BlockSummary blockSummary, CancellationToken stoppingToken)
    {
        if (blockInfo.BlockHeight == 0)
        {
            var accountList = await _client.GetAccountListAsync(blockInfo.BlockHash, stoppingToken);
            var accountInfoTasks = accountList
                .Select(x => _client.GetAccountInfoAsync(x, blockInfo.BlockHash, stoppingToken))
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
                .Select(x => _client.GetAccountInfoAsync(x.Contents, blockInfo.BlockHash, stoppingToken))
                .ToArray();
            return await Task.WhenAll(accountInfoTasks);
        }
    }

    private async Task EnsurePreconditions(InitialImportState initialState, CancellationToken stoppingToken)
    {
        _logger.Information("Checking preconditions for importing data from Concordium node...");
        
        var importedGenesisBlockHash = initialState.GenesisBlockHash;
        if (importedGenesisBlockHash != null)
        {
            var databaseNetworkId = ConcordiumNetworkId.GetFromGenesisBlockHash(importedGenesisBlockHash);
            _logger.Information(
                "Database contains genesis block hash '{genesisBlockHash}' indicating network is {concordiumNetwork}",
                importedGenesisBlockHash.AsString, databaseNetworkId.NetworkName);

            var consensusStatus = await GetWithGrpcRetryAsync(() => _client.GetConsensusStatusAsync(stoppingToken), "GetConsensusStatus", stoppingToken);
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

    private async Task<T> GetWithGrpcRetryAsync<T>(Func<Task<T>> func, string operationName, CancellationToken stoppingToken)
    {
        var policyResult = await Policy
            .Handle<RpcException>(ex => ex.StatusCode != StatusCode.Cancelled) 
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), (exception, span) => OnGetRetry(exception, operationName))
            .ExecuteAndCaptureAsync(_ => func(), stoppingToken);

        if (policyResult.Outcome == OutcomeType.Successful)
            return policyResult.Result;
        
        stoppingToken.ThrowIfCancellationRequested();

        throw new ApplicationException($"An unexpected exception occurred during operation '{operationName}'", policyResult.FinalException);
    }

    private void OnGetRetry(Exception exception, string operationName)
    {
        var message = exception is RpcException rpcException ? $"{rpcException.Status.StatusCode}: {rpcException.Status.Detail}" : exception.Message;
        _logger.Error("Error while executing operation '{operationName}'. Will wait a while and then try again! [message={errorMessage}] [exception-type={exceptionType}]", operationName, message, exception.GetType());
    }
}
