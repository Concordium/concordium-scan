using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.SmartContract.Dto;
using Application.Aggregates.SmartContract.Entities;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.SmartContract;

internal sealed class SmartContractRepository : ISmartContractRepository
{
    private readonly GraphQlDbContext _context;
    
    public SmartContractRepository(GraphQlDbContext context)
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
    public async Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetSmartContractRelatedTransactionResultEventRelations(int heightFrom, int heightTo)
    {
        const string sql = @"
SELECT
    gb.block_height as BlockHeight,
    gt.transaction_type as TransactionType,
    gt.sender as TransactionSender,
    gt.transaction_hash as TransactionHash,
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
            .QueryAsync<TransactionResultEventDto>(sql, new { FromHeight = heightFrom, ToHeight = heightTo });
        return queryAsync.ToList();
    }

    /// <inheritdoc/>
    public async Task<SmartContractReadHeight?> GetReadOnlySmartContractReadHeightAtHeight(ulong blockHeight)
    {
        var block = await _context.SmartContractReadHeights
            .Where(b => b.BlockHeight == blockHeight)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        return block;
    }
    /// <inheritdoc/>
    public async Task<long> GetReadOnlyBlockIdAtHeight(int blockHeight)
    {
        return await _context.Blocks
            .Where(b => b.BlockHeight == blockHeight)
            .Select(b => b.Id)
            .FirstAsync();
    }
    /// <inheritdoc/>
    public async Task<IList<Transaction>> GetReadOnlyTransactionsAtBlockId(long blockId)
    {
        return await _context.Transactions
            .Where(t => t.BlockId == blockId)
            .AsNoTracking()
            .ToListAsync();
    }
    /// <inheritdoc/>
    public async Task<IList<TransactionRelated<TransactionResultEvent>>> GetReadOnlyTransactionResultEventsFromTransactionId(
        long transactionId)
    {
        return await _context.TransactionResultEvents
            .Where(te => te.TransactionId == transactionId)
            .AsNoTracking()
            .ToListAsync();
    }
    /// <inheritdoc/>
    public async Task<SmartContractReadHeight?> GetReadOnlyLatestSmartContractReadHeight()
    {
        return await _context.SmartContractReadHeights
            .OrderByDescending(x => x.BlockHeight)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
    /// <inheritdoc/>
    public async Task<long> GetLatestImportState(CancellationToken token)
    {
        return await _context.ImportState
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