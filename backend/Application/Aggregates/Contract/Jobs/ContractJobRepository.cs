using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Jobs;

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