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

            var delegatedStake = await GetTotalStakedToPassiveDelegation();
            var delegatedStakePercentage = Math.Round((decimal)delegatedStake / payload.RewardStatus.TotalAmount.MicroCcdValue, 10);
            await UpdatePassiveDelegation(delegatorCountDelta, delegatedStake, delegatedStakePercentage);
        }
    }

    private async Task UpdatePassiveDelegation(int delegatorCountDelta, ulong delegatedStake, decimal delegatedStakePercentage)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var instance = await dbContext.PassiveDelegations.SingleAsync();
        instance.DelegatorCount += delegatorCountDelta;
        instance.DelegatedStake = delegatedStake;
        instance.DelegatedStakePercentage = delegatedStakePercentage;
        await dbContext.SaveChangesAsync();
    }
    
    private async Task<ulong> GetTotalStakedToPassiveDelegation()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var result = await dbContext.Accounts.AsNoTracking()
            .Where(x => x.Delegation!.DelegationTarget == new PassiveDelegationTarget())
            .SumAsync(x => (long)x.Delegation!.StakedAmount);
        
        return (ulong)result;
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