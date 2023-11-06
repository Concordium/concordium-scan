using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Resilience;
using Application.Api.GraphQL.EfCore;
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
    public async Task<IEnumerable<int>> GetBatches(CancellationToken cancellationToken)
    {
        var eventCount = await GetEventCount(cancellationToken);
        var batchCount = eventCount / _jobOptions.BatchSize + 1;
        
        _logger.Debug($"Should process {eventCount} contract events in {batchCount} batches");

        return Enumerable.Range(0, batchCount);
    }
    
    /// <summary>
    /// Updates <see cref="Application.Aggregates.Contract.Entities.ContractEvent"/> with hexadecimal fields parsed. 
    /// </summary>
    public async ValueTask Process(int batch, CancellationToken token)
    {
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _contractAggregateOptions.RetryCount, _contractAggregateOptions.RetryDelay)
            .ExecuteAsync(async () =>
            {
                var take = _jobOptions.BatchSize;
                var skip = batch * take;
                
                _logger.Debug("Start parsing events skip {Skip} and take to {Last}", skip, skip + take);

                var context = await _contextFactory.CreateDbContextAsync(token);

                await using var contractRepository = new ContractRepository(context);
                await using var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

                var contractEvents = await context.ContractEvents
                    .OrderBy(ce => ce.BlockHeight)
                    .ThenBy(ce => ce.TransactionIndex)
                    .ThenBy(ce => ce.EventIndex)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(token);

                foreach (var contractEvent in contractEvents
                             .Where(contractEvent => !contractEvent.IsHexadecimalFieldsParsed()))
                {
                    try
                    {
                        await contractEvent.ParseEvent(contractRepository, moduleReadonlyRepository);
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
                
                await context.SaveChangesAsync(token);

                _logger.Debug("Successfully parsed events from {Skip} to {Last}", skip, skip + take);
            });
    }    
    
    private async Task<int> GetEventCount(CancellationToken token)
    {
        
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var readHeight = await context.ContractEvents
            .CountAsync(cancellationToken: token);
        
        return readHeight;
    }
}
