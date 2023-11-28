using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Jobs;

internal interface IJobRepository<T> where T : class, IJobEntity<T>, new()
{
    /// <summary>
    /// Checks in storage if a job has been executed.
    ///
    /// If the job identifier is in the storage this function is expected to return true.
    /// Hence only successfully executed jobs should be placed in storage.
    /// </summary>
    Task<bool> DoesExistingJobExist(IJob job, CancellationToken token = default);

    /// <summary>
    /// Saves identifier of successfully executed job to storage.
    /// </summary>
    Task SaveSuccessfullyExecutedJob(IJob job, CancellationToken token = default);
}

internal class JobRepository<T> : IJobRepository<T> where T : class, IJobEntity<T>, new()
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public JobRepository(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    /// <inheritdoc/>
    public async Task<bool> DoesExistingJobExist(IJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var existingJob = await context.Set<T>()
            .AsNoTracking()
            .Where(j => j.Job == job.GetUniqueIdentifier())
            .FirstOrDefaultAsync(token);

        return existingJob != null;
    }
    
    /// <inheritdoc/>
    public async Task SaveSuccessfullyExecutedJob(IJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context
            .Set<T>()
            .AddAsync( new T {Job = job.GetUniqueIdentifier()}, token);
        await context.SaveChangesAsync(token);
    }
}
