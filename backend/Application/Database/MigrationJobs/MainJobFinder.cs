using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application.Database.MigrationJobs;

public interface IMainMigrationJobFinder
{
    IEnumerable<IMainMigrationJob> GetJobs();
    Task AwaitJobsAsync(CancellationToken token = default);
}

public sealed class MainMigrationJobFinder : IMainMigrationJobFinder
{
    private readonly IServiceProvider _provider;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly MainMigrationJobOptions _options;
    private readonly ILogger _logger;

    public MainMigrationJobFinder(
        IServiceProvider provider, 
        IOptions<MainMigrationJobOptions> options,
        IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _provider = provider;
        this._dbContextFactory = dbContextFactory;
        _options = options.Value;
        _logger = Log.ForContext<MainMigrationJobFinder>();
    }
    
    public async Task AwaitJobsAsync(CancellationToken token = default)
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
    
    public IEnumerable<IMainMigrationJob> GetJobs()
    {
        return _provider.GetServices<IMainMigrationJob>();
    }

    internal async Task<IList<string>> GetJobsToAwait(CancellationToken token = default)
    {
        var migrationJobs = GetJobs()
            .Where(j => j.ShouldNodeImportAwait())
            .Select(j => j.GetUniqueIdentifier())
            .ToList();
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var doneJobs = await context
            .MainMigrationJobs
            .AsNoTracking()
            .Where(j => migrationJobs.Contains(j.Job))
            .Select(j => j.Job)
            .ToListAsync(cancellationToken: token);
        
        return migrationJobs.Except(doneJobs).ToList();
    }
}
