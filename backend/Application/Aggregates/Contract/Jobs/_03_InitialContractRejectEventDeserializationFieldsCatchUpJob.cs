using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Configurations;
using Application.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

public sealed class InitialContractRejectEventDeserializationFieldsCatchUpJob : IStatelessJob
{
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    internal const string JobName = "InitialContractRejectEventDeserializationFieldsCatchUpJob";
    
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly ILogger _logger;
    private readonly ContractAggregateOptions _contractAggregateOptions;
    private readonly JobOptions _jobOptions;

    public InitialContractRejectEventDeserializationFieldsCatchUpJob(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IOptions<ContractAggregateOptions> options
    )
    {
        _contextFactory = contextFactory;
        _logger = Log.ForContext<InitialContractRejectEventDeserializationFieldsCatchUpJob>();
        _contractAggregateOptions = options.Value;
        var gotJobOptions = _contractAggregateOptions.Jobs.TryGetValue(GetUniqueIdentifier(), out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new JobOptions();    
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;
    
    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken)
    {
        var eventCount = await GetEventCount(cancellationToken);
        var batchCount = eventCount / _jobOptions.BatchSize + 1;
        
        _logger.Debug($"Should process {eventCount} contract reject events in {batchCount} batches");

        return Enumerable.Range(0, batchCount);
    }

    /// <inheritdoc/>
    public ValueTask Setup(CancellationToken token) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => false;
    
    /// <summary>
    /// Updates <see cref="Application.Aggregates.Contract.Entities.ContractRejectEvent"/> with hexadecimal fields parsed. 
    /// </summary>
    public async ValueTask Process(int identifier, CancellationToken token)
    {
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _contractAggregateOptions.RetryCount, _contractAggregateOptions.RetryDelay)
            .ExecuteAsync(async () =>
            {
                var take = _jobOptions.BatchSize;
                var skip = identifier * take;
                _logger.Debug($"Start parsing contract reject events in range {skip + 1} to {skip + take}");

                var context = await _contextFactory.CreateDbContextAsync(token);
                
                await using var moduleReadonlyRepository = new ModuleReadonlyRepository(await _contextFactory.CreateDbContextAsync(token));
                
                var contractRejectEvents = await context.ContractRejectEvents
                    .OrderBy(ce => ce.BlockHeight)
                    .ThenBy(ce => ce.TransactionIndex)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(token);

                foreach (var contractRejectEvent in contractRejectEvents
                             .Where(contractRejectEvent => !IsParsed(contractRejectEvent)))
                {
                    try
                    {
                        await contractRejectEvent.ParseEvent(moduleReadonlyRepository);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Exception when processing <{ContractAddressIndex},{ContractAddressSubIndex}> contract reject event at {BlockHeight}, {TransactionIndex}",
                            contractRejectEvent.ContractAddressIndex,
                            contractRejectEvent.ContractAddressSubIndex,
                            contractRejectEvent.BlockHeight,
                            contractRejectEvent.TransactionIndex);
                        throw;
                    }
                }
                
                await context.SaveChangesAsync(token);

                _logger.Debug($"Successfully parsed contract reject events in range {skip + 1} to {skip + take}");
            });
    }
    
    /// <summary>
    /// Check if <see cref="ContractRejectEvent"/> has been parsed.
    ///
    /// Also returns true if there is nothing to parse.
    /// </summary>
    private static bool IsParsed(ContractRejectEvent contractRejectEvent)
    {
        return contractRejectEvent.RejectedEvent switch
        {
            RejectedReceive rejectedReceive => rejectedReceive.Message != null,
            _ => true
        };
    }    
    
    private async Task<int> GetEventCount(CancellationToken token)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var readHeight = await context.ContractRejectEvents
            .CountAsync(cancellationToken: token);
        
        return readHeight;
    }
}
