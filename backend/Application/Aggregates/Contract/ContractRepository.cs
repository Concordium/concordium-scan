using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract;

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
    public async Task<ContractReadHeight?> GetReadOnlyContractReadHeightAtHeight(ulong blockHeight)
    {
        var block = await _context.ContractReadHeights
            .Where(b => b.BlockHeight == blockHeight)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        return block;
    }
    /// <inheritdoc/>
    public async Task<long> GetReadOnlyBlockIdAtHeight(int blockHeight)
    {
        return await _context.Blocks
            .AsNoTracking()
            .Where(b => b.BlockHeight == blockHeight)
            .Select(b => b.Id)
            .FirstAsync();
    }
    /// <inheritdoc/>
    public async Task<IList<Transaction>> GetReadOnlyTransactionsAtBlockId(long blockId)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.BlockId == blockId)
            .ToListAsync();
    }
    /// <inheritdoc/>
    public async Task<IList<TransactionRelated<TransactionResultEvent>>> GetReadOnlyTransactionResultEventsFromTransactionId(
        long transactionId)
    {
        return await _context.TransactionResultEvents
            .AsNoTracking()
            .Where(te => te.TransactionId == transactionId)
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