using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Database.MigrationJobs;

interface IMainMigrationJobRepository
{
    /// <summary>
    /// Checks in storage if a job has been executed.
    ///
    /// If the job identifier is in the storage this function is expected to return true.
    /// Hence only successfully executed jobs should be placed in storage.
    /// </summary>
    Task<bool> DoesExistingJobExist(IMainMigrationJob job, CancellationToken token = default);

    /// <summary>
    /// Saves identifier of successfully executed job to storage.
    /// </summary>
    Task SaveSuccessfullyExecutedJob(IMainMigrationJob job, CancellationToken token = default);
}

internal class MainMigrationJobRepository : IMainMigrationJobRepository
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public MainMigrationJobRepository(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    /// <inheritdoc/>
    public async Task<bool> DoesExistingJobExist(IMainMigrationJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var existingJob = await context.MainMigrationJobs
            .AsNoTracking()
            .Where(j => j.Job == job.GetUniqueIdentifier())
            .FirstOrDefaultAsync(token);

        return existingJob != null;
    }
    
    /// <inheritdoc/>
    public async Task SaveSuccessfullyExecutedJob(IMainMigrationJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.MainMigrationJobs.AddAsync(new MainMigrationJob(job.GetUniqueIdentifier()), token);
        await context.SaveChangesAsync(token);
    }
}
