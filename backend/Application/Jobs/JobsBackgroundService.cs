using System.Threading;
using System.Threading.Tasks;
using Application.Configurations;
using Application.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Application.Jobs;

/// <summary>
/// Background service which executes background jobs related to <see cref="TEntity"/>
/// from jobs which inherits from <see cref="TJob"/>.
///
/// When new jobs are added they should be dependency injected and added to the constructor of this service.
/// </summary>
internal sealed class JobsBackgroundService<TJob, TEntity> : BackgroundService
    where TJob : IJob
    where TEntity : class, IJobEntity, new()
{
    private readonly IJobFinder<TJob> _jobFinder;
    private readonly IJobRepository<TEntity> _jobRepository;
    private readonly JobHealthCheck _jobHealthCheck;
    private readonly FeatureFlagOptions _featureFlags;
    private readonly ILogger _logger;

    public JobsBackgroundService(
        IJobFinder<TJob> jobFinder,
        IJobRepository<TEntity> jobRepository,
        IOptions<FeatureFlagOptions> featureFlagsOptions,
        JobHealthCheck jobHealthCheck
    )
    {
        _jobFinder = jobFinder;
        _jobRepository = jobRepository;
        _jobHealthCheck = jobHealthCheck;
        _featureFlags = featureFlagsOptions.Value;
        _logger = Log.ForContext<JobsBackgroundService<TJob, TEntity>>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = TraceContext.StartActivity(nameof(JobsBackgroundService<TJob, TEntity>));
        
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }

        var jobs = _jobFinder.GetJobs();

        try
        {
            await Task.WhenAll(jobs.Select(j => RunJob(j, stoppingToken)));
            
            _logger.Information($"{nameof(JobsBackgroundService<TJob, TEntity>)} done.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{nameof(JobsBackgroundService<TJob, TEntity>)} didn't succeed successfully due to exception.");
        }
    }

    private async Task RunJob(TJob job, CancellationToken token)
    {
        try
        {
            if (await _jobRepository.DoesExistingJobExist(job, token))
            {
                _logger.Information($"{job.GetUniqueIdentifier()} already done.");
                return;
            }
            _logger.Information($"{job.GetUniqueIdentifier()} starts.");
            
            using var _ = TraceContext.StartActivity(job.GetUniqueIdentifier());
            using var __ = LogContext.PushProperty("Job", job.GetUniqueIdentifier());
            
            await job.StartImport(token);

            await _jobRepository.SaveSuccessfullyExecutedJob(job, token);
            _logger.Information($"{job.GetUniqueIdentifier()} finished successfully.");
        }
        catch (Exception e)
        {
            _jobHealthCheck.AddUnhealthyJobWithMessage(job.GetUniqueIdentifier(), "Job stopped due to exception.");
            _logger.Fatal(e, $"{job.GetUniqueIdentifier()} stopped due to exception.");_logger.Error(e, $"{job.GetUniqueIdentifier()} didn't succeed successfully due to exception.");
            throw;
        }
    }
}
