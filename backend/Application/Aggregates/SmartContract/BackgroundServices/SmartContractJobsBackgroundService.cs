using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.SmartContract.Entities;
using Application.Aggregates.SmartContract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Aggregates.SmartContract.BackgroundServices;

/// <summary>
/// Background service which executes background jobs related to Smart Contracts.
///
/// When new jobs are added they should be dependency injected and added to the constructor of this service.
/// </summary>
internal sealed class SmartContractJobsBackgroundService : BackgroundService
{
    private readonly ISmartContractJobFinder _jobFinder;
    private readonly IFeatureFlags _featureFlags;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public SmartContractJobsBackgroundService(
        ISmartContractJobFinder jobFinder,
        IFeatureFlags featureFlags, 
        IDbContextFactory<GraphQlDbContext> dbContextFactory
    )
    {
        _jobFinder = jobFinder;
        _featureFlags = featureFlags;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<SmartContractJobsBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }

        var jobs = _jobFinder.GetJobs();

        try
        {
            await Task.WhenAll(jobs.Select(j => RunJob(j, stoppingToken)));
            
            _logger.Information($"{nameof(SmartContractJobsBackgroundService)} done.");
        }
        catch (Exception e)
        {
            _logger.Error(e, $"{nameof(SmartContractJobsBackgroundService)} didn't succeed successfully due to exception.");
        }
    }

    private async Task RunJob(ISmartContractJob job, CancellationToken token)
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

    internal async Task<bool> DoesExistingJobExist(ISmartContractJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var existingJob = await context.SmartContractJobs
            .AsNoTracking()
            .Where(j => j.Job == job.GetUniqueIdentifier())
            .FirstOrDefaultAsync(token);

        return existingJob != null;
    }

    internal async Task SaveSuccessfullyExecutedJob(ISmartContractJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.SmartContractJobs.AddAsync(new SmartContractJob(job.GetUniqueIdentifier()), token);
        await context.SaveChangesAsync(token);
    }
}