using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Resilience;
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

                await using var contractRepository = new ContractRepository(context);
                await using var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

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
                        if (transactionResultEvent == null)
                        {
                            continue;
                        }
                        eventUpdates.Add(new Update(
                            transactionResultEvent,
                            DateTimeOffset.UtcNow,
                            (long)contractEvent.ContractAddressIndex,
                            (long)contractEvent.ContractAddressSubIndex,
                            (long)contractEvent.BlockHeight,
                            (long)contractEvent.TransactionIndex,
                            (int)contractEvent.EventIndex));
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
}
