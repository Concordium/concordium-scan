using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.PassiveDelegations;

public class PassiveDelegation
{
    [GraphQLIgnore] 
    public int Id { get; init; }
    
    public int DelegatorCount { get; set; }
    
    [GraphQLDescription("The total amount staked by delegators to passive delegation.")]
    public ulong DelegatedStake { get; set; }

    [GraphQLDescription("Total stake passively delegated as a percentage of all CCDs in existence.")]
    public decimal DelegatedStakePercentage { get; set; }

    [GraphQLIgnore]
    public ulong CurrentPaydayDelegatedStake { get; set; }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10)]
    public IQueryable<DelegationSummary> GetDelegators([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Accounts.AsNoTracking()
            .Where(x => x.Delegation!.DelegationTarget == new PassiveDelegationTarget())
            .OrderByDescending(x => x.Delegation!.StakedAmount)
            .Select(x => new DelegationSummary(x.CanonicalAddress, x.Delegation!.StakedAmount, x.Delegation.RestakeEarnings));
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public CommissionRates GetCommissionRates([ScopedService] GraphQlDbContext dbContext)
    {
        var latestChainParameters = dbContext.ChainParameters
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        if (latestChainParameters is ChainParametersV1 chainParamsV1)
        {
            return new CommissionRates
            {
                TransactionCommission = chainParamsV1.PassiveTransactionCommission,
                FinalizationCommission = chainParamsV1.PassiveFinalizationCommission,
                BakingCommission = chainParamsV1.PassiveBakingCommission
            };
        }
        throw new NotImplementedException("Cannot get commission rates for passive delegation for this version of chain parameters!");
    }
    
    [GraphQLDeprecated("Use poolRewards instead. Will be removed in the near future")]
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "pool_reward_by_descending_index")]
    public IQueryable<PoolReward> GetRewards([ScopedService] GraphQlDbContext dbContext)
    {
        var pool = new PassiveDelegationPoolRewardTarget();

        return dbContext.PoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "payday_pool_reward_by_descending_index")]
    public IQueryable<PaydayPoolReward> GetPoolRewards([ScopedService] GraphQlDbContext dbContext)
    {
        var pool = new PassiveDelegationPoolRewardTarget();

        return dbContext.PaydayPoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
    
    public async Task<double?> GetApy([Service] ApyQuery query,  ApyPeriod period)
    {
        var result = await query.GetApy(new PassiveDelegationPoolRewardTarget(), period);
        return result.DelegatorsApy;
    }
}