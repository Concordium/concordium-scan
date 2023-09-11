using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Observability;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Transactions;
using Application.Observability;
using Concordium.Sdk.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractAddress = Application.Api.GraphQL.ContractAddress;
using ContractEvent = Application.Aggregates.Contract.Entities.ContractEvent;
using ContractInitialized = Concordium.Sdk.Types.ContractInitialized;
using Transferred = Application.Api.GraphQL.Transactions.Transferred;

namespace Application.Aggregates.Contract;

internal sealed class ContractAggregate
{
    private readonly IContractRepositoryFactory _repositoryFactory;
    private readonly ContractAggregateOptions _options;
    private readonly ILogger _logger;
    private const string NodeImportJobActivity = "NodeImportJobActivity";
    private const string NodeImportJobLoopActivity = "NodeImportJobLoopActivity";

    public ContractAggregate(
        IContractRepositoryFactory repositoryFactory,
        ContractAggregateOptions options
            )
    {
        _repositoryFactory = repositoryFactory;
        _options = options;
        _logger = _logger = Log.ForContext<ContractAggregate>();
    }

    internal async Task NodeImportJob(IContractNodeClient client, CancellationToken token = default)
    {
        using var _ = TraceContext.StartActivity(NodeImportJobActivity);
        
        var retryCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var nextBlockHeightToImport = await GetNextBlockHeight();
                while (!token.IsCancellationRequested)
                {
                    var lastFinalizedHeight = await GetLastFinalizedBlockHeight(client, token);
                    if (nextBlockHeightToImport > lastFinalizedHeight)
                    {
                        await Task.Delay(_options.DelayBetweenRetries, token);
                        continue;
                    }

                    await NodeImportRange(client, nextBlockHeightToImport, lastFinalizedHeight, token);

                    nextBlockHeightToImport = lastFinalizedHeight + 1;
                    retryCount = 0;
                }
                break;
            }
            catch (Exception e)
            {
                if (retryCount == _options.RetryCount)
                {
                    throw;
                }
                _logger.Error(e, "Got exception running {Method} will try again with {RetryCount}", nameof(NodeImportJob), retryCount);
                retryCount += 1;
                await Task.Delay(_options.JobDelay, token);
            }
        }
    }

    /// <summary>
    /// For a given height fetch state from node and store relevant events.
    /// </summary>
    /// <returns>Count of transaction events mapped.</returns>
    internal async Task<uint> NodeImport(
        IContractRepository repository,
        IContractNodeClient client,
        ulong height,
        CancellationToken token = default
        )
    {
        var blockHashInput = new Absolute(height);
        var (blockHash, transactionEvents) = await client.GetBlockTransactionEvents(blockHashInput, token);
        _logger.Debug("Reading block {BlockHash}", blockHash.ToString());
        
        BlockInfo? blockInfo = null;
        var totalEvents = 0u;
        await foreach (var blockItemSummary in transactionEvents.WithCancellation(token))
        {
            if (blockItemSummary.Details is not AccountTransactionDetails details)
            {
                continue;
            }
            blockInfo ??= (await client.GetBlockInfoAsync(new Given(blockHash), token)).Response;

            var transactionHash = blockItemSummary.TransactionHash.ToString();
            var eventIndex = 0u;
            foreach (var transactionResultEvent in FilterEvents(details.Effects))
            {
                await StoreEvent(
                    ImportSource.NodeImport,
                    repository,
                    transactionResultEvent,
                    AccountAddress.From(details.Sender),
                    blockInfo.BlockHeight,
                    blockInfo.BlockSlotTime,
                    transactionHash,
                    blockItemSummary.Index,
                    eventIndex
                );
                eventIndex++;
            }

            totalEvents += eventIndex;
        }

        return totalEvents;
    }
    
    /// <summary>
    /// Stores successfully processed blocks. Will store from block height <see cref="heightFrom"/> to
    /// <see cref="heightTo"/> except for those given in list <see cref="except"/>
    /// </summary>
    internal static async Task SaveLastReadBlocks(
        IContractRepository repository,
        ulong heightFrom,
        ulong heightTo,
        ICollection<ulong> except,
        ImportSource source)
    {
        var heights = new List<ContractReadHeight>();
        for (var height = heightFrom; height <= heightTo; height++)
        {
            if (except.Contains(height))
            {
                continue;
            }
            heights.Add(new ContractReadHeight(height, source));
        }
        await repository.AddRangeAsync(heights);
    }

    internal static async Task StoreEvent(
        ImportSource source,
        IContractRepository repository,        
        TransactionResultEvent transactionResultEvent,
        AccountAddress sender,
        ulong blockHeight, 
        DateTimeOffset blockSlotTime,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex
    )
    {
        switch (transactionResultEvent)
        {
            case Api.GraphQL.Transactions.ContractInitialized contractInitialized:
                await repository.AddAsync(new Entities.Contract(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    eventIndex,
                    contractInitialized.ContractAddress,
                    sender,
                    source,
                    blockSlotTime
                ));
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ContractAddress,
                        contractInitialized,
                        source,
                        blockSlotTime
                    ));
                await repository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ModuleRef,
                        contractInitialized.ContractAddress,
                        source,
                        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added,
                        blockSlotTime
                    ));
                break;
            case ContractInterrupted contractInterrupted:
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInterrupted.ContractAddress,
                        contractInterrupted,
                        source,
                        blockSlotTime
                    ));
                break;
            case ContractResumed contractResumed:
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractResumed.ContractAddress,
                        contractResumed,
                        source,
                        blockSlotTime
                    ));
                break;
            case ContractUpdated contractUpdated:
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpdated.ContractAddress,
                        contractUpdated,
                        source,
                        blockSlotTime
                        ));
                if (contractUpdated.Instigator is ContractAddress contractInstigator && contractUpdated.Amount != 0)
                {
                    await repository
                        .AddAsync(new ContractEvent(
                            blockHeight,
                            transactionHash,
                            transactionIndex,
                            eventIndex,
                            contractInstigator,
                            new Transferred(
                                contractUpdated.Amount,
                                contractInstigator,
                                contractUpdated.ContractAddress
                            ),
                            source,
                            blockSlotTime
                        ));
                }
                break;
            case ContractUpgraded contractUpgraded:
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.ContractAddress,
                        contractUpgraded,
                        source,
                        blockSlotTime
                    ));
                await repository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.To,
                        contractUpgraded.ContractAddress,
                        source,
                        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added,
                        blockSlotTime
                    ));
                await repository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.From,
                        contractUpgraded.ContractAddress,
                        source,
                        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed,
                        blockSlotTime
                    ));
                break;
            case Transferred transferred:
                if (transferred.From is ContractAddress contractAddressFrom)
                {
                    await repository
                        .AddAsync(new ContractEvent(
                            blockHeight,
                            transactionHash,
                            transactionIndex,
                            eventIndex,
                            contractAddressFrom,
                            transferred,
                            source,
                            blockSlotTime
                        ));
                }
                if (transferred.To is ContractAddress contractAddressTo)
                {
                    await repository
                        .AddAsync(new ContractEvent(
                            blockHeight,
                            transactionHash,
                            transactionIndex,
                            eventIndex,
                            contractAddressTo,
                            transferred,
                            source,
                            blockSlotTime
                        ));
                }
                break;
            case ContractModuleDeployed contractModuleDeployed:
                await repository
                    .AddAsync(new ModuleReferenceEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractModuleDeployed.ModuleRef,
                        source,
                        blockSlotTime
                    ));
                break;
        }
    }
    
    private static async Task<ulong> GetLastFinalizedBlockHeight(IContractNodeClient client, CancellationToken token)
    {
        var consensusInfo = await client.GetConsensusInfoAsync(token);
        return consensusInfo.LastFinalizedBlockHeight;
    }

    private async Task NodeImportRange(IContractNodeClient client, ulong fromBlockHeight, ulong toBlockHeight, CancellationToken token)
    {
        for (var height = fromBlockHeight; height <= toBlockHeight; height++)
        {
            using var __ = TraceContext.StartActivity(NodeImportJobLoopActivity);
            using var durationMetric = new ContractMetrics.DurationMetric(ImportSource.NodeImport);
            try
            {
                await using var repository = await _repositoryFactory.CreateAsync();

                var affectedEvents = await NodeImport(repository, client, height, token);
                await repository.AddAsync(new ContractReadHeight(height, ImportSource.NodeImport));
                await repository.SaveChangesAsync(token);
                
                ContractMetrics.SetReadHeight(height, ImportSource.NodeImport);
                ContractMetrics.IncTransactionEvents(affectedEvents, ImportSource.NodeImport);
            }
            catch (Exception e)
            {
                durationMetric.SetException(e);
                throw;
            }
        }
    }
    
    private async Task<ulong> GetNextBlockHeight()
    {
        await using var repository = await _repositoryFactory.CreateAsync();
        var importState = await repository.GetReadOnlyLatestContractReadHeight();
        return importState != null ? importState.BlockHeight + 1 : 0;
    }

    private static IEnumerable<TransactionResultEvent> FilterEvents(IAccountTransactionEffects effect)
    {
        switch (effect)
        {
            case ContractInitialized contractInitialized:
                yield return Application.Api.GraphQL.Transactions.ContractInitialized.From(contractInitialized);
                break;
            case ContractUpdateIssued contractUpdateIssued:
                foreach (var transactionResultEvent in TransactionResultEvent.ToIter(contractUpdateIssued))
                {
                    yield return transactionResultEvent;
                }
                break;
            case ModuleDeployed moduleDeployed:
                yield return ContractModuleDeployed.From(moduleDeployed);
                break;
        }
    }
}
