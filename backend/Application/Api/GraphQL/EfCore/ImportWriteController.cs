using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Api.GraphQL.Validations;
using Application.Common;
using Application.Database;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Api.GraphQL.EfCore;

public class ImportWriteController : BackgroundService
{
    private readonly ITopicEventSender _sender;
    private readonly ImportChannel _channel;
    private readonly ImportValidationController _accountBalanceValidator;
    private readonly BlockWriter _blockWriter;
    private readonly IdentityProviderWriter _identityProviderWriter;
    private readonly ChainParametersWriter _chainParametersWriter;
    private readonly TransactionWriter _transactionWriter;
    private readonly AccountWriter _accountWriter;
    private readonly MetricsWriter _metricsWriter;
    private readonly ILogger _logger;
    private readonly ImportStateController _importStateController;

    public ImportWriteController(IDbContextFactory<GraphQlDbContext> dbContextFactory, DatabaseSettings dbSettings, 
        ITopicEventSender sender, ImportChannel channel, ImportValidationController accountBalanceValidator,
        IAccountLookup accountLookup)
    {
        _sender = sender;
        _channel = channel;
        _accountBalanceValidator = accountBalanceValidator;
        _blockWriter = new BlockWriter(dbContextFactory);
        _identityProviderWriter = new IdentityProviderWriter(dbContextFactory);
        _chainParametersWriter = new ChainParametersWriter(dbContextFactory);
        _transactionWriter = new TransactionWriter(dbContextFactory);
        _accountWriter = new AccountWriter(dbContextFactory, accountLookup);
        _metricsWriter = new MetricsWriter(dbSettings);
        _logger = Log.ForContext(GetType());
        _importStateController = new ImportStateController(dbContextFactory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReadAndPublishInitialState();

            var splitTime = new SplitTimeDiagnosticsLogger();
            splitTime.Start("wait");
            
            await foreach (var readFromNodeTask in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var envelope = await readFromNodeTask;
                stoppingToken.ThrowIfCancellationRequested();
                
                splitTime.Restart("write");
                var block = await WriteData(envelope.Payload);

                splitTime.Restart("notify");
                await _sender.SendAsync(nameof(Subscription.BlockAdded), block, stoppingToken);
                
                var measurements = splitTime.Stop();
                _logger.Information("Block {blockhash} at block height {blockheight} written [read: {readDuration:0}ms] " + measurements, 
                    block.BlockHash, block.BlockHeight, envelope.ReadDuration.TotalMilliseconds);
                
                await _accountBalanceValidator.PerformValidations(block);
                
                splitTime.Start("wait");
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
        await _identityProviderWriter.AddOrUpdateIdentityProviders(payload.BlockSummary.TransactionSummaries);
        await _accountWriter.AddAccounts(payload.CreatedAccounts, payload.BlockInfo.BlockSlotTime);

        var chainParameters = await _chainParametersWriter.GetOrCreateChainParameters(payload.BlockSummary, importState);
        
        var block = await _blockWriter.AddBlock(payload.BlockInfo, payload.BlockSummary, payload.RewardStatus, chainParameters.Id, importState);
        var transactions = await _transactionWriter.AddTransactions(payload.BlockSummary, block.Id);

        var accountTransactionRelations = await _accountWriter.AddAccountTransactionRelations(transactions);
        await _accountWriter.UpdateAccountBalances(payload.BlockSummary, block, accountTransactionRelations);
        await _accountWriter.AddAccountReleaseScheduleItems(transactions);

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