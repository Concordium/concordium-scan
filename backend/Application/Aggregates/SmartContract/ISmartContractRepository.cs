using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.SmartContract.Dto;
using Application.Aggregates.SmartContract.Entities;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.SmartContract;

public interface ISmartContractRepository : IAsyncDisposable
{
    /// <summary>
    /// From block <see cref="heightFrom"/> to block <see cref="heightTo"/> all transaction result event
    /// related to smart contract will be returned.
    /// </summary>
    Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetSmartContractRelatedTransactionResultEventRelations(ulong heightFrom, ulong heightTo);
    /// <summary>
    /// From block <see cref="heightFrom"/> to block <see cref="heightTo"/> return all block heights
    /// which has already been read and processed successfully.
    /// </summary>
    public Task<List<ulong>> FromBlockHeightRangeGetBlockHeightsReadOrdered(ulong heightFrom, ulong heightTo);
    /// <summary>
    /// Returns null if entity doesn't exist at height.
    /// </summary>
    Task<SmartContractReadHeight?> GetReadOnlySmartContractReadHeightAtHeight(ulong blockHeight);
    /// <summary>
    /// Get block id at block height.
    ///
    /// Should throw exception if no block at height exist.
    /// </summary>
    Task<long> GetReadOnlyBlockIdAtHeight(int blockHeight);
    /// <summary>
    /// Returns all transaction from a block id.
    /// </summary>
    Task<IList<Transaction>> GetReadOnlyTransactionsAtBlockId(long blockId);
    /// <summary>
    /// Returns transaction result events from a transaction id. 
    /// </summary>
    Task<IList<TransactionRelated<TransactionResultEvent>>> GetReadOnlyTransactionResultEventsFromTransactionId(long transactionId);
    /// <summary>
    /// Returns latest smart contract read height ordered descending by block height. 
    /// </summary>
    Task<SmartContractReadHeight?> GetReadOnlyLatestSmartContractReadHeight();
    /// <summary>
    /// Get latest import state of block- and transactions ordered by
    /// block slot time.
    ///
    /// Should return zero (default) is no entity is present.
    /// </summary>
    Task<long> GetLatestImportState(CancellationToken token = default);
    /// <summary>
    /// Adds entity to repository.
    /// </summary>
    Task AddAsync<T>(params T[] entities) where T : class;
    /// <summary>
    /// Adds entities to repository,
    /// </summary>
    Task AddRangeAsync<T>(IEnumerable<T> heights) where T : class;
    /// <summary>
    /// Save changes to storage.
    /// </summary>
    Task SaveChangesAsync(CancellationToken token);
}