using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.SmartContract.Configurations;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.SmartContract.BackgroundServices;

/// <summary>
/// Background service which starts smart data import data from nodes.
/// </summary>
internal class SmartContractNodeImportBackgroundService : BackgroundService
{
    private readonly SmartContractJobsBackgroundService _jobsBackgroundService;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ISmartContractNodeClient _client;
    private readonly IFeatureFlags _featureFlags;
    private readonly SmartContractAggregateOptions _options;
    private readonly ILogger _logger;

    public SmartContractNodeImportBackgroundService(
        SmartContractJobsBackgroundService jobsBackgroundService,
        IDbContextFactory<GraphQlDbContext> dbContextFactory,
        ISmartContractRepositoryFactory repositoryFactory,
        ISmartContractNodeClient client,
        IFeatureFlags featureFlags,
        IOptions<SmartContractAggregateOptions> options)
    {
        _jobsBackgroundService = jobsBackgroundService;
        _dbContextFactory = dbContextFactory;
        _repositoryFactory = repositoryFactory;
        _client = client;
        _featureFlags = featureFlags;
        _options = options.Value;
        _logger = Log.ForContext<SmartContractNodeImportBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }

        try
        {
            await AwaitJobsAsync(stoppingToken);
        
            var smartContractAggregate = new SmartContractAggregate(_repositoryFactory);
            
            _logger.Information($"{nameof(SmartContractNodeImportBackgroundService)} started.");
            await smartContractAggregate.NodeImportJob(_client, stoppingToken);
        }
        catch (Exception e)
        {
            _logger.Fatal(e, $"{nameof(SmartContractNodeImportBackgroundService)} stopped due to exception.");
            // TODO: Set health state to non healthy
        }
    }

    private async Task AwaitJobsAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            var jobsToAwait = await GetJobsToAwait();
            if (jobsToAwait.Count == 0)
            {
                break;
            }
            
            foreach (var job in jobsToAwait)
            {
                _logger.Information($"Awaiting job {job}");
            }

            await Task.Delay(_options.JobDelay, token);
        }
    }

    internal async Task<IList<string>> GetJobsToAwait(CancellationToken token = default)
    {
        var smartContractJobs = _jobsBackgroundService.GetJobs()
            .Select(j => j.GetUniqueIdentifier())
            .ToList();
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var doneJobs = await context
            .SmartContractJobs
            .AsNoTracking()
            .Where(j => smartContractJobs.Contains(j.Job))
            .Select(j => j.Job)
            .ToListAsync(cancellationToken: token);
        
        return smartContractJobs.Except(doneJobs).ToList();
    }
}