using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Jobs;
using Application.Aggregates.Contract.Observability;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.BackgroundServices;

/// <summary>
/// Background service which starts contract data import data from nodes.
/// </summary>
internal class ContractNodeImportBackgroundService : BackgroundService
{
    private readonly IContractJobFinder _jobFinder;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IContractRepositoryFactory _repositoryFactory;
    private readonly IContractNodeClient _client;
    private readonly IFeatureFlags _featureFlags;
    private readonly ContractHealthCheck _healthCheck;
    private readonly ContractAggregateOptions _options;
    private readonly ILogger _logger;

    public ContractNodeImportBackgroundService(
        IContractJobFinder jobFinder,
        IDbContextFactory<GraphQlDbContext> dbContextFactory,
        IContractRepositoryFactory repositoryFactory,
        IContractNodeClient client,
        IFeatureFlags featureFlags,
        IOptions<ContractAggregateOptions> options,
        ContractHealthCheck healthCheck)
    {
        _jobFinder = jobFinder;
        _dbContextFactory = dbContextFactory;
        _repositoryFactory = repositoryFactory;
        _client = client;
        _featureFlags = featureFlags;
        _healthCheck = healthCheck;
        _options = options.Value;
        _logger = Log.ForContext<ContractNodeImportBackgroundService>();
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
        
            var contractAggregate = new ContractAggregate(_repositoryFactory, _options);
            
            _logger.Information($"{nameof(ContractNodeImportBackgroundService)} started.");
            await contractAggregate.NodeImportJob(_client, stoppingToken);
        }
        catch (Exception e)
        {
            _logger.Fatal(e, $"{nameof(ContractNodeImportBackgroundService)} stopped due to exception.");
            _healthCheck.AddUnhealthyJobWithMessage(nameof(ContractNodeImportBackgroundService), "Stopped due to exception.");
            _logger.Fatal(e, $"{nameof(ContractNodeImportBackgroundService)} stopped due to exception.");
        }
    }

    private async Task AwaitJobsAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            var jobsToAwait = await GetJobsToAwait(token);
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
        var contractJobs = _jobFinder.GetJobs()
            .Select(j => j.GetUniqueIdentifier())
            .ToList();
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var doneJobs = await context
            .ContractJobs
            .AsNoTracking()
            .Where(j => contractJobs.Contains(j.Job))
            .Select(j => j.Job)
            .ToListAsync(cancellationToken: token);
        
        return contractJobs.Except(doneJobs).ToList();
    }
}
