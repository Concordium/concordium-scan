using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Transactions;
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
        var retryCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var lastHeight = await GetNextBlockHeight();
                while (!token.IsCancellationRequested)
                {
                    var consensusInfo = await client.GetConsensusInfoAsync(token);
                    var newLastHeight = consensusInfo.LastFinalizedBlockHeight;
                    if (lastHeight == newLastHeight)
                    {
                        await Task.Delay(_options.DelayBetweenRetries, token);
                        continue;
                    }

                    for (var height = lastHeight; height <= newLastHeight; height++)
                    {
                        await using var repository = await _repositoryFactory.CreateAsync();
                
                        await NodeImport(repository, client, height, token);
                        await SaveLastReadBlock(repository, height, ImportSource.NodeImport);
                        await repository.SaveChangesAsync(token);
                    }
                    lastHeight = newLastHeight;
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

    internal async Task NodeImport(
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
                    transactionHash,
                    blockItemSummary.Index,
                    eventIndex
                );
                eventIndex++;
            }
        }
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
                    source
                ));
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ContractAddress,
                        contractInitialized,
                        source
                    ));
                await repository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ModuleRef,
                        contractInitialized.ContractAddress,
                        source
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
                        source
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
                        source
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
                        source
                    ));
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
                        source
                    ));
                await repository
                    .AddAsync(new ModuleReferenceContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.To,
                        contractUpgraded.ContractAddress,
                        source
                    ));
                break;
            case Transferred transferred:
                if (transferred.From is not ContractAddress contractAddress ||
                    transferred.To is not AccountAddress)
                {
                    break;
                }
                await repository
                    .AddAsync(new ContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractAddress,
                        transferred,
                        source
                    ));
                break;
            case ContractModuleDeployed contractModuleDeployed:
                await repository
                    .AddAsync(new ModuleReferenceEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractModuleDeployed.ModuleRef,
                        source
                    ));
                break;
        }
    }
    
    private async Task<ulong> GetNextBlockHeight()
    {
        await using var repository = await _repositoryFactory.CreateAsync();
        var importState = await repository.GetReadOnlyLatestContractReadHeight();
        return importState != null ? importState.BlockHeight + 1 : 0;
    }

    private static Task SaveLastReadBlock(
        IContractRepository repository,
        ulong blockHeight,
        ImportSource source
    )
    {
        return repository
            .AddAsync(new ContractReadHeight(blockHeight, source));
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