using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Observability;
using Application.Configurations;
using Application.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.BackgroundServices;

/// <summary>
/// Background service which executes background jobs related to Contracts.
///
/// When new jobs are added they should be dependency injected and added to the constructor of this service.
/// </summary>
internal sealed class ContractJobsBackgroundService : BackgroundService
{
    private readonly IContractJobFinder _jobFinder;
    private readonly IJobRepository<ContractJob> _contractJobRepository;
    private readonly FeatureFlagOptions _featureFlags;
    private readonly ILogger _logger;

    public ContractJobsBackgroundService(
        IContractJobFinder jobFinder,
        IJobRepository<ContractJob> contractJobRepository,
        IOptions<FeatureFlagOptions> featureFlagsOptions
    )
    {
        _jobFinder = jobFinder;
        _contractJobRepository = contractJobRepository;
        _featureFlags = featureFlagsOptions.Value;
        _logger = Log.ForContext<ContractJobsBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = TraceContext.StartActivity(nameof(ContractJobsBackgroundService));
        
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }

        var jobs = _jobFinder.GetJobs();

        try
        {
            await Task.WhenAll(jobs.Select(j => RunJob(j, stoppingToken)));
            
            _logger.Information($"{nameof(ContractJobsBackgroundService)} done.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{nameof(ContractJobsBackgroundService)} didn't succeed successfully due to exception.");
        }
    }

    private async Task RunJob(IContractJob job, CancellationToken token)
    {
        try
        {
            if (await _contractJobRepository.DoesExistingJobExist(job, token))
            {
                return;
            }
            
            await job.StartImport(token);

            await _contractJobRepository.SaveSuccessfullyExecutedJob(job, token);
            _logger.Information($"{job.GetUniqueIdentifier()} finished successfully.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{job.GetUniqueIdentifier()} didn't succeed successfully due to exception.");
            throw;
        }
    }
}
