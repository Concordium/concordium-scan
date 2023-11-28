using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application.Jobs;

public interface IJobFinder<out T> 
    where T : IJob
{
    /// <summary>
    /// Get jobs which inherits from <see cref="T"/>.
    /// </summary>
    IEnumerable<T> GetJobs();
    /// <summary>
    /// Awaits all jobs which inherits from <see cref="T"/>.
    /// </summary>
    /// <param name="token"></param>
    Task AwaitJobsAsync(CancellationToken token = default);
}

internal sealed class JobFinder<T, TEntity> : IJobFinder<T> 
    where T : IJob
    where TEntity : class, IJobEntity, new()
{
    private readonly IServiceProvider _provider;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly GeneralJobOption _options;
    private readonly ILogger _logger;

    public JobFinder(
        IServiceProvider provider, 
        IOptions<GeneralJobOption> options,
        IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _provider = provider;
        this._dbContextFactory = dbContextFactory;
        _options = options.Value;
        _logger = Log.ForContext<JobFinder<T, TEntity>>();
    }
    
    public IEnumerable<T> GetJobs()
    {
        return _provider.GetServices<T>();
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
    
    internal async Task<IList<string>> GetJobsToAwait(CancellationToken token = default)
    {
        var migrationJobs = GetJobs()
            .Where(j => j.ShouldNodeImportAwait())
            .Select(j => j.GetUniqueIdentifier())
            .ToList();
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var doneJobs = await context
            .Set<TEntity>()
            .AsNoTracking()
            .Where(j => migrationJobs.Contains(j.Job))
            .Select(j => j.Job)
            .ToListAsync(cancellationToken: token);
        
        return migrationJobs.Except(doneJobs).ToList();
    }
}
