using System.Threading;
using System.Threading.Tasks;
using Application.Configurations;
using Application.Entities;
using Application.Jobs;
using Application.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Background service which executes migration jobs for main process.
///
/// When new jobs are added they should be dependency injected and added to the constructor of this service.
/// </summary>
internal sealed class MigrationJobsBackgroundService : BackgroundService
{
    private readonly IMainMigrationJobFinder _jobFinder;
    private readonly IJobRepository<MainMigrationJob> _mainMigrationJobRepository;
    private readonly FeatureFlagOptions _featureFlags;
    private readonly ILogger _logger;

    public MigrationJobsBackgroundService(
        IMainMigrationJobFinder jobFinder,
        IJobRepository<MainMigrationJob> mainMigrationJobRepository,
        IOptions<FeatureFlagOptions> featureFlagsOptions
    )
    {
        _jobFinder = jobFinder;
        _mainMigrationJobRepository = mainMigrationJobRepository;
        _featureFlags = featureFlagsOptions.Value;
        _logger = Log.ForContext<MigrationJobsBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = TraceContext.StartActivity(nameof(MigrationJobsBackgroundService));
        
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }

        var jobs = _jobFinder.GetJobs();

        try
        {
            await Task.WhenAll(jobs.Select(j => RunJob(j, stoppingToken)));
            
            _logger.Information($"{nameof(MigrationJobsBackgroundService)} done.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{nameof(MigrationJobsBackgroundService)} didn't succeed successfully due to exception.");
        }
    }

    private async Task RunJob(IMainMigrationJob job, CancellationToken token)
    {
        try
        {
            if (await _mainMigrationJobRepository.DoesExistingJobExist(job, token))
            {
                return;
            }
            
            await job.StartImport(token);

            await _mainMigrationJobRepository.SaveSuccessfullyExecutedJob(job, token);
            _logger.Information($"{job.GetUniqueIdentifier()} finished successfully.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{job.GetUniqueIdentifier()} didn't succeed successfully due to exception.");
            throw;
        }
    }
}
