using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Aggregates.SmartContract;

public interface ISmartContractRepository : IAsyncDisposable
{
    /// <summary>
    /// Returns null if entity doesn't exist at height.
    /// </summary>
    public Task<SmartContractReadHeight?> GetReadOnlySmartContractReadHeightAtHeight(ulong blockHeight);
    /// <summary>
    /// Throws exception if block at height doesn't exist.
    /// </summary>
    /// <param name="blockHeight"></param>
    /// <returns></returns>
    public Task<long> GetReadOnlyBlockIdAtHeight(int blockHeight);
    /// <summary>
    /// Returns all transaction from a block id.
    /// </summary>
    public Task<IList<Transaction>> GetReadOnlyTransactionsAtBlockId(long blockId);
    /// <summary>
    /// Returns transaction result events from a transaction id. 
    /// </summary>
    public Task<IList<TransactionRelated<TransactionResultEvent>>> GetReadOnlyTransactionResultEventsFromTransactionId(long transactionId);
    /// <summary>
    /// Returns latest smart contract read height ordered descending by block height. 
    /// </summary>
    /// <returns></returns>
    public Task<SmartContractReadHeight?> GetReadOnlyLatestSmartContractReadHeight();
    /// <summary>
    /// Get latest import state of block- and transactions.
    ///
    /// Should return zero (default) is no entity is present.
    /// </summary>
    /// <returns></returns>
    public Task<long> GetLatestImportState(CancellationToken token = default);
    /// <summary>
    /// Adds entity to repository.
    /// </summary>
    ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class;
    /// <summary>
    /// Save changes to storage.
    /// </summary>
    Task SaveChangesAsync(CancellationToken token);
}