using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.PassiveDelegations;
using Application.Import;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class PassiveDelegationImportHandler
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public PassiveDelegationImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PaydayPassiveDelegationStakeSnapshot?> UpdatePassiveDelegation(
        DelegationUpdateResults delegationUpdateResults, BlockDataPayload payload,
        ImportState importState, BlockImportPaydayStatus importPaydayStatus, Block block)
    {
        PaydayPassiveDelegationStakeSnapshot? result = null;

        if (payload.BlockInfo.ProtocolVersion.AsInt() < 4) return result;
        
        await EnsureInitialized(importState);
            
        var delegatorCountDelta = delegationUpdateResults.DelegatorCountDeltas
            .SingleOrDefault(x => x.DelegationTarget == new PassiveDelegationTarget())?
            .DelegatorCountDelta ?? 0;

        var delegatedStake = await GetTotalStakedToPassiveDelegation();
        var delegatedStakePercentage = Math.Round((decimal)delegatedStake / payload.RewardStatus.TotalAmount.Value, 10);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var instance = await dbContext.PassiveDelegations.SingleAsync();
        instance.DelegatorCount += delegatorCountDelta;
        instance.DelegatedStake = delegatedStake;
        instance.DelegatedStakePercentage = delegatedStakePercentage;

        if (importPaydayStatus is FirstBlockAfterPayday)
        {
            var stakes = new PoolPaydayStakes
            {
                PayoutBlockId = block.Id,
                PoolId = -1,
                BakerStake = 0,
                DelegatedStake = (long)instance.CurrentPaydayDelegatedStake
            };
            dbContext.PoolPaydayStakes.Add(stakes);
                
            result = new PaydayPassiveDelegationStakeSnapshot((long)instance.CurrentPaydayDelegatedStake);

            var status = await payload.ReadPassiveDelegationPoolStatus();
            instance.CurrentPaydayDelegatedStake = status.CurrentPaydayDelegatedCapital.Value;
        }
            
        await dbContext.SaveChangesAsync();

        return result;
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
                Id = -1,
                DelegatorCount = 0,
                DelegatedStakePercentage = 0m,
                CurrentPaydayDelegatedStake = 0
            };
            dbContext.Add(instance);
            await dbContext.SaveChangesAsync();

            importState.PassiveDelegationAdded = true;
        }
    }
}