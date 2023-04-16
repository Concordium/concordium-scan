using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import.EventLogs;
using Application.Api.GraphQL.Import.Validations;
using Application.Common;
using Application.Common.Diagnostics;
using Application.Common.FeatureFlags;
using Application.Database;
using Application.Import;
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
    private readonly EventLogHandler _eventLogHandler;
    private readonly NonCirculatingAccounts _nonCirculatingAccounts;
    private readonly BakerImportHandler _bakerHandler;
    private readonly MetricsWriter _metricsWriter;
    private readonly ILogger _logger;
    private readonly ImportStateController _importStateController;
    private readonly IFeatureFlags _featureFlags;
    private readonly IAccountLookup _accountLookup;
    private readonly MaterializedViewRefresher _materializedViewRefresher;
    private readonly DelegationImportHandler _delegationHandler;
    private readonly PassiveDelegationImportHandler _passiveDelegationHandler;
    private readonly PaydayImportHandler _paydayHandler;
    private readonly ModuleSchemaWriter _moduleSchemaWriter;

    public ImportWriteController(
        IDbContextFactory<GraphQlDbContext> dbContextFactory, 
        DatabaseSettings dbSettings,
        IFeatureFlags featureFlags, 
        ITopicEventSender sender, 
        ImportChannel channel, 
        ImportValidationController accountBalanceValidator,
        IAccountLookup accountLookup, 
        IMetrics metrics, 
        MetricsListener metricsListener,
        NonCirculatingAccounts nonCirculatingAccounts, 
        Modules.SmartContractModuleSerDe smartContractModuleSerDe)
    {
        _featureFlags = featureFlags;
        _accountLookup = accountLookup;
        _sender = sender;
        _channel = channel;
        _accountBalanceValidator = accountBalanceValidator;
        _metrics = metrics;
        _metricsListener = metricsListener;
        _blockWriter = new BlockWriter(dbContextFactory, metrics);
        _identityProviderWriter = new IdentityProviderWriter(dbContextFactory, metrics);
        _chainParametersWriter = new ChainParametersWriter(dbContextFactory, metrics);
        _transactionWriter = new TransactionWriter(dbContextFactory, metrics, smartContractModuleSerDe);
        var accountWriter = new AccountWriter(dbContextFactory, metrics);
        _accountHandler = new AccountImportHandler(accountLookup, metrics, accountWriter);
        _bakerHandler = new BakerImportHandler(dbContextFactory, metrics);
        _delegationHandler = new DelegationImportHandler(accountWriter);
        _metricsWriter = new MetricsWriter(dbSettings, _metrics);
        _logger = Log.ForContext(GetType());
        _importStateController = new ImportStateController(dbContextFactory, metrics);
        _materializedViewRefresher = new MaterializedViewRefresher(dbSettings, metrics);
        _passiveDelegationHandler = new PassiveDelegationImportHandler(dbContextFactory);
        _paydayHandler = new PaydayImportHandler(dbContextFactory, metrics);
        _eventLogHandler = new EventLogHandler(new EventLogWriter(dbContextFactory, accountLookup, metrics));
        _moduleSchemaWriter = new ModuleSchemaWriter(dbContextFactory);
        _nonCirculatingAccounts = nonCirculatingAccounts;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Warning("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }
        
        try
        {
            await ReadAndPublishInitialState();

            var waitCounter = _metrics.MeasureDuration(nameof(ImportWriteController), "Wait");
            await foreach (var readFromNodeTask in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var envelope = await readFromNodeTask;
                waitCounter.Dispose();
                stoppingToken.ThrowIfCancellationRequested();
                
                var result = await WriteData(envelope.Payload, envelope.ConsensusStatus, stoppingToken);
                await _materializedViewRefresher.RefreshAllIfNeeded(result);

                await _sender.SendAsync(nameof(Subscription.BlockAdded), result.Block, stoppingToken);
                
                _logger.Information(
                    "Block {blockhash} at block height {blockheight} written, time: {blockTime}", 
                    result.Block.BlockHash, 
                    result.Block.BlockHeight,
                    result.Block.BlockSlotTime.ToUniversalTime().ToString());
                
                await _accountBalanceValidator.PerformValidations(result.Block);
                
                if (result.Block.BlockHeight % 5000 == 0)
                    _metricsListener.DumpCapturedMetrics();
                
                waitCounter = _metrics.MeasureDuration(nameof(ImportWriteController), "Wait");
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

    private async Task<BlockWriteResult> WriteData(BlockDataPayload payload, ConsensusStatus consensusStatus, CancellationToken stoppingToken)
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportWriteController), nameof(WriteData));
        
        var txScope = CreateTransactionScope();

        BlockWriteResult result;
        try
        {
            var importState = payload switch
            {
                GenesisBlockDataPayload genesisPayload => ImportState.CreateGenesisState(
                    genesisPayload, 
                    consensusStatus.EpochDuration
                ),
                _ => await _importStateController.GetState()
            };

            if (payload is GenesisBlockDataPayload genesisData)
                await HandleGenesisOnlyWrites(genesisData);

            result = await HandleCommonWrites(payload, importState, stoppingToken);

            importState.MaxImportedBlockHeight = payload.BlockInfo.BlockHeight;
            importState.EpochDuration = consensusStatus.EpochDuration;
            await _importStateController.SaveChanges(importState);

            txScope.Complete();
        }
        finally
        {
            using (_metrics.MeasureDuration(nameof(ImportWriteController), "WriteDataDisposeTx"))
                txScope.Dispose(); // this is where the actual commit or rollback is performed
        }

        _importStateController.SavedChangesCommitted();
        return result;
    }

    private async Task HandleGenesisOnlyWrites(GenesisBlockDataPayload payload)
    {
        await _identityProviderWriter.AddGenesisIdentityProviders(payload.GenesisIdentityProviders);
    }

    private async Task<BlockWriteResult> HandleCommonWrites(BlockDataPayload payload, ImportState importState, CancellationToken stoppingToken)
    {
        using var counter = _metrics.MeasureDuration(nameof(ImportWriteController), nameof(HandleCommonWrites));
        
        var importPaydayStatus = await _paydayHandler.UpdatePaydayStatus(payload);
        await _identityProviderWriter.AddOrUpdateIdentityProviders(payload.BlockSummary.TransactionSummaries);
        await _accountHandler.AddNewAccounts(
            payload.AccountInfos.CreatedAccounts, 
            payload.BlockInfo.BlockSlotTime,
            payload.BlockInfo.BlockHeight);
        await _moduleSchemaWriter.AddModuleSchemas(payload.ModuleSchemas);

        var chainParameters = await _chainParametersWriter.GetOrCreateChainParameters(payload.BlockSummary, importState);

        var rewardsSummary = RewardsSummary.Create(payload.BlockSummary, _accountLookup);
        var bakerUpdateResults = await _bakerHandler.HandleBakerUpdates(payload, rewardsSummary, chainParameters, importPaydayStatus, importState);
        var delegationUpdateResults = await _delegationHandler.HandleDelegationUpdates(payload, chainParameters.Current, bakerUpdateResults, rewardsSummary, importPaydayStatus);
        await _bakerHandler.ApplyDelegationUpdates(payload, delegationUpdateResults, bakerUpdateResults, chainParameters.Current);

        var nonCirculatingAccountIds = _accountLookup
            .GetAccountIdsFromBaseAddresses(_nonCirculatingAccounts.accounts.Select(a => a.AsString))
            .AsEnumerable()
            .Select(kvp => kvp.Value)
            .Where(id => id.HasValue)
            .Select(id => (ulong)id.Value)
            .ToArray();

        var block = await _blockWriter.AddBlock(
            payload.BlockInfo, 
            payload.BlockSummary, 
            payload.RewardStatus, 
            chainParameters.Current.Id, 
            bakerUpdateResults, 
            delegationUpdateResults, 
            importState,
            nonCirculatingAccountIds);

        var specialEvents = await _blockWriter.AddSpecialEvents(block, payload.BlockSummary);
        var transactions = await _transactionWriter.AddTransactions(payload.BlockSummary, block.Id, block.BlockSlotTime);

        var passiveDelegationUpdateResults = await _passiveDelegationHandler.UpdatePassiveDelegation(delegationUpdateResults, payload, importState, importPaydayStatus, block);
        await _bakerHandler.ApplyChangesAfterBlocksAndTransactionsWritten(block, transactions, importPaydayStatus);
        var accountBalanceUpdates = _accountHandler.HandleAccountUpdates(payload, transactions, block);
        var accountTokenUpdates = _eventLogHandler.HandleLogs(transactions);

        var updatedAccountAddresses = accountTokenUpdates
            .Select(u => u.Address)
            .Concat(accountBalanceUpdates.Select(a => a.AccountAddress))
            .Distinct()
            .ToArray();

        foreach (var accntAddress in updatedAccountAddresses)
        {
            await _sender.SendAsync(
                accntAddress.AsString,
                new AccountsUpdatedSubscriptionItem(accntAddress),
                stoppingToken
            );
        }

        await _blockWriter.UpdateTotalAmountLockedInReleaseSchedules(block);
        var paydaySummary = await _paydayHandler.AddPaydaySummaryOnPayday(importPaydayStatus, block);
        
        await _metricsWriter.AddBlockMetrics(block);
        await _metricsWriter.AddTransactionMetrics(payload.BlockInfo, payload.BlockSummary, importState);
        await _metricsWriter.AddAccountsMetrics(payload.BlockInfo, payload.AccountInfos.CreatedAccounts, importState);
        await _metricsWriter.AddBakerMetrics(payload.BlockInfo.BlockSlotTime, bakerUpdateResults, importState);
        _metricsWriter.AddRewardMetrics(payload.BlockInfo.BlockSlotTime, rewardsSummary);
        _metricsWriter.AddPaydayPoolRewardMetrics(block, specialEvents, rewardsSummary, paydaySummary, bakerUpdateResults.PaydayPoolStakeSnapshot, passiveDelegationUpdateResults) ;
        
        var finalizationTimeUpdates = await _blockWriter.UpdateFinalizationTimeOnBlocksInFinalizationProof(block, importState);
        await _metricsWriter.UpdateFinalizationTimes(finalizationTimeUpdates);

        return new BlockWriteResult(block, importPaydayStatus);
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

public record BlockWriteResult(
    Block Block,
    BlockImportPaydayStatus PaydayStatus);
