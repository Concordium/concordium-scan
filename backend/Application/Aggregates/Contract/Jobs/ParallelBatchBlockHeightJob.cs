using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Configurations;
using Application.Observability;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// This class is used for jobs which depends on a block height as input. From the block height input the
/// job will update any relevant events and update database. The import can be from both existing data in database
/// or from the nodes.
///
/// This class calls the input job in batches and parallel. Hence the order of block height processed is out of order.
/// Because of this the jobs are expected to be stateless.
/// </summary>
internal sealed class ParallelBatchBlockHeightJob<TStatelessJob> : IContractJob where TStatelessJob : IStatelessBlockHeightJobs
{
    
    private readonly IStatelessBlockHeightJobs _statelessJob;
    private readonly ILogger _logger;
    private readonly ContractAggregateOptions _contractAggregateOptions;
    private readonly JobOptions _jobOptions;

    public ParallelBatchBlockHeightJob(
        TStatelessJob statelessJob,
        IOptions<ContractAggregateOptions> options
        )
    {
        _statelessJob = statelessJob;
        _logger = Log.ForContext<ParallelBatchBlockHeightJob<TStatelessJob>>();
        _contractAggregateOptions = options.Value;
        var gotJobOptions = _contractAggregateOptions.Jobs.TryGetValue(GetUniqueIdentifier(), out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new JobOptions();     
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => _statelessJob.GetUniqueIdentifier();

    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => _statelessJob.ShouldNodeImportAwait();

    public async Task StartImport(CancellationToken token)
    {
        var fromBatch = 0;
        while (!token.IsCancellationRequested)
        {
            var finalHeight = await _statelessJob.GetMaximumHeight(token);

            if (finalHeight < fromBatch * _jobOptions.BatchSize)
            {
                break;
            }
            var toBatch = (int)(finalHeight / _jobOptions.BatchSize);

            var cycle = Parallel.ForEachAsync(
                Enumerable.Range(fromBatch, toBatch - fromBatch + 1),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _jobOptions.MaxParallelTasks
                },
                (height, batchToken) => RunBatch(height, batchToken));
                
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var metricUpdater = UpdateReadHeightMetric(cts.Token);

            await cycle;
            cts.Cancel();
            await metricUpdater;

            fromBatch = toBatch + 1;
        }
            
        _logger.Information($"Done with job {GetUniqueIdentifier()}");
    }

    private async Task UpdateReadHeightMetric(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _statelessJob.UpdateMetric(token);
                await Task.Delay(_contractAggregateOptions.MetricDelay, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Thrown from `Task.Delay` when token is cancelled. We don't want to have this rethrown but just
            // stop loop.
        }
    }
    
    /// <summary>
    /// Run a batch.
    /// </summary>
    private async ValueTask RunBatch(long height, CancellationToken token)
    {
        using var _ = TraceContext.StartActivity(GetUniqueIdentifier());
            
        var blockHeightTo = height * _jobOptions.BatchSize;
        var blockHeightFrom = Math.Max((height - 1) * _jobOptions.BatchSize + 1, 0);
        var affectedRows = await _statelessJob.BatchImportJob((ulong)blockHeightFrom, (ulong)blockHeightTo, token);

        if (affectedRows == 0)
        {
            return;
        };
        _logger.Information("Written heights {From} to {To} for job {Identifier}", blockHeightFrom, blockHeightTo, GetUniqueIdentifier());
    }
}
