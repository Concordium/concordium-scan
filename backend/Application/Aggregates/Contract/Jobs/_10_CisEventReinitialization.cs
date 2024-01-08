using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Jobs;

public sealed class _10_CisEventReinitialization : IStatelessJob
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IEventLogHandler _eventLogHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_10_CisEventReinitialization";

    public _10_CisEventReinitialization(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IEventLogHandler eventLogHandler
        )
    {
        _contextFactory = contextFactory;
        _eventLogHandler = eventLogHandler;
        _logger = Log.ForContext<_10_CisEventReinitialization>();
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var max = context.ContractEvents.Max(ce => ce.ContractAddressIndex);
        return Enumerable.Range(0, (int)max + 1);
    }

    /// <inheritdoc/>
    public async ValueTask Setup(CancellationToken token)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var connection = context.Database.GetDbConnection();
        await connection.ExecuteAsync("truncate table graphql_account_tokens, graphql_token_events, graphql_tokens;");
    }

    /// <inheritdoc/>
    public async ValueTask Process(int identifier, CancellationToken token = default)
    {
        _logger.Debug($"Start processing {identifier}");
        TransactionScope? transactionScope = null;
        try
        {
            transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
            
            var contractEvents = await GetContractEvents(identifier, token);
            var jobContractRepository = new JobContractRepository(contractEvents);
            await _eventLogHandler.HandleCisEvent(jobContractRepository);
            transactionScope.Complete();
            _logger.Debug($"Completed successfully processing {identifier}");
        }
        finally
        {
            transactionScope?.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => true;

    private async Task<IList<ContractEvent>> GetContractEvents(int address, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var contractEvents = await context.ContractEvents.Where(ce => ce.ContractAddressIndex == (ulong)address)
            .ToListAsync(cancellationToken: token);
        return contractEvents;
    }
    
    private sealed class JobContractRepository : IContractRepository
    {
        private readonly IEnumerable<ContractEvent> _contractEvents;
        
        public JobContractRepository(IEnumerable<ContractEvent> contractEvents)
        {
            _contractEvents = contractEvents;
        }
    
        public IEnumerable<ContractEvent> GetContractEventsAddedInTransaction() => _contractEvents;
    
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    
        public Task<IList<TransactionRejectEventDto>> FromBlockHeightRangeGetContractRelatedRejections(ulong heightFrom, ulong heightTo)
        {
            throw new NotImplementedException();
        }
    
        public Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(ulong heightFrom, ulong heightTo)
        {
            throw new NotImplementedException();
        }
    
        public Task<List<ulong>> FromBlockHeightRangeGetBlockHeightsReadOrdered(ulong heightFrom, ulong heightTo)
        {
            throw new NotImplementedException();
        }
    
        public Task<ContractReadHeight?> GetReadonlyLatestContractReadHeight()
        {
            throw new NotImplementedException();
        }
    
        public Task<long> GetReadonlyLatestImportState(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    
        public Task<ContractInitialized> GetReadonlyContractInitializedEventAsync(ContractAddress contractAddress)
        {
            throw new NotImplementedException();
        }
    
        public Task AddAsync<T>(params T[] entities) where T : class
        {
            throw new NotImplementedException();
        }
    
        public Task AddRangeAsync<T>(IEnumerable<T> heights) where T : class
        {
            throw new NotImplementedException();
        }
    
        public Task CommitAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
