using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Configurations;
using Application.Observability;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

internal sealed class ParallelBatchJob<TStatelessJob> : IContractJob where TStatelessJob : IStatelessJob
{
    private readonly TStatelessJob _statelessJob;
    private readonly ILogger _logger;
    private readonly JobOptions _jobOptions;
    private readonly JobHealthCheck _jobHealthCheck;
    
    public ParallelBatchJob(
        TStatelessJob statelessJob,
        IOptions<ContractAggregateOptions> options,
        JobHealthCheck jobHealthCheck
        )
    {
        _statelessJob = statelessJob;
        _logger = Log.ForContext<InitialContractRejectEventDeserializationFieldsCatchUpJob>();
        _jobHealthCheck = jobHealthCheck;
        var gotJobOptions = options.Value.Jobs.TryGetValue(GetUniqueIdentifier(), out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new JobOptions();
    }
    
    public async Task StartImport(CancellationToken token)
    {
        _logger.Information($"Start processing {GetUniqueIdentifier()}");
        var batches = await _statelessJob.GetIdentifierSequence(token);

        await Parallel.ForEachAsync(batches,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _jobOptions.MaxParallelTasks
            }, _statelessJob.Process);
            
        _logger.Information($"Done with job {GetUniqueIdentifier()}");
    }

    public string GetUniqueIdentifier() => _statelessJob.GetUniqueIdentifier();

    public bool ShouldNodeImportAwait() => _statelessJob.ShouldNodeImportAwait();
}
