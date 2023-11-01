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
using InvalidInitMethod = Application.Api.GraphQL.Transactions.InvalidInitMethod;
using InvalidReceiveMethod = Application.Api.GraphQL.Transactions.InvalidReceiveMethod;
using ModuleHashAlreadyExists = Application.Api.GraphQL.Transactions.ModuleHashAlreadyExists;
using RejectedReceive = Application.Api.GraphQL.Transactions.RejectedReceive;
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
        _logger.Debug("Reading block {BlockHash}, at height {BlockHeight}", blockHash.ToString(), height);
        
        await using var moduleReadonlyRepository = await _repositoryFactory.CreateModuleReadonlyRepository();
        BlockInfo? blockInfo = null;
        var totalEvents = 0u;
        await foreach (var blockItemSummary in transactionEvents.WithCancellation(token))
        {
            if (blockItemSummary.Details is not AccountTransactionDetails details)
            {
                continue;
            }
            blockInfo ??= (await client.GetBlockInfoAsync(new Given(blockHash), token)).Response;

            totalEvents += await StoreEvents(repository, moduleReadonlyRepository, client, blockInfo, details, blockItemSummary);
            totalEvents += await StorePossibleRejectEvent(repository, moduleReadonlyRepository, blockInfo, details, blockItemSummary);
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

    internal static async Task StoreReject(
        ImportSource source,
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        TransactionRejectReason rejectEvent,
        AccountAddress sender,
        ulong blockHeight,
        DateTimeOffset blockSlotTime,
        string transactionHash,
        ulong transactionIndex
    )
    {
        switch (rejectEvent)
        {
            case InvalidInitMethod invalidInitMethod:
                await contractRepository.AddAsync(new ModuleReferenceRejectEvent(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    invalidInitMethod.ModuleRef,
                    sender,
                    rejectEvent,
                    source,
                    blockSlotTime
                ));
                break;
            case InvalidReceiveMethod invalidReceiveMethod:
                await contractRepository.AddAsync(new ModuleReferenceRejectEvent(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    invalidReceiveMethod.ModuleRef,
                    sender,
                    rejectEvent,
                    source,
                    blockSlotTime
                ));
                break;
            case ModuleHashAlreadyExists moduleHashAlreadyExists:
                await contractRepository.AddAsync(new ModuleReferenceRejectEvent(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    moduleHashAlreadyExists.ModuleRef,
                    sender,
                    rejectEvent,
                    source,
                    blockSlotTime
                ));
                break;
            case RejectedReceive rejectedReceive:
                var rejectedReceiveEvent = await rejectedReceive
                    .TryUpdateMessage(moduleReadonlyRepository, blockHeight, transactionIndex);
                if (rejectedReceiveEvent != null)
                {
                    rejectedReceive = rejectedReceiveEvent;
                }
                await contractRepository.AddAsync(new ContractRejectEvent(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    rejectedReceive.ContractAddress,
                    sender,
                    rejectEvent,
                    source,
                    blockSlotTime
                ));
                break;
        }
    }
    
    /// <summary>
    /// Store a event and returns the latest <see cref="eventIndex"/> given to a event.
    /// </summary>
    internal static async Task<uint> StoreEvent(
        ImportSource source,
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        IContractNodeClient client,
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
                var contractInitializedEvent = await contractInitialized.TryUpdateWithParsedEvents(moduleReadonlyRepository);
                if (contractInitializedEvent != null)
                {
                    contractInitialized = contractInitializedEvent;
                }
                await contractRepository.AddAsync(new Entities.Contract(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    eventIndex,
                    contractInitialized.ContractAddress,
                    sender,
                    source,
                    blockSlotTime
                ));
                await contractRepository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ContractAddress,
                        sender,
                        contractInitialized,
                        source,
                        blockSlotTime
                    ));
                await contractRepository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ModuleRef,
                        contractInitialized.ContractAddress,
                        sender,
                        source,
                        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added,
                        blockSlotTime
                    ));
                break;
            case ContractInterrupted contractInterrupted:
                var contractInterruptedEvent = await contractInterrupted
                    .TryUpdateWithParsedEvents(contractRepository, moduleReadonlyRepository, blockHeight, transactionIndex, eventIndex);
                if (contractInterruptedEvent != null)
                {
                    contractInterrupted = contractInterruptedEvent;
                }
                await contractRepository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInterrupted.ContractAddress,
                        sender,
                        contractInterrupted,
                        source,
                        blockSlotTime
                    ));
                break;
            case ContractResumed contractResumed:
                await contractRepository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractResumed.ContractAddress,
                        sender,
                        contractResumed,
                        source,
                        blockSlotTime
                    ));
                break;
            case ContractUpdated contractUpdated:
                var contractUpdatedEvent = await contractUpdated
                    .TryUpdate(moduleReadonlyRepository, blockHeight, transactionIndex, eventIndex);
                if (contractUpdatedEvent != null)
                {
                    contractUpdated = contractUpdatedEvent;
                }
                if (contractUpdated.Instigator is ContractAddress contractInstigator)
                {
                    await contractRepository
                        .AddAsync(new ContractEvent(
                            blockHeight,
                            transactionHash,
                            transactionIndex,
                            eventIndex,
                            contractInstigator,
                            sender,
                            new ContractCall(
                                contractUpdated
                            ),
                            source,
                            blockSlotTime
                        ));
                    // Possible a contract has called itself.
                    eventIndex += 1;
                }
                await contractRepository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpdated.ContractAddress,
                        sender,
                        contractUpdated,
                        source,
                        blockSlotTime
                        ));
                break;
            case ContractUpgraded contractUpgraded:
                await contractRepository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.ContractAddress,
                        sender,
                        contractUpgraded,
                        source,
                        blockSlotTime
                    ));
                await contractRepository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.To,
                        contractUpgraded.ContractAddress,
                        sender,
                        source,
                        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added,
                        blockSlotTime
                    ));
                await contractRepository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.From,
                        contractUpgraded.ContractAddress,
                        sender,
                        source,
                        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed,
                        blockSlotTime
                    ));
                break;
            case Transferred transferred:
                if (transferred.From is ContractAddress contractAddressFrom)
                {
                    await contractRepository
                        .AddAsync(new ContractEvent(
                            blockHeight,
                            transactionHash,
                            transactionIndex,
                            eventIndex,
                            contractAddressFrom,
                            sender,
                            transferred,
                            source,
                            blockSlotTime
                        ));
                }
                break;
            case ContractModuleDeployed contractModuleDeployed:
                var moduleReferenceEvent = await ModuleReferenceEvent.Create(new ModuleReferenceEventInfo(blockHeight,
                    transactionHash,
                    transactionIndex,
                    eventIndex,
                    contractModuleDeployed.ModuleRef,
                    sender,
                    source,
                    blockSlotTime), client);
                await contractRepository
                    .AddAsync(moduleReferenceEvent);
                break;
        }

        return eventIndex;
    }

    /// <summary>
    /// Store rejected events if present in block item summary and if related to contract- or modules.
    ///
    /// Return 1 if a rejected event was created.
    /// </summary>
    private static async Task<uint> StorePossibleRejectEvent(
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        BlockInfo blockInfo, 
        AccountTransactionDetails details,
        BlockItemSummary blockItemSummary
    )
    {
        if (details.Effects is not None none || !TryGetRelevantReject(none.RejectReason, out var rejectReason))
        {
            return 0;
        }
        
        await StoreReject(
            ImportSource.NodeImport,
            contractRepository,
            moduleReadonlyRepository,
            rejectReason!,
            AccountAddress.From(details.Sender),
            blockInfo.BlockHeight,
            blockInfo.BlockSlotTime,
            blockItemSummary.TransactionHash.ToString(),
            blockItemSummary.Index
        );

        return 1;
    }

    /// <summary>
    /// Map relevant rejected reasons to <see cref="TransactionRejectReason"/>. Returns `false` if the rejected
    /// event wasn't relevant for the aggregate.
    /// </summary>
    private static bool TryGetRelevantReject(IRejectReason rejectReason, out TransactionRejectReason? rejected)
    {
        rejected = null;
        switch (rejectReason)
        {
            case Concordium.Sdk.Types.InvalidInitMethod x:
                rejected = new InvalidInitMethod(x.ModuleReference.ToString(), x.ContractName.Name);
                return true;
            case Concordium.Sdk.Types.InvalidReceiveMethod x:
                rejected = new InvalidReceiveMethod(x.ModuleReference.ToString(), x.ReceiveName.Receive);
                return true;
            case Concordium.Sdk.Types.ModuleHashAlreadyExists x:
                rejected = new ModuleHashAlreadyExists(x.ModuleReference.ToString());
                return true;
            case Concordium.Sdk.Types.RejectedReceive x:
                rejected = new RejectedReceive(x.RejectReason, 
                    ContractAddress.From(x.ContractAddress), x.ReceiveName.Receive, x.Parameter.ToHexString());
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Store related events and return mapped events count.
    /// </summary>
    private static async Task<uint> StoreEvents(
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        IContractNodeClient client,
        BlockInfo blockInfo, 
        AccountTransactionDetails details,
        BlockItemSummary blockItemSummary
        )
    {
        var transactionHash = blockItemSummary.TransactionHash.ToString();
        var eventIndex = 0u;
        foreach (var transactionResultEvent in FilterEvents(details.Effects))
        {
            eventIndex = await StoreEvent(
                ImportSource.NodeImport,
                contractRepository,
                moduleReadonlyRepository,
                client,
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

        return eventIndex;
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
                await using var repository = await _repositoryFactory.CreateContractRepositoryAsync();

                var affectedEvents = await NodeImport(repository, client, height, token);
                await repository.AddAsync(new ContractReadHeight(height, ImportSource.NodeImport));
                await repository.SaveChangesAsync(token);
                _logger.Information("Block Height: {BlockHeight} has been processed from node.", height);
                
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
        await using var repository = await _repositoryFactory.CreateContractRepositoryAsync();
        var importState = await repository.GetReadonlyLatestContractReadHeight();
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
