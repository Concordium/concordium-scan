using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Resilience;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Observability;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

public sealed class InitialContractEventDeserializationFieldsCatchUpJob : IStatelessJob
{
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "InitialContractEventDeserializationFieldsCatchUpJob";
    
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly ILogger _logger;
    private readonly ContractAggregateOptions _contractAggregateOptions;
    private readonly ContractAggregateJobOptions _jobOptions;

    public InitialContractEventDeserializationFieldsCatchUpJob(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IOptions<ContractAggregateOptions> options
    )
    {
        _contextFactory = contextFactory;
        _logger = Log.ForContext<InitialContractEventDeserializationFieldsCatchUpJob>();
        _contractAggregateOptions = options.Value;
        var gotJobOptions = _contractAggregateOptions.Jobs.TryGetValue(GetUniqueIdentifier(), out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new ContractAggregateJobOptions();    
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;
    
    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => false;
    
    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken)
    {
        var eventCount = await GetEventCount(cancellationToken);
        var batchCount = eventCount / _jobOptions.BatchSize + 1;
        
        _logger.Debug($"Should process {eventCount} contract events in {batchCount} batches");
        
        return Enumerable.Range(0, batchCount);
    }
    
    /// <summary>
    /// Updates <see cref="Application.Aggregates.Contract.Entities.ContractEvent"/> with hexadecimal fields parsed. 
    /// </summary>
    public async ValueTask Process(int identifier, CancellationToken token)
    {
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _contractAggregateOptions.RetryCount, _contractAggregateOptions.RetryDelay)
            .ExecuteAsync(async () =>
            {
                using var _ = TraceContext.StartActivity($"{nameof(InitialContractEventDeserializationFieldsCatchUpJob)}.{nameof(Process)}");
                var take = _jobOptions.BatchSize;
                var skip = identifier * take;
                _logger.Debug($"Start parsing contract events events in range {skip + 1} to {skip + take}");

                var context = await _contextFactory.CreateDbContextAsync(token);

                await using var contractRepository = InMemoryContractRepository.Create(context);
                await using var moduleReadonlyRepository = InMemoryModuleRepository.Create(context);

                var contractEvents = await context.ContractEvents
                    .AsNoTracking()
                    .OrderBy(ce => ce.BlockHeight)
                    .ThenBy(ce => ce.TransactionIndex)
                    .ThenBy(ce => ce.EventIndex)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(token);
                
                var eventUpdates = new List<Update>();
                foreach (var contractEvent in contractEvents
                             .Where(contractEvent => !IsHexadecimalFieldsParsed(contractEvent)))
                {
                    try
                    {
                        var transactionResultEvent = await TryParseEvent(contractEvent, contractRepository, moduleReadonlyRepository);
                        if (transactionResultEvent != null)
                        {
                            eventUpdates.Add(new Update(
                                transactionResultEvent,
                                DateTimeOffset.UtcNow,
                                (long)contractEvent.ContractAddressIndex,
                                (long)contractEvent.ContractAddressSubIndex,
                                (long)contractEvent.BlockHeight,
                                (long)contractEvent.TransactionIndex,
                                (int)contractEvent.EventIndex));
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Exception when processing <{ContractAddressIndex},{ContractAddressSubIndex}> contract event at {BlockHeight}, {TransactionIndex}, {EventIndex}",
                            contractEvent.ContractAddressIndex,
                            contractEvent.ContractAddressSubIndex,
                            contractEvent.BlockHeight,
                            contractEvent.TransactionIndex,
                            contractEvent.EventIndex);
                        throw;
                    }
                }
                
                _logger.Debug($"Updating {eventUpdates.Count} contract events in range {skip + 1} to {skip + take}");

                await context.Database.GetDbConnection()
                    .ExecuteAsync(
                        @"
                    UPDATE graphql_contract_events SET event = @Event::json, updated_at = @UpdatedAt
                    WHERE block_height = @BlockHeight 
                      AND contract_address_index = @ContractAddressIndex 
                      AND contract_address_subindex = @ContractAddressSubIndex 
                      AND event_index = @EventIndex 
                      AND transaction_index = @TransactionIndex;", eventUpdates);
                
                _logger.Debug($"Successfully parsed contract events in range {skip + 1} to {skip + take}");
            });
    }

    private sealed record Update(
        TransactionResultEvent Event,
        DateTimeOffset UpdatedAt,
        long ContractAddressIndex,
        long ContractAddressSubIndex,
        long BlockHeight,
        long TransactionIndex,
        int EventIndex
        );
    
    /// <summary>
    /// Try parse hexadecimal events and parameters in <see cref="ContractEvent.Event"/>. If parsing succeeds the event is overriden.
    /// </summary>
    private static async Task<TransactionResultEvent?> TryParseEvent(ContractEvent contractEvent, IContractRepository contractRepository, IModuleReadonlyRepository moduleReadonlyRepository)
    {
        return contractEvent.Event switch
        {
            ContractCall contractCall => await contractCall.TryUpdate(
                moduleReadonlyRepository,
                contractEvent.BlockHeight,
                contractEvent.TransactionIndex,
                contractEvent.EventIndex
            ),
            ContractInitialized contractInitialized => await contractInitialized.TryUpdateWithParsedEvents(moduleReadonlyRepository),
            ContractInterrupted contractInterrupted => await contractInterrupted.TryUpdateWithParsedEvents(
                contractRepository,
                moduleReadonlyRepository,
                contractEvent.BlockHeight,
                contractEvent.TransactionIndex,
                contractEvent.EventIndex),
            ContractUpdated contractUpdated => await contractUpdated.TryUpdate(
                moduleReadonlyRepository,
                contractEvent.BlockHeight,
                contractEvent.TransactionIndex,
                contractEvent.EventIndex
            ),
            _ => null
        };
    }
    
    /// <summary>
    /// Check if hexadecimal fields in <see cref="ContractEvent"/>'s has been parsed or there is nothing to parse.
    /// </summary>
    private static bool IsHexadecimalFieldsParsed(ContractEvent contractEvent)
    {
        return contractEvent.Event switch
        {
            ContractCall contractCall => contractCall.ContractUpdated.Message != null &&
                                         contractCall.ContractUpdated.Events != null,
            ContractInitialized contractInitialized => contractInitialized.Events != null,
            ContractInterrupted contractInterrupted => contractInterrupted.Events != null,
            ContractUpdated contractUpdated => contractUpdated.Message != null && contractUpdated.Events != null,
            _ => true
        };
    }
        
    private async Task<int> GetEventCount(CancellationToken token)
    {
        
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var readHeight = await context.ContractEvents
            .CountAsync(cancellationToken: token);
        
        return readHeight;
    }

    internal sealed class InMemoryContractRepository : IContractRepository
    {
        private static readonly object Lock = new();
        private static InMemoryContractRepository? _inMemoryContractRepository;
        
        private readonly IList<ContractInitialized> _contractInitialized;
        
        /// <summary>
        /// Returns contract initialized event(s) for contracts. There should always be only one.
        ///
        /// <see cref="Application.Api.GraphQL.EfCore.Converters.Json.TransactionResultEventConverter"/> has event mapping.
        /// </summary>
        private const string ContractInitializedEventSql = @"
SELECT
    g0.block_height as BlockHeight,
    g0.transaction_index as TransactionIndex,
    g0.event_index as EventIndex,
    g0.contract_address_index as ContractAddressIndex,
    g0.contract_address_subindex as ContractAddressSubIndex,
    g0.block_slot_time as BlockSlotTime,
    g0.created_at as CreatedAt,
    g0.event as Event,
    g0.sender as Creator,
    g0.source as Source,
    g0.transaction_hash as TransactionHash
FROM graphql_contract_events AS g0
WHERE g0.event ->> 'tag' = '16' 
";
        
        private InMemoryContractRepository(IList<ContractInitialized> contractInitialized)
        {
            _contractInitialized = contractInitialized;
        }

        internal static InMemoryContractRepository Create(GraphQlDbContext context)
        {
            if (_inMemoryContractRepository != null)
            {
                return _inMemoryContractRepository;
            }

            lock (Lock)
            {
                if (_inMemoryContractRepository == null)
                {
                    var dbConnection = context.Database.GetDbConnection();
                    var initializations = dbConnection.Query<ContractEvent>(ContractInitializedEventSql)
                        .Select(e => (e.Event as ContractInitialized)!)
                        .ToList();
            
                    _inMemoryContractRepository = new InMemoryContractRepository(initializations);
                }
                return _inMemoryContractRepository;
            }
        }

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

        public Task<ContractInitialized> GetReadonlyContractInitializedEventAsync(ContractAddress contractAddress) => 
            Task.FromResult(_contractInitialized.First(c => c.ContractAddress == contractAddress));

        public Task AddAsync<T>(params T[] entities) where T : class
        {
            throw new NotImplementedException();
        }

        public Task AddRangeAsync<T>(IEnumerable<T> heights) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
    
    internal sealed class InMemoryModuleRepository : IModuleReadonlyRepository
    {
        private static readonly object ModuleRepositoryLock = new();
        private static InMemoryModuleRepository? _inMemoryModuleRepository;
        
        private readonly IList<ModuleReferenceEvent> _moduleReferenceEvents;
        private readonly IList<ModuleReferenceContractLinkEvent> _moduleReferenceContractLinkEvents;
        
        private InMemoryModuleRepository(
            IList<ModuleReferenceEvent> moduleReferenceEvents, 
            IList<ModuleReferenceContractLinkEvent> moduleReferenceContractLinkEvents)
        {
            _moduleReferenceEvents = moduleReferenceEvents;
            _moduleReferenceContractLinkEvents = moduleReferenceContractLinkEvents;
        }

        internal static InMemoryModuleRepository Create(GraphQlDbContext context)
        {
            if (_inMemoryModuleRepository != null)
            {
                return _inMemoryModuleRepository;
            }

            lock (ModuleRepositoryLock)
            {
                if (_inMemoryModuleRepository == null)
                {
                    var moduleReferenceContractLinkEvents = context.ModuleReferenceContractLinkEvents.AsNoTracking().ToList();
                    var moduleReferenceEvents = context.ModuleReferenceEvents.AsNoTracking().ToList();
                    _inMemoryModuleRepository = new InMemoryModuleRepository(moduleReferenceEvents, moduleReferenceContractLinkEvents);       
                }   
                return _inMemoryModuleRepository;
            }
        }
        
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task<ModuleReferenceEvent> GetModuleReferenceEventAsync(string moduleReference)
        {
            return Task.FromResult(_moduleReferenceEvents.First(m => m.ModuleReference == moduleReference));
        }

        public Task<ModuleReferenceEvent> GetModuleReferenceEventAtAsync(ContractAddress contractAddress, ulong blockHeight, ulong transactionIndex,
            uint eventIndex)
        {
            var link = _moduleReferenceContractLinkEvents
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
                .First();
            
            return Task.FromResult(_moduleReferenceEvents.First(m => m.ModuleReference == link.ModuleReference));
        }
    }
}
