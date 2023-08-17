using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ContractAddress = Application.Api.GraphQL.ContractAddress;
using ContractInitialized = Concordium.Sdk.Types.ContractInitialized;
using Transferred = Application.Api.GraphQL.Transactions.Transferred;

namespace Application.Aggregates.SmartContract;

internal sealed class SmartContractAggregate
{
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ISmartContractNodeClient _client;
    private readonly ILogger _logger;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

    public SmartContractAggregate(
        ISmartContractRepositoryFactory repositoryFactory,
        ISmartContractNodeClient client
    )
    {
        _repositoryFactory = repositoryFactory;
        _client = client;
        _logger = _logger = Log.ForContext<SmartContractAggregate>();
    }

    internal async Task Import(CancellationToken token = default)
    {
        // TODO - add some resilience
        var lastHeight = await GetLastReadBlockHeight();
        while (!token.IsCancellationRequested)
        {
            var consensusInfo = await _client.GetConsensusInfoAsync(token);
            var newLastHeight = consensusInfo.LastFinalizedBlockHeight;
            if (lastHeight == newLastHeight)
            {
                await Task.Delay(_delay, token);
                continue;
            }

            for (var height = lastHeight; height <= newLastHeight; height++)
            {
                await using var repository = await _repositoryFactory.CreateAsync();
                
                var absolute = new Absolute(height);
                await GetTransactionEvents(repository, absolute, token);
                await SaveLastReadBlock(repository, height);
                await repository.SaveChangesAsync(token);
            }
            lastHeight = newLastHeight;
        }
    }

    private async Task GetTransactionEvents(
        ISmartContractRepository repository,
        IBlockHashInput blockHashInput,
        CancellationToken token = default
        )
    {
        var (blockHash, transactionEvents) = await _client.GetBlockTransactionEvents(blockHashInput, token);
        _logger.Debug("Reading {@BlockHash}", blockHash);
        
        BlockInfo? blockInfo = null;
        await foreach (var blockItemSummary in transactionEvents.WithCancellation(token))
        {
            if (blockItemSummary.Details is not AccountTransactionDetails details)
            {
                continue;
            }
            blockInfo ??= (await _client.GetBlockInfoAsync(new Given(blockHash), token)).Response;

            var transactionHash = blockItemSummary.TransactionHash.ToString();

            var eventIndex = 0u;
            foreach (var transactionResultEvent in FilterEvents(details.Effects))
            {
                await StoreEvent(
                    repository,
                    transactionResultEvent,
                    blockInfo.BlockHeight,
                    transactionHash,
                    blockItemSummary.Index,
                    eventIndex
                );
                eventIndex++;
            }
        }
    }
    
    private async Task StoreEvent(
        ISmartContractRepository repository,        
        TransactionResultEvent transactionResultEvent,
        ulong blockHeight, 
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex
    )
    {
        switch (transactionResultEvent)
        {
            case Api.GraphQL.Transactions.ContractInitialized contractInitialized:
                await repository
                    .AddAsync(new SmartContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ContractAddress,
                        contractInitialized
                    ));
                await repository
                    .AddAsync(new ModuleReferenceSmartContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInitialized.ModuleRef,
                        contractInitialized.ContractAddress
                    ));
                break;
            case ContractInterrupted contractInterrupted:
                await repository
                    .AddAsync(new SmartContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractInterrupted.ContractAddress,
                        contractInterrupted
                    ));
                break;
            case ContractResumed contractResumed:
                await repository
                    .AddAsync(new SmartContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractResumed.ContractAddress,
                        contractResumed
                    ));
                break;
            case ContractUpdated contractUpdated:
                await repository
                    .AddAsync(new SmartContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpdated.ContractAddress,
                        contractUpdated
                    ));
                break;
            case ContractUpgraded contractUpgraded:
                await repository
                    .AddAsync(new SmartContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.ContractAddress,
                        contractUpgraded
                    ));
                await repository
                    .AddAsync(new ModuleReferenceSmartContractLinkEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractUpgraded.To,
                        contractUpgraded.ContractAddress
                    ));
                break;
            case Transferred transferred:
                if (transferred.From is not ContractAddress contractAddress ||
                    transferred.To is not Api.GraphQL.Accounts.AccountAddress)
                {
                    _logger.Error("Error when map transfer got {@From} and {@To} in transaction {TransactionHash}",
                        transferred.From, transferred.To, transactionHash);
                    break;
                    // TODO Throw exception? Somehow this is just a swallow...
                }
                await repository
                    .AddAsync(new SmartContractEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractAddress,
                        transferred
                    ));
                break;
            case ContractModuleDeployed contractModuleDeployed:
                await repository
                    .AddAsync(new ModuleReferenceEvent(
                        blockHeight,
                        transactionHash,
                        transactionIndex,
                        eventIndex,
                        contractModuleDeployed.ModuleRef
                    ));
                break;
        }
    }
    
    private async Task<ulong> GetLastReadBlockHeight()
    {
        await using var repository = await _repositoryFactory.CreateAsync();
        var importState = repository.GetReadOnlyQueryable<SmartContractReadHeight>()
            .OrderByDescending(x => x.BlockHeight)
            .FirstOrDefault();
        return importState?.BlockHeight ?? 0;
    }

    private static ValueTask<EntityEntry<SmartContractReadHeight>> SaveLastReadBlock(
        ISmartContractRepository repository,
        ulong blockHeight
    )
    {
        return repository
            .AddAsync(new SmartContractReadHeight(blockHeight));
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