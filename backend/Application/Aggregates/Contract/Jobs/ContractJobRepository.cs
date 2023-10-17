using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Jobs;

interface IContractJobRepository
{
    /// <summary>
    /// Checks in storage if a job has been executed.
    ///
    /// If the job identifier is in the storage this function is expected to return true.
    /// Hence only successfully executed jobs should be placed in storage.
    /// </summary>
    Task<bool> DoesExistingJobExist(IContractJob job, CancellationToken token = default);

    /// <summary>
    /// Saves identifier of successfully executed job to storage.
    /// </summary>
    Task SaveSuccessfullyExecutedJob(IContractJob job, CancellationToken token = default);
}

internal class ContractJobRepository : IContractJobRepository
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public ContractJobRepository(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    /// <inheritdoc/>
    public async Task<bool> DoesExistingJobExist(IContractJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var existingJob = await context.ContractJobs
            .AsNoTracking()
            .Where(j => j.Job == job.GetUniqueIdentifier())
            .FirstOrDefaultAsync(token);

        return existingJob != null;
    }
    
    /// <inheritdoc/>
    public async Task SaveSuccessfullyExecutedJob(IContractJob job, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.ContractJobs.AddAsync(new ContractJob(job.GetUniqueIdentifier()), token);
        await context.SaveChangesAsync(token);
    }
}
