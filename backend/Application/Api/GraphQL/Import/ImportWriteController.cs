using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import.Validations;
using Application.Common.Diagnostics;
using Application.Database;
using Application.Import;
using Application.Import.ConcordiumNode;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Api.GraphQL.Import;

public class ImportWriteController : BackgroundService
{
    private readonly ITopicEventSender _sender;
    private readonly ImportChannel _channel;
    private readonly ImportValidationController _accountBalanceValidator;
    private readonly IMetrics _metrics;
    private readonly MetricsListener _metricsListener;
    private readonly BlockWriter _blockWriter;
    private readonly IdentityProviderWriter _identityProviderWriter;
    private readonly ChainParametersWriter _chainParametersWriter;
    private readonly TransactionWriter _transactionWriter;
    private readonly AccountImportHandler _accountHandler;
    private readonly MetricsWriter _metricsWriter;
    private readonly ILogger _logger;
    private readonly ImportStateController _importStateController;

    public ImportWriteController(IDbContextFactory<GraphQlDbContext> dbContextFactory, DatabaseSettings dbSettings, 
        ITopicEventSender sender, ImportChannel channel, ImportValidationController accountBalanceValidator,
        IAccountLookup accountLookup, IMetrics metrics, MetricsListener metricsListener)
    {
        _sender = sender;
        _channel = channel;
        _accountBalanceValidator = accountBalanceValidator;
        _metrics = metrics;
        _metricsListener = metricsListener;
        _blockWriter = new BlockWriter(dbContextFactory, metrics);
        _identityProviderWriter = new IdentityProviderWriter(dbContextFactory);
        _chainParametersWriter = new ChainParametersWriter(dbContextFactory);
        _transactionWriter = new TransactionWriter(dbContextFactory);
        _accountHandler = new AccountImportHandler(dbContextFactory, accountLookup, metrics);
        _metricsWriter = new MetricsWriter(dbSettings, _metrics);
        _logger = Log.ForContext(GetType());
        _importStateController = new ImportStateController(dbContextFactory, metrics);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReadAndPublishInitialState();

            var waitCounter = _metrics.MeasureDuration(nameof(ImportReadController), "Wait");
            await foreach (var readFromNodeTask in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var envelope = await readFromNodeTask;
                waitCounter.Dispose();
                stoppingToken.ThrowIfCancellationRequested();
                
                var block = await WriteData(envelope.Payload);

                await _sender.SendAsync(nameof(Subscription.BlockAdded), block, stoppingToken);
                
                _logger.Information("Block {blockhash} at block height {blockheight} written", block.BlockHash, block.BlockHeight);
                
                await _accountBalanceValidator.PerformValidations(block);
                
                if (block.BlockHeight % 5000 == 0)
                    _metricsListener.DumpCapturedMetrics();
                
                waitCounter = _metrics.MeasureDuration(nameof(ImportReadController), "Wait");
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
        var state = await _importStateController.GetStateIfExists();
        _logger.Information("Initial import state read");

        var initialState = state != null
            ? new InitialImportState(state.MaxImportedBlockHeight, new BlockHash(state.GenesisBlockHash))
            : new InitialImportState(null, null);
        _channel.SetInitialImportState(initialState);
    }

    private async Task<Block> WriteData(BlockDataPayload payload)
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportReadController), nameof(WriteData));
        
        using var txScope = CreateTransactionScope();

        var importState = payload switch
        {
            GenesisBlockDataPayload genesisPayload => ImportState.CreateGenesisState(genesisPayload),
            _ => await _importStateController.GetState()
        };

        if (payload is GenesisBlockDataPayload genesisData)
            await HandleGenesisOnlyWrites(genesisData.GenesisIdentityProviders);

        var block = await HandleCommonWrites(payload, importState);

        importState.MaxImportedBlockHeight = payload.BlockInfo.BlockHeight;
        await _importStateController.SaveChanges(importState);
        
        using (_metrics.MeasureDuration(nameof(ImportWriteController), "commit"))
            txScope.Complete();

        _importStateController.SavedChangesCommitted();
        
        return block;
    }

    private async Task HandleGenesisOnlyWrites(IdentityProviderInfo[] identityProviders)
    {
        await _identityProviderWriter.AddGenesisIdentityProviders(identityProviders);
    }

    private async Task<Block> HandleCommonWrites(BlockDataPayload payload, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportWriteController), nameof(HandleCommonWrites));
        
        await _identityProviderWriter.AddOrUpdateIdentityProviders(payload.BlockSummary.TransactionSummaries);
        await _accountHandler.AddNewAccounts(payload.CreatedAccounts, payload.BlockInfo.BlockSlotTime);

        var chainParameters = await _chainParametersWriter.GetOrCreateChainParameters(payload.BlockSummary, importState);
        
        var block = await _blockWriter.AddBlock(payload.BlockInfo, payload.BlockSummary, payload.RewardStatus, chainParameters.Id, importState);
        var transactions = await _transactionWriter.AddTransactions(payload.BlockSummary, block.Id);

        await _accountHandler.HandleAccountUpdates(payload, transactions, block);

        await _blockWriter.UpdateTotalAmountLockedInReleaseSchedules(block);

        await _metricsWriter.AddBlockMetrics(block);
        await _metricsWriter.AddTransactionMetrics(payload.BlockInfo, payload.BlockSummary, importState);
        await _metricsWriter.AddAccountsMetrics(payload.BlockInfo, payload.CreatedAccounts, importState);

        var finalizationTimeUpdates = await _blockWriter.UpdateFinalizationTimeOnBlocksInFinalizationProof(block, importState);
        await _metricsWriter.UpdateFinalizationTimes(finalizationTimeUpdates);
        
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