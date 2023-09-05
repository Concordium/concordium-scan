using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract;

public interface IContractRepository : IAsyncDisposable
{
    /// <summary>
    /// From block <see cref="heightFrom"/> to block <see cref="heightTo"/> all transaction result event
    /// related to smart contract will be returned.
    /// </summary>
    Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(ulong heightFrom, ulong heightTo);
    /// <summary>
    /// From block <see cref="heightFrom"/> to block <see cref="heightTo"/> return all block heights
    /// which has already been read and processed successfully.
    /// </summary>
    public Task<List<ulong>> FromBlockHeightRangeGetBlockHeightsReadOrdered(ulong heightFrom, ulong heightTo);
    /// <summary>
    /// Returns latest smart contract read height ordered descending by block height. 
    /// </summary>
    Task<ContractReadHeight?> GetReadOnlyLatestContractReadHeight();
    /// <summary>
    /// Get latest import state of block- and transactions ordered by
    /// block slot time.
    ///
    /// Should return zero (default) is no entity is present.
    /// </summary>
    Task<long> GetReadOnlyLatestImportState(CancellationToken token = default);
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

internal sealed class ContractRepository : IContractRepository
{
    private readonly GraphQlDbContext _context;
    
    public ContractRepository(GraphQlDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// <see cref="Transaction"/> has column `block_id` which is reference to <see cref="Application.Api.GraphQL.Blocks.Block"/>.
    /// <see cref="TransactionResultEvent"/> has column `transaction_id` which is reference to <see cref="Transaction"/>.
    ///
    /// From <see cref="Application.Api.GraphQL.EfCore.Converters.Json.TransactionResultEventConverter"/> there is a mapping between
    /// `tag` in column `event` and a transaction event.
    /// </summary>
    public async Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(ulong heightFrom, ulong heightTo)
    {
        const string sql = @"
SELECT
    gb.block_height as BlockHeight,
    gt.transaction_type as TransactionType,
    gt.sender as TransactionSender,
    gt.transaction_hash as TransactionHash,
    gt.index as TransactionIndex,
    te.index as TransactionEventIndex,
    te.event as Event
FROM
    graphql_transaction_events te
        JOIN
    graphql_transactions gt ON te.transaction_id = gt.id
        JOIN
    graphql_blocks gb ON gt.block_id = gb.id
WHERE
        gb.block_height >= @FromHeight AND gb.block_height <= @ToHeight
  AND te.event->>'tag' IN ('1', '16', '18', '17', '34', '35', '36');
";
        var queryAsync = await _context.Database.GetDbConnection()
            .QueryAsync<TransactionResultEventDto>(sql, new { FromHeight = (long)heightFrom, ToHeight = (long)heightTo });
        return queryAsync.ToList();
    }
    
    /// <inheritdoc/>
    public async Task<List<ulong>> FromBlockHeightRangeGetBlockHeightsReadOrdered(ulong heightFrom, ulong heightTo)
    {
        return await _context.ContractReadHeights
            .Where(r => r.BlockHeight >= heightFrom && r.BlockHeight <= heightTo)
            .Select(r => r.BlockHeight)
            .OrderBy(r => r)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<ContractReadHeight?> GetReadOnlyLatestContractReadHeight()
    {
        return await _context.ContractReadHeights
            .AsNoTracking()
            .OrderByDescending(x => x.BlockHeight)
            .FirstOrDefaultAsync();
    }
    
    /// <inheritdoc/>
    public async Task<long> GetReadOnlyLatestImportState(CancellationToken token)
    {
        return await _context.ImportState
            .AsNoTracking()
            .OrderByDescending(s => s.LastBlockSlotTime)
            .Select(s => s.MaxImportedBlockHeight)
            .FirstOrDefaultAsync(token);
    }
    
    /// <inheritdoc/>
    public Task AddAsync<T>(params T[] entities) where T : class
    {
        return _context.Set<T>().AddRangeAsync(entities);
    }
    
    /// <inheritdoc/>
    public Task AddRangeAsync<T>(IEnumerable<T> heights) where T : class
    {
        return _context.Set<T>().AddRangeAsync(heights);
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken token = default)
    {
        return _context.SaveChangesAsync(token);
    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}