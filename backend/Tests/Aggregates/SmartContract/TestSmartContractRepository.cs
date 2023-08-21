using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.SmartContract;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Tests.Aggregates.SmartContract;


internal sealed class TestSmartContractRepository : ISmartContractRepository
{
    internal readonly IList<SmartContractReadHeight> SmartContractAggregateImportStates = new List<SmartContractReadHeight>();
    internal readonly IList<SmartContractEvent> SmartContractEvents = new List<SmartContractEvent>();
    internal readonly IList<ModuleReferenceEvent> ModuleReferenceEvents = new List<ModuleReferenceEvent>();
    internal readonly IList<ModuleReferenceSmartContractLinkEvent> ModuleReferenceSmartContractLinkEvents = new List<ModuleReferenceSmartContractLinkEvent>();
    internal readonly IList<Application.Aggregates.SmartContract.SmartContract> SmartContracts = new List<Application.Aggregates.SmartContract.SmartContract>();
    internal readonly IList<Block> Blocks = new List<Block>();
    internal readonly IList<Transaction> Transactions = new List<Transaction>();
    internal readonly IList<TransactionRelated<TransactionResultEvent>> TransactionRelations = new List<TransactionRelated<TransactionResultEvent>>();

    public IQueryable<T> GetReadOnlyQueryable<T>() where T : class
    {
        if (typeof(T) == typeof(SmartContractReadHeight))
        {
            return SmartContractAggregateImportStates.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(SmartContractEvent))
        {
            return SmartContractEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(ModuleReferenceEvent))
        {
            return ModuleReferenceEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(ModuleReferenceSmartContractLinkEvent))
        {
            return ModuleReferenceSmartContractLinkEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(Application.Aggregates.SmartContract.SmartContract))
        {
            return SmartContracts.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(Block))
        {
            return Blocks.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(Transaction))
        {
            return Transactions.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(TransactionRelated<TransactionResultEvent>))
        {
            return TransactionRelations.Cast<T>().AsQueryable();
        }
        throw new NotImplementedException($"Not implemented for type: {typeof(T)}");
    }

    public Task<SmartContractReadHeight?> GetReadOnlySmartContractReadHeightAtHeight(ulong blockHeight)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetReadOnlyBlockIdAtHeight(int blockHeight)
    {
        throw new NotImplementedException();
    }

    public Task<IList<Transaction>> GetReadOnlyTransactionsAtBlockId(long blockId)
    {
        throw new NotImplementedException();
    }

    public Task<IList<TransactionRelated<TransactionResultEvent>>> GetReadOnlyTransactionResultEventsFromTransactionId(long transactionId)
    {
        throw new NotImplementedException();
    }

    public Task<SmartContractReadHeight?> GetReadOnlyLatestSmartContractReadHeight()
    {
        throw new NotImplementedException();
    }

    public Task<long> GetLatestImportState(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync<T>(params T[] entity) where T : class
    {
        if (typeof(T) == typeof(SmartContractReadHeight))
        {
            SmartContractAggregateImportStates.Add((entity as SmartContractReadHeight)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(SmartContractEvent))
        {
            SmartContractEvents.Add((entity as SmartContractEvent)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(ModuleReferenceEvent))
        {
            ModuleReferenceEvents.Add((entity as ModuleReferenceEvent)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(ModuleReferenceSmartContractLinkEvent))
        {
            ModuleReferenceSmartContractLinkEvents.Add((entity as ModuleReferenceSmartContractLinkEvent)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(Application.Aggregates.SmartContract.SmartContract))
        {
            SmartContracts.Add((entity as Application.Aggregates.SmartContract.SmartContract)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(Block))
        {
            Blocks.Add((entity as Block)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(Transaction))
        {
            Transactions.Add((entity as Transaction)!);
            return Task.CompletedTask;
        }
        if (typeof(T) == typeof(TransactionRelated<TransactionResultEvent>))
        {
            TransactionRelations.Add((entity as TransactionRelated<TransactionResultEvent>)!);
            return Task.CompletedTask;
        }
        throw new NotImplementedException($"Not implemented for type: {typeof(T)}");
    }

    public Task SaveChangesAsync(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}