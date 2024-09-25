using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract;

public interface IContractRepository : IAsyncDisposable, IModuleReadonlyRepository
{
    /// <summary>
    /// From block <see cref="heightFrom"/> to block <see cref="heightTo"/> all transaction rejected events
    /// will be returned which is related to contract or module entities.
    /// </summary>
    public Task<IList<TransactionRejectEventDto>>
        FromBlockHeightRangeGetContractRelatedRejections(ulong heightFrom, ulong heightTo);
    /// <summary>
    /// From block <see cref="heightFrom"/> to block <see cref="heightTo"/> all transaction result event
    /// related to contracts will be returned.
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
    Task<ContractReadHeight?> GetReadonlyLatestContractReadHeight();
    /// <summary>
    /// Get latest import state of block- and transactions ordered by
    /// block slot time.
    ///
    /// Should return zero (default) is no entity is present.
    /// </summary>
    Task<long> GetReadonlyLatestImportState(CancellationToken token = default);
    /// <summary>
    /// Get contract initialization event for <see cref="contractAddress"/>.
    ///
    /// Entity is read only and changes on entity will not be persisted.
    /// </summary>
    Task<ContractInitialized> GetReadonlyContractInitializedEventAsync(ContractAddress contractAddress);
    /// <summary>
    /// Get added <see cref="T"/> in current transaction.
    /// </summary>
    IEnumerable<T> GetEntitiesAddedInTransaction<T>() where T : class;
    /// <summary>
    /// Get latest added <see cref="ContractSnapshot"/> for a <see cref="Contract"/>.
    /// </summary>
    Task<ContractSnapshot> GetReadonlyLatestContractSnapshot(ContractAddress contractAddress);
    /// <summary>
    /// Adds entity to repository.
    /// </summary>
    Task AddAsync<T>(params T[] entities) where T : class;
    /// <summary>
    /// Adds entities to repository,
    /// </summary>
    Task AddRangeAsync<T>(IEnumerable<T> heights) where T : class;
    /// <summary>
    /// Saves and commit changes to storage.
    ///
    /// This should be the last method used before disposing the entity.
    /// </summary>
    Task CommitAsync(CancellationToken token);
}

internal sealed class ContractRepository : IContractRepository
{
    private readonly TransactionScope _transactionScope;
    private readonly GraphQlDbContext _context;
    
    private ContractRepository(TransactionScope scope, GraphQlDbContext context)
    {
        _transactionScope = scope;
        _context = context;
    }

    internal static async Task<ContractRepository> Create(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        var transactionScope = CreateTransactionScope();
        var graphQlDbContext = await dbContextFactory.CreateDbContextAsync();
        return new ContractRepository(transactionScope, graphQlDbContext);
    }
    
    private static TransactionScope CreateTransactionScope() =>
        new(
            TransactionScopeOption.Required,
            new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
            TransactionScopeAsyncFlowOption.Enabled);

    /// <summary>
    /// <see cref="Api.GraphQL.Transactions.Transaction"/> has column `block_id` which is reference to <see cref="Application.Api.GraphQL.Blocks.Block"/>.
    ///
    /// From <see cref="Application.Api.GraphQL.EfCore.Converters.Json.TransactionRejectReasonConverter"/> there is a mapping between
    /// `tag` in column `reject_reason` and a transaction.
    /// </summary>
    public async Task<IList<TransactionRejectEventDto>>
        FromBlockHeightRangeGetContractRelatedRejections(ulong heightFrom, ulong heightTo)
    {
        const string sql = @"
SELECT
    gb.block_height as BlockHeight,
    gb.block_slot_time as BlockSlotTime,
    gt.transaction_type as TransactionType,
    gt.sender as TransactionSender,
    gt.transaction_hash as TransactionHash,
    gt.index as TransactionIndex,
    gt.reject_reason as RejectedEvent
FROM
    graphql_transactions gt
        JOIN
    graphql_blocks gb ON gt.block_id = gb.id
WHERE
        gb.block_height >= @FromHeight AND gb.block_height <= @ToHeight
  AND gt.reject_reason is not null and gt.reject_reason->>'tag' IN ('2', '4', '5', '13');
";
        var queryAsync = await _context.Database.GetDbConnection()
            .QueryAsync<TransactionRejectEventDto>(sql, new { FromHeight = (long)heightFrom, ToHeight = (long)heightTo });
        return queryAsync.ToList();        
    }

    /// <summary>
    /// <see cref="Api.GraphQL.Transactions.Transaction"/> has column `block_id` which is reference to <see cref="Application.Api.GraphQL.Blocks.Block"/>.
    /// <see cref="TransactionResultEvent"/> has column `transaction_id` which is reference to <see cref="Api.GraphQL.Transactions.Transaction"/>.
    ///
    /// From <see cref="Application.Api.GraphQL.EfCore.Converters.Json.TransactionResultEventConverter"/> there is a mapping between
    /// `tag` in column `event` and a transaction event.
    /// </summary>
    public async Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(ulong heightFrom, ulong heightTo)
    {
        const string sql = @"
SELECT
    gb.block_height as BlockHeight,
    gb.block_slot_time as BlockSlotTime,
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
  AND te.event->>'tag' IN ('1', '16', '17', '18', '34', '35', '36');
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
    public async Task<ContractReadHeight?> GetReadonlyLatestContractReadHeight()
    {
        return await _context.ContractReadHeights
            .AsNoTracking()
            .OrderByDescending(x => x.BlockHeight)
            .FirstOrDefaultAsync();
    }
    
    /// <inheritdoc/>
    public async Task<long> GetReadonlyLatestImportState(CancellationToken token)
    {
        return await _context.ImportState
            .AsNoTracking()
            .OrderByDescending(s => s.LastBlockSlotTime)
            .Select(s => s.MaxImportedBlockHeight)
            .FirstOrDefaultAsync(token);
    }
    
    /// <summary>
    /// Looking after <see cref="ContractInitialized"/> event for <see cref="contractAddress"/>. First the event is searched
    /// for in the change provider of Entity Framework. These are the entities which have been added in the current transaction
    /// but are not yet committed to the database.
    ///
    /// If <see cref="ContractInitialized"/> has not been added in this transaction the database it queried for the event.
    ///
    /// The <see cref="ContractInitialized"/> event is expected to exist, and an exception will be thrown if no event
    /// is found. An exception will also be thrown if multiple events are returned, as this should not be possible and
    /// would indicate data corruption.
    /// </summary>
    public async Task<ContractInitialized> GetReadonlyContractInitializedEventAsync(ContractAddress contractAddress)
    {
        var contractInitialized = GetEntitiesAddedInTransaction<ContractEvent>()
            .Where(e => e.ContractAddressIndex == contractAddress.Index && e.ContractAddressSubIndex == contractAddress.SubIndex)
            .Where(e => e.Event is ContractInitialized)
            .Select(e => (e.Event as ContractInitialized)!)
            .FirstOrDefault();

        if (contractInitialized != null)
        {
            return contractInitialized;
        }

        var connection = _context.Database.GetDbConnection();
        var parameter = new { Index = (long)contractAddress.Index, Subindex = (long)contractAddress.SubIndex};
        var contractEvents = (await connection.QueryAsync<ContractEvent>(ContractEvent.ContractInitializedEventSql, parameter))
            .First();
        
        return (contractEvents!.Event as ContractInitialized)!;
    }

    /// <inheritdoc/>
    public Task<ContractSnapshot> GetReadonlyLatestContractSnapshot(ContractAddress contractAddress) =>
        _context.ContractSnapshots
            .Where(cs => cs.ContractAddressIndex == contractAddress.Index && cs.ContractAddressSubIndex == contractAddress.SubIndex)
            .OrderByDescending(cs => cs.BlockHeight)
            .FirstAsync();

    /// <inheritdoc/>
    public IEnumerable<T> GetEntitiesAddedInTransaction<T>() where T : class
        => _context.ChangeTracker.Entries<T>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity);
    
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
    public async Task CommitAsync(CancellationToken token = default)
    {
        await _context.SaveChangesAsync(token);
        _transactionScope.Complete();
    }
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _transactionScope.Dispose();
    }

    public async Task<ModuleReferenceEvent> GetModuleReferenceEventAsync(string moduleReference)
    {
        var moduleRefEvent = GetEntitiesAddedInTransaction<ModuleReferenceEvent>()
            .Where(e => e.ModuleReference == moduleReference)
            .FirstOrDefault();

        if (moduleRefEvent != null)
        {
            return moduleRefEvent;
        }

        return await _context.ModuleReferenceEvents
            .AsNoTracking()
            .FirstAsync(m => m.ModuleReference == moduleReference);
    }

    /// <summary>
    /// Starts by looking after <see cref="ModuleReferenceContractLinkEvent"/> with <see cref="ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added"/>
    /// for the given <see cref="contractAddress"/> in the change provider of Entity Framework. These are the entity which has been added in the current transaction
    /// but are not yet committed to the database.
    ///
    /// If none is present the database is queried.
    /// </summary>
    public async Task<ModuleReferenceEvent> GetModuleReferenceEventAtAsync(ContractAddress contractAddress, ulong blockHeight, ulong transactionIndex,
        uint eventIndex)
    {
        var link = _context.ChangeTracker
            .Entries<ModuleReferenceContractLinkEvent>()
            .Select(e => e.Entity)
            .Where(l => 
                l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                (l.BlockHeight == blockHeight && l.TransactionIndex == transactionIndex && l.EventIndex <= eventIndex ||
                 l.BlockHeight == blockHeight && l.TransactionIndex < transactionIndex ||
                 l.BlockHeight < blockHeight
                 ) &&
                l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .OrderByDescending(l => l.BlockHeight)
            .ThenByDescending(l => l.TransactionIndex)
            .ThenByDescending(l => l.EventIndex)
            .FirstOrDefault();

        if (link == null)
        {
            link = await _context.ModuleReferenceContractLinkEvents
                .AsNoTracking()
                .Where(l => 
                    l.ContractAddressIndex == contractAddress.Index && l.ContractAddressSubIndex == contractAddress.SubIndex &&
                    (l.BlockHeight == blockHeight && l.TransactionIndex == transactionIndex && l.EventIndex <= eventIndex ||
                     l.BlockHeight == blockHeight && l.TransactionIndex < transactionIndex ||
                     l.BlockHeight < blockHeight
                    ) &&
                    l.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                .OrderByDescending(l => l.BlockHeight)
                .ThenByDescending(l => l.TransactionIndex)
                .ThenByDescending(l => l.EventIndex)
                .FirstAsync();
        }

        var module = await _context.ModuleReferenceEvents
            .AsNoTracking()
            .FirstAsync(m => m.ModuleReference == link.ModuleReference);
        
        return module;
    }
}
