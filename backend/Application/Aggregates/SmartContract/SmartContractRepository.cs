using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Aggregates.SmartContract;

internal sealed class SmartContractRepository : ISmartContractRepository
{
    private readonly GraphQlDbContext _context;
    
    public SmartContractRepository(GraphQlDbContext context)
    {
        _context = context;
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
    public ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class
    {
        return _context.Set<T>().AddAsync(entity);
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