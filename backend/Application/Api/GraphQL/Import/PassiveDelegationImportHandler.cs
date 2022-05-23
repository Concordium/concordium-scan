using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.PassiveDelegations;
using Application.Import;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class PassiveDelegationImportHandler
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public PassiveDelegationImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task UpdatePassiveDelegation(DelegationUpdateResults delegationUpdateResults, BlockDataPayload payload,
        ImportState importState)
    {
        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            await EnsureInitialized(importState);
            
            var delegatorCountDelta = delegationUpdateResults.DelegatorCountDeltas
                .SingleOrDefault(x => x.DelegationTarget == new PassiveDelegationTarget())?
                .DelegatorCountDelta ?? 0;

            if (delegatorCountDelta != 0)
                await UpdatePassiveDelegation(delegatorCountDelta);
        }
    }

    private async Task UpdatePassiveDelegation(int delegatorCountDelta)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var instance = await dbContext.PassiveDelegations.SingleAsync();
        instance.DelegatorCount += delegatorCountDelta;
        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureInitialized(ImportState importState)
    {
        if (!importState.PassiveDelegationAdded)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var instance = new PassiveDelegation
            {
                DelegatorCount = 0
            };
            dbContext.Add(instance);
            await dbContext.SaveChangesAsync();

            importState.PassiveDelegationAdded = true;
        }
    }
}