using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Common;
using Application.Database;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Api.GraphQL.EfCore;

public class ImportWriteController : BackgroundService
{
    private readonly ImportStateReader _stateReader;
    private readonly ITopicEventSender _sender;
    private readonly ImportChannel _channel;
    private readonly BlockWriter _blockWriter;
    private readonly IdentityProviderWriter _identityProviderWriter;
    private readonly TransactionWriter _transactionWriter;
    private readonly AccountWriter _accountWriter;
    private readonly MetricsWriter _metricsWriter;
    private readonly ILogger _logger;

    private readonly MemoryCacheManager _cacheManager;

    public ImportWriteController(IDbContextFactory<GraphQlDbContext> dbContextFactory, DatabaseSettings dbSettings, ITopicEventSender sender, ImportChannel channel)
    {
        _cacheManager = new();

        _stateReader = new ImportStateReader(dbSettings);
        _sender = sender;
        _channel = channel;
        _blockWriter = new BlockWriter(dbContextFactory, _cacheManager.CreateCachedValue<DateTimeOffset>(), _cacheManager.CreateCachedValue<long>());
        _identityProviderWriter = new IdentityProviderWriter(dbContextFactory);
        _transactionWriter = new TransactionWriter(dbContextFactory);
        _accountWriter = new AccountWriter(dbContextFactory);
        _metricsWriter = new MetricsWriter(dbSettings, _cacheManager.CreateCachedValue<long>(), _cacheManager.CreateCachedValue<long>());
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReadAndPublishInitialState();

            await foreach (var readFromNodeTask in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var envelope = await readFromNodeTask;
                stoppingToken.ThrowIfCancellationRequested();
                
                var sw = Stopwatch.StartNew();
                var block = await WriteData(envelope.Payload);

                _logger.Information("Block {blockhash} at block height {blockheight} written [read: {readDuration:0}ms] [write: {writeDuration:0}ms]", 
                    block.BlockHash, block.BlockHeight, envelope.ReadDuration.TotalMilliseconds, sw.Elapsed.TotalMilliseconds);

                await _sender.SendAsync(nameof(Subscription.BlockAdded), block, stoppingToken);
            }
        }
        finally
        {
            _logger.Information("Stopped");
        }
    }

    private async Task ReadAndPublishInitialState()
    {
        _logger.Information("Reading initial import state...");
        var importState = await _stateReader.ReadImportStatus();
        _logger.Information("Initial import state read");

        _channel.SetInitialImportState(importState);
    }

    private async Task<Block> WriteData(BlockDataPayload payload)
    {
        using var txScope = CreateTransactionScope();

        if (payload is GenesisBlockDataPayload genesisData)
            await HandleGenesisOnlyWrites(genesisData.GenesisIdentityProviders);

        var block = await HandleCommonWrites(payload.BlockInfo, payload.BlockSummary, payload.RewardStatus, payload.CreatedAccounts);

        txScope.Complete();
        _cacheManager.CommitEnqueuedUpdates();
        
        return block;
    }

    private async Task HandleGenesisOnlyWrites(IdentityProviderInfo[] identityProviders)
    {
        await _identityProviderWriter.AddGenesisIdentityProviders(identityProviders);
    }

    private async Task<Block> HandleCommonWrites(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus,
        AccountInfo[] createdAccounts)
    {
        await _identityProviderWriter.AddOrUpdateIdentityProviders(blockSummary.TransactionSummaries);
        
        var block = await _blockWriter.AddBlock(blockInfo, blockSummary, rewardStatus);
        var transactions = await _transactionWriter.AddTransactions(blockSummary, block.Id);

        await _accountWriter.AddAccounts(createdAccounts, blockInfo.BlockSlotTime);

        await _accountWriter.AddAccountTransactionRelations(transactions);
        await _accountWriter.AddAccountReleaseScheduleItems(transactions);

        await _blockWriter.CalculateAndUpdateTotalAmountLockedInSchedules(block.Id, block.BlockSlotTime);

        await _metricsWriter.AddBlockMetrics(block);
        await _metricsWriter.AddTransactionMetrics(blockInfo, blockSummary);
        await _metricsWriter.AddAccountsMetrics(blockInfo, createdAccounts);

        await _blockWriter.UpdateFinalizationTimeOnBlocksInFinalizationProof(block);
        
        return block;
    }

    private static TransactionScope CreateTransactionScope()
    {
        return new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);
    }
}