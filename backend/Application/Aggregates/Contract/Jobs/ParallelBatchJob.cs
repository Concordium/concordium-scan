using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Observability;
using Application.Observability;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Application.Aggregates.Contract.Jobs;

internal sealed class ParallelBatchJob<TStatelessJob> : IContractJob where TStatelessJob : IStatelessJob
{
    private readonly TStatelessJob _statelessJob;
    private readonly ILogger _logger;
    private readonly ContractAggregateJobOptions _jobOptions;
    private readonly ContractHealthCheck _healthCheck;
    
    public ParallelBatchJob(
        TStatelessJob statelessJob,
        IOptions<ContractAggregateOptions> options,
        ContractHealthCheck healthCheck
        )
    {
        _statelessJob = statelessJob;
        _logger = Log.ForContext<InitialContractRejectEventDeserializationFieldsCatchUpJob>();
        _healthCheck = healthCheck;
        var gotJobOptions = options.Value.Jobs.TryGetValue(GetUniqueIdentifier(), out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new ContractAggregateJobOptions();
    }
    
    public async Task StartImport(CancellationToken token)
    {
        using var _ = TraceContext.StartActivity(GetUniqueIdentifier());
        using var __ = LogContext.PushProperty("Job", GetUniqueIdentifier());
        
        try
        {
            _logger.Information($"Start processing {GetUniqueIdentifier()}");
            var batches = await _statelessJob.GetIdentifierSequence(token);

            var cycle = Parallel.ForEachAsync(batches,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _jobOptions.MaxParallelTasks
                }, _statelessJob.Process);
            await cycle;
            
            _logger.Information($"Done with job {GetUniqueIdentifier()}");
        }
        catch (Exception e)
        {
            _healthCheck.AddUnhealthyJobWithMessage(GetUniqueIdentifier(), "Job stopped due to exception.");
            _logger.Fatal(e, $"{GetUniqueIdentifier()} stopped due to exception.");
            throw;
        }
    }

    public string GetUniqueIdentifier() => _statelessJob.GetUniqueIdentifier();

    public bool ShouldNodeImportAwait() => _statelessJob.ShouldNodeImportAwait();
}
