using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractAddress = Application.Api.GraphQL.ContractAddress;
using ContractInitialized = Concordium.Sdk.Types.ContractInitialized;
using Transferred = Application.Api.GraphQL.Transactions.Transferred;

namespace Application.Aggregates.SmartContract;

internal sealed class SmartContractAggregate
{
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

    internal enum ImportType
    {
        NodeImport,
        DatabaseImport,
    }

    public SmartContractAggregate(
        ISmartContractRepositoryFactory repositoryFactory
    )
    {
        _repositoryFactory = repositoryFactory;
        _logger = _logger = Log.ForContext<SmartContractAggregate>();
    }

    internal async Task NodeImportJob(ISmartContractNodeClient client, CancellationToken token = default)
    {
        // TODO - add some resilience
        var lastHeight = await GetLastReadBlockHeight();
        while (!token.IsCancellationRequested)
        {
            var consensusInfo = await client.GetConsensusInfoAsync(token);
            var newLastHeight = consensusInfo.LastFinalizedBlockHeight;
            if (lastHeight == newLastHeight)
            {
                await Task.Delay(_delay, token);
                continue;
            }

            for (var height = lastHeight; height <= newLastHeight; height++)
            {
                await using var repository = await _repositoryFactory.CreateAsync();
                
                await NodeImport(repository, client, height, token);
                await SaveLastReadBlock(repository, height);
                await repository.SaveChangesAsync(token);
            }
            lastHeight = newLastHeight;
        }
    }

    internal async Task DatabaseImportJob(long height, CancellationToken token = default)
    {
        await using var repository = await _repositoryFactory.CreateAsync();

        var existing = await repository.GetReadOnlySmartContractReadHeightAtHeight((ulong)height);
        if (existing is not null)
        {
            _logger.Warning($"Block Height {height} already written.");
        }

        await DatabaseImport(repository, height);
        await SaveLastReadBlock(repository, (ulong)height);
        await repository.SaveChangesAsync(token);
    }
    
    internal async Task DatabaseImport(ISmartContractRepository repository, long blockHeight)
    {
        var blockIdAtHeight = await repository.GetReadOnlyBlockIdAtHeight((int)blockHeight);

        var transactions = await repository.GetReadOnlyTransactionsAtBlockId(blockIdAtHeight);
        
        foreach (var transaction in transactions)
        {
            if (transaction.SenderAccountAddress == null)
            {
                throw new SmartContractImportException(
                    $"Not able to map transaction: {transaction.TransactionHash}, since transaction sender was null");
            }
            
            var transactionEvents = await repository.GetReadOnlyTransactionResultEventsFromTransactionId(transaction.Id);
            foreach (var transactionRelated in transactionEvents)
            {
                await StoreEvent(
                    repository,
                    transactionRelated.Entity,
                    transaction.SenderAccountAddress,
                    (ulong)blockHeight,
                    transaction.TransactionHash,
                    0,
                    (uint)transactionRelated.Index
                );
            }
        }
    }

    internal async Task NodeImport(
        ISmartContractRepository repository,
        ISmartContractNodeClient client,
        ulong height,
        CancellationToken token = default
        )
    {
        var blockHashInput = new Absolute(height);
        var (blockHash, transactionEvents) = await client.GetBlockTransactionEvents(blockHashInput, token);
        _logger.Debug("Reading {@BlockHash}", blockHash);
        
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
    
    private async Task StoreEvent(
        ISmartContractRepository repository,        
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
                await repository.AddAsync(new SmartContract(
                    blockHeight,
                    transactionHash,
                    transactionIndex,
                    eventIndex,
                    contractInitialized.ContractAddress,
                    sender
                ));
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
                    transferred.To is not AccountAddress)
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
    
    internal async Task<ulong> GetLastReadBlockHeight()
    {
        await using var repository = await _repositoryFactory.CreateAsync();
        var importState = await repository.GetReadOnlyLatestSmartContractReadHeight();
        return importState?.BlockHeight ?? 0;
    }

    private static Task SaveLastReadBlock(
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