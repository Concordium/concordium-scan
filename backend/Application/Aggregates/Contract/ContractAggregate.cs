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

            totalEvents += await StoreEvents(repository, blockInfo, details, blockItemSummary);
            totalEvents += await StorePossibleRejectEvent(repository, blockInfo, details, blockItemSummary);
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
        IContractRepository repository,
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
                await repository.AddAsync(new ModuleReferenceRejectEvent(
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
                await repository.AddAsync(new ModuleReferenceRejectEvent(
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
                await repository.AddAsync(new ModuleReferenceRejectEvent(
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
                await repository.AddAsync(new ContractRejectEvent(
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
                        sender,
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
                        sender,
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
                        sender,
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
                        sender,
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
                        sender,
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
                            sender,
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
                        sender,
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
                        sender,
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
                        sender,
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
                            sender,
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
                            sender,
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
                        sender,
                        source,
                        blockSlotTime
                    ));
                break;
        }
    }

    /// <summary>
    /// Store rejected events if present in block item summary and if related to contract- or modules.
    ///
    /// Return 1 if a rejected event was created.
    /// </summary>
    private static async Task<uint> StorePossibleRejectEvent(
        IContractRepository repository, 
        BlockInfo blockInfo, 
        AccountTransactionDetails details,
        BlockItemSummary blockItemSummary
    )
    {
        if (details.Effects is not None none || !IsRelevantReject(none.RejectReason, out var rejectReason))
        {
            return 0;
        }
        
        await StoreReject(
            ImportSource.NodeImport,
            repository,
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
    /// Map relevant rejected reasons to <see cref="TransactionRejectReason"/>.
    /// </summary>
    private static bool IsRelevantReject(IRejectReason rejectReason, out TransactionRejectReason? rejected)
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
                rejected =new RejectedReceive(x.RejectReason,
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
        IContractRepository repository, 
        BlockInfo blockInfo, 
        AccountTransactionDetails details,
        BlockItemSummary blockItemSummary
        )
    {
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
