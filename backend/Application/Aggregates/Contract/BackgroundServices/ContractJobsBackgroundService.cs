using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Application.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Aggregates.Contract.BackgroundServices;

/// <summary>
/// Background service which executes background jobs related to Contracts.
///
/// When new jobs are added they should be dependency injected and added to the constructor of this service.
/// </summary>
internal sealed class ContractJobsBackgroundService : BackgroundService
{
    private readonly IContractJobFinder _jobFinder;
    private readonly IFeatureFlags _featureFlags;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public ContractJobsBackgroundService(
        IContractJobFinder jobFinder,
        IFeatureFlags featureFlags, 
        IDbContextFactory<GraphQlDbContext> dbContextFactory
    )
    {
        _jobFinder = jobFinder;
        _featureFlags = featureFlags;
        _dbContextFactory = dbContextFactory;
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
            if (await DoesExistingJobExist(job, token))
            {
                return;
            }
            
            await job.StartImport(token);

            await SaveSuccessfullyExecutedJob(job, token);
            _logger.Information($"{job.GetUniqueIdentifier()} finished successfully.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{job.GetUniqueIdentifier()} didn't succeed successfully due to exception.");
            throw;
        }
    }

    internal async Task<bool> DoesExistingJobExist(IContractJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var existingJob = await context.ContractJobs
            .AsNoTracking()
            .Where(j => j.Job == job.GetUniqueIdentifier())
            .FirstOrDefaultAsync(token);

        return existingJob != null;
    }

    internal async Task SaveSuccessfullyExecutedJob(IContractJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.ContractJobs.AddAsync(new ContractJob(job.GetUniqueIdentifier()), token);
        await context.SaveChangesAsync(token);
    }
}