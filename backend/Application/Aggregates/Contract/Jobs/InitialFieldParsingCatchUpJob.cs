using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Resilience;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

public sealed class InitialFieldParsingCatchUpJob : IStatelessBlockHeightJobs
{
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "InitialFieldParsingCatchUpJob";
    
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly ILogger _logger;
    private readonly ContractAggregateOptions _contractAggregateOptions;
    private long? _maximumHeight;
    
    public InitialFieldParsingCatchUpJob(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IOptions<ContractAggregateOptions> options
    )
    {
        _contextFactory = contextFactory;
        _logger = Log.ForContext<InitialFieldParsingCatchUpJob>();
        _contractAggregateOptions = options.Value;
    }
    
    public string GetUniqueIdentifier() => JobName;
    
    public async Task<long> GetMaximumHeight(CancellationToken token)
    {
        if (_maximumHeight != null)
        {
            return _maximumHeight.Value;
        }
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var readHeight = await context.ContractReadHeights
            .AsNoTracking()
            .OrderByDescending(b => b.BlockHeight)
            .FirstOrDefaultAsync(token);
        if (readHeight == null)
        {
            return 0;
        }

        _maximumHeight = (long)readHeight.BlockHeight;
        return _maximumHeight.Value;
    }

    public Task UpdateMetric(CancellationToken token) => Task.CompletedTask;
    
    public bool ShouldNodeImportAwait() => false;
    
    /// <summary>
    /// Updates <see cref="Application.Aggregates.Contract.Entities.ContractEvent"/> and <see cref="Application.Aggregates.Contract.Entities.ContractRejectEvent"/>
    /// with hexadecimal fields parsed. 
    /// </summary>
    public async Task<ulong> BatchImportJob(ulong heightFrom, ulong heightTo, CancellationToken token = default)
    {
        return await Policies.GetTransientPolicy<ulong>(_logger, _contractAggregateOptions.RetryCount, _contractAggregateOptions.RetryDelay)
            .ExecuteAsync(async () =>
            {
                _logger.Debug("Start parsing events from {HeightFrom} to {HeightTo}", heightFrom, heightTo);

                var context = await _contextFactory.CreateDbContextAsync(token);

                var contractRepository = new ContractRepository(context);

                var contractEvents = await context.ContractEvents
                    .Where(ce => heightFrom <= ce.BlockHeight && ce.BlockHeight <= heightTo)
                    .ToListAsync(token);

                foreach (var contractEvent in contractEvents
                             .Where(contractEvent => !contractEvent.IsParsed()))
                {
                    await contractEvent.ParseEvent(contractRepository);
                }

                var contractRejectEvents = await context.ContractRejectEvents
                    .Where(ce => heightFrom <= ce.BlockHeight && ce.BlockHeight <= heightTo)
                    .ToListAsync(token);

                foreach (var contractRejectEvent in contractRejectEvents
                             .Where(contractRejectEvent => !contractRejectEvent.IsParsed()))
                {
                    await contractRejectEvent.ParseEvent(contractRepository);
                }

                await context.SaveChangesAsync(token);

                _logger.Debug("Successfully parsed events from {HeightFrom} to {HeightTo}", heightFrom, heightTo);

                return (ulong)(contractEvents.Count + contractRejectEvents.Count);
            });
    }
}
