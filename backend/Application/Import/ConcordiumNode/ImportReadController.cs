﻿using System.Threading;
using System.Threading.Tasks;
using Application.Common.Diagnostics;
using Application.Configurations;
using Application.Database.MigrationJobs;
using Application.Jobs;
using Application.NodeApi;
using Application.Observability;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Serilog.Context;

namespace Application.Import.ConcordiumNode;

public class ImportReadController : BackgroundService
{
    private readonly ConcordiumClient _client;
    private readonly FeatureFlagOptions _featureFlags;
    private readonly ILogger _logger;
    private readonly ImportChannel _channel;
    private readonly IMetrics _metrics;
    private readonly IJobFinder<IMainMigrationJob> _migrationJobFinder;

    public ImportReadController(
        ConcordiumClient client,
        IOptions<FeatureFlagOptions> featureFlagsOptions,
        ImportChannel channel,
        IMetrics metrics,
        IJobFinder<IMainMigrationJob> migrationJobFinder
        )
    {
        _client = client;
        _featureFlags = featureFlagsOptions.Value;
        _channel = channel;
        _metrics = metrics;
        _migrationJobFinder = migrationJobFinder;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = TraceContext.StartActivity(nameof(ImportReadController));
        
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Warning("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }
        
        try
        {
            _logger.Information("Awaiting migration jobs.");
            await _migrationJobFinder.AwaitJobsAsync(stoppingToken);
            
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
            using var _ = TraceContext.StartActivity(nameof(ImportData));
            
            var consensusStatus = await GetWithGrpcRetryAsync(() => _client.GetConsensusInfoAsync(stoppingToken), nameof(_client.GetConsensusInfoAsync), stoppingToken);

            if (!importedMaxBlockHeight.HasValue || consensusStatus.LastFinalizedBlockHeight > importedMaxBlockHeight)
            {
                var startBlockHeight = importedMaxBlockHeight.HasValue ? importedMaxBlockHeight.Value + 1 : 0;
                await ImportBatch(startBlockHeight, consensusStatus, stoppingToken);
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

    private async Task ImportBatch(ulong startBlockHeight, ConsensusInfo consensusStatus, CancellationToken stoppingToken)
    {
        for (var blockHeight = startBlockHeight; blockHeight <= consensusStatus.LastFinalizedBlockHeight; blockHeight++)
        {
            using (LogContext.PushProperty("BlockHeight", blockHeight))
            {
                using var _ = TraceContext.StartActivity(nameof(ImportData));
            
                stoppingToken.ThrowIfCancellationRequested();
                var readFromNodeTask = ReadBlockDataPayload(blockHeight, consensusStatus, stoppingToken);
                await _channel.Writer.WriteAsync(readFromNodeTask, stoppingToken);   
            }
        }
    }

    private async Task<BlockDataEnvelope> ReadBlockDataPayload(ulong blockHeight, ConsensusInfo consensusStatus, CancellationToken stoppingToken)
    {
        var absoluteBlockHeight = new AbsoluteHeight(blockHeight);
        
        var blocksAtHeight = await GetWithGrpcRetryAsync(() => _client.GetBlocksAtHeightAsync(absoluteBlockHeight, stoppingToken), nameof(_client.GetBlocksAtHeightAsync), stoppingToken);
        if (blocksAtHeight.Count != 1)
            throw new InvalidOperationException("Unexpected with more than one block at a given height."); 
        var blockHash = blocksAtHeight.Single();
        var blockHashInput = new Given(blockHash);

        var blockInfoTask = GetWithGrpcRetryAsync(() => _client.GetBlockInfoAsync(blockHashInput, stoppingToken), nameof(_client.GetBlockInfoAsync), stoppingToken);
        var rewardStatusTask = GetWithGrpcRetryAsync(() => _client.GetTokenomicsInfoAsync(blockHashInput, stoppingToken), nameof(_client.GetTokenomicsInfoAsync), stoppingToken);
        var blockItemSummariesTask = GetWithGrpcRetryAsync(async () =>
        {
            var response = await _client.GetBlockTransactionEvents(blockHashInput, stoppingToken);
            return await response.Response.ToListAsync(stoppingToken);
        }, nameof(_client.GetBlockTransactionEvents), stoppingToken);
        var chainParametersTask = GetWithGrpcRetryAsync(() => _client.GetBlockChainParametersAsync(blockHashInput, stoppingToken), nameof(_client.GetBlockChainParametersAsync), stoppingToken);
        var specialEventsTask = GetWithGrpcRetryAsync(async () =>
        {
            var response = await _client.GetBlockSpecialEvents(blockHashInput, stoppingToken);
            return await response.Response.ToListAsync(stoppingToken);
        }, nameof(_client.GetBlockSpecialEvents), stoppingToken);
        

        await Task.WhenAll(blockInfoTask, rewardStatusTask, blockItemSummariesTask, chainParametersTask, specialEventsTask);
        var (_, blockInfo) = await blockInfoTask;
        var (_, rewardStatus) = await rewardStatusTask;
        var blockItemSummaries = await blockItemSummariesTask;
        var (_, chainParameters) = await chainParametersTask;
        var specialEvents = await specialEventsTask;

        var accountInfos = await GetWithGrpcRetryAsync(() => GetRelevantAccountInfosAsync(blockHashInput, blockItemSummaries, blockInfo, stoppingToken), nameof(GetRelevantAccountInfosAsync), stoppingToken);
        
        // Dont proactively read pool statuses for each block, we will let the write controller decide if they must be read!
        var bakerPoolStatusesFunc = () => GetWithGrpcRetryAsync(() => GetAllBakerPoolStatusesAsync(blockHashInput, blockInfo.ProtocolVersion, stoppingToken), nameof(GetAllBakerPoolStatusesAsync), stoppingToken);
        var passiveDelegationPoolStatusFunc = () => GetWithGrpcRetryAsync(() => GetPassiveDelegationPoolStatusAsync(blockHashInput, blockInfo.ProtocolVersion, stoppingToken), nameof(GetPassiveDelegationPoolStatusAsync), stoppingToken);
        
        BlockDataPayload payload;
        if (blockInfo.BlockHeight == 0)
        {
            var response = await GetWithGrpcRetryAsync(() => _client.GetIdentityProvidersAsync(blockHashInput, stoppingToken), nameof(_client.GetIdentityProvidersAsync), stoppingToken);
            var genesisIdentityProviders = await response.Response.ToListAsync(stoppingToken);
            payload = new GenesisBlockDataPayload(genesisIdentityProviders, blockInfo, blockItemSummaries, chainParameters, specialEvents, accountInfos, rewardStatus, bakerPoolStatusesFunc, passiveDelegationPoolStatusFunc);
        }
        else
        {
            payload = new BlockDataPayload(blockInfo, blockItemSummaries, chainParameters, specialEvents, accountInfos, rewardStatus, bakerPoolStatusesFunc, passiveDelegationPoolStatusFunc);
        }

        return new BlockDataEnvelope(payload, consensusStatus);
    }

    private async Task<PassiveDelegationStatus> GetPassiveDelegationPoolStatusAsync(IBlockHashInput blockHash, ProtocolVersion protocolVersion, CancellationToken stoppingToken)
    {
        if ((int)protocolVersion < 4)
            throw new InvalidOperationException("Cannot read baker pool statuses when protocol version is not 4 or greater.");
        
        var (_, poolStatus) = await _client.GetPassiveDelegationInfoAsync(blockHash, stoppingToken);
        if (poolStatus == null) throw new InvalidOperationException("Did not expect pool status for passive delegation to be null");
        return poolStatus!;
    }

    private async Task<BakerPoolStatus[]> GetAllBakerPoolStatusesAsync(IBlockHashInput blockHash, ProtocolVersion protocolVersion, CancellationToken stoppingToken)
    {
        if ((int)protocolVersion < 4)
            throw new InvalidOperationException("Cannot read baker pool statuses when protocol version is not 4 or greater.");
        
        var result = new List<BakerPoolStatus>();
        var (_, bakerIds) = await _client.GetBakerListAsync(blockHash, stoppingToken);
        await foreach (var bakerId in bakerIds.WithCancellation(stoppingToken))
        {
            var (_, bakerPoolStatus) = await _client.GetPoolInfoAsync(bakerId, blockHash, stoppingToken);
            if (bakerPoolStatus == null) throw new InvalidOperationException("Did not expect baker pool status to be null");
            result.Add(bakerPoolStatus);
        }
        return result.ToArray();
    }

    private async Task<AccountInfosRetrieved> GetRelevantAccountInfosAsync(IBlockHashInput blockHash, IList<BlockItemSummary> blockItemSummaries, BlockInfo blockInfo, CancellationToken stoppingToken)
    {
        if (blockInfo.BlockHeight == 0)
        {
            var accountList = await (await _client.GetAccountListAsync(blockHash, stoppingToken)).Response.ToListAsync(stoppingToken);
            var accountInfoTasks = accountList
                .Select(async x =>
                {
                    var response = await _client.GetAccountInfoAsync(x, blockHash, stoppingToken);
                    return response.Response;
                });

            var accountsCreated = await Task.WhenAll(accountInfoTasks);
            return new AccountInfosRetrieved(accountsCreated, Array.Empty<AccountInfo>());
        }
        else
        {
            var accountsToRead = blockItemSummaries
                .Where(b => b.IsSuccess())
                .Select(b => b.Details)
                .SelectMany(b => GetAccountAddressesToRetrieve(b, blockInfo.ProtocolVersion))
                .ToArray();

            if (accountsToRead.Length == 0)
                return new AccountInfosRetrieved(Array.Empty<AccountInfo>(), Array.Empty<AccountInfo>());
            
            var accountsCreatedTasks = accountsToRead.Where(x => x.Reason == typeof(AccountCreationDetails))
                .Select(async x =>
                {
                    var response = await _client.GetAccountInfoAsync(x.Address, blockHash, stoppingToken);
                    return response.Response;
                })
                .ToArray();
            var bakersWithNewPendingChangesTask = accountsToRead.Where(x => x.Reason == typeof(AccountBakerPendingChange))
                .Select(async x =>
                {
                    var response = await _client.GetAccountInfoAsync(x.Address, blockHash, stoppingToken);
                    return response.Response;
                })
                .ToArray();
            
            var accountsCreated = await Task.WhenAll(accountsCreatedTasks);
            var bakersWithNewPendingChanges = await Task.WhenAll(bakersWithNewPendingChangesTask);
            return new AccountInfosRetrieved(accountsCreated, bakersWithNewPendingChanges);
        }
    }

    private static IEnumerable<AccountAddressAndReason> GetAccountAddressesToRetrieve(IBlockItemSummaryDetails details, ProtocolVersion protocolVersion)
    {
        switch (details)
        {
            case AccountCreationDetails accountCreationDetails:
                yield return new AccountAddressAndReason(accountCreationDetails.Address, typeof(AccountCreationDetails));
                break;
            case AccountTransactionDetails accountTransactionDetails:
                switch (accountTransactionDetails.Effects)
                {
                    case BakerRemoved when (int)protocolVersion < 4:
                        yield return new AccountAddressAndReason(accountTransactionDetails.Sender, typeof(AccountBakerPendingChange));
                        break;
                    case BakerStakeUpdated bakerStakeUpdated when (int)protocolVersion < 4 && bakerStakeUpdated.Data is
                    {
                        Increased: false
                    }:
                        yield return new AccountAddressAndReason(accountTransactionDetails.Sender, typeof(AccountBakerPendingChange));
                        break;
                }
                if (accountTransactionDetails.Effects is not BakerConfigured bakerConfigured)
                {
                    break;
                }
                
                foreach (var bakerEvent in bakerConfigured.Data)
                {
                    switch (bakerEvent)
                    {
                        case BakerRemovedEvent:
                            yield return new AccountAddressAndReason(accountTransactionDetails.Sender, typeof(AccountBakerPendingChange));
                            break;
                        case BakerStakeDecreasedEvent:
                            yield return new AccountAddressAndReason(accountTransactionDetails.Sender, typeof(AccountBakerPendingChange));
                            break;
                    }
                }
                break;
            case UpdateDetails:
                break;
        }
    }

    private record AccountAddressAndReason(AccountAddress Address, Type Reason);
    
    private async Task EnsurePreconditions(InitialImportState initialState, CancellationToken stoppingToken)
    {
        _logger.Information("Checking preconditions for importing data from Concordium node...");
        
        var importedGenesisBlockHash = initialState.GenesisBlockHash;
        if (importedGenesisBlockHash != null)
        {
            var databaseNetworkId = ConcordiumNetworkId.TryGetFromGenesisBlockHash(importedGenesisBlockHash);
            if (databaseNetworkId != null)
                _logger.Information(
                    "Database contains genesis block hash '{genesisBlockHash}' indicating network is {concordiumNetwork}",
                    importedGenesisBlockHash.ToString(), databaseNetworkId.NetworkName);
            else
                _logger.Information(
                    "Database contains genesis block hash '{genesisBlockHash}' which is not one of the known Concordium networks.",
                    importedGenesisBlockHash.ToString());

            var consensusStatus = await GetWithGrpcRetryAsync(() => _client.GetConsensusInfoAsync(stoppingToken), "GetConsensusStatus", stoppingToken);
            var nodeNetworkId = ConcordiumNetworkId.TryGetFromGenesisBlockHash(consensusStatus.GenesisBlock);
            if (nodeNetworkId != null)
                _logger.Information("Concordium Node says genesis block hash is '{genesisBlockHash}' indicating network is {concordiumNetwork}", 
                    consensusStatus.GenesisBlock.ToString(), nodeNetworkId.NetworkName);
            else
                _logger.Information("Concordium Node says genesis block hash is '{genesisBlockHash}' which is not one of the known Concordium networks.", 
                    consensusStatus.GenesisBlock.ToString());

            if (consensusStatus.GenesisBlock != importedGenesisBlockHash)
                throw new InvalidOperationException("Genesis block hash of Concordium Node and Database are not identical.");
            _logger.Information("Genesis block hash of Concordium Node and Database are identical");
        }
        else
            _logger.Information("Database does not contain genesis blocks hash. No data was previously imported.");
    }

    private async Task<T> GetWithGrpcRetryAsync<T>(Func<Task<T>> func, string operationName, CancellationToken stoppingToken)
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportReadController), operationName);

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
