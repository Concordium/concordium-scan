using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.PassiveDelegations;

public class PassiveDelegation
{
    [GraphQLIgnore] 
    public long Id { get; init; }
    
    public int DelegatorCount { get; set; }
    
    [GraphQLDescription("The total amount staked by delegators to passive delegation.")]
    public ulong DelegatedStake { get; set; }

    [GraphQLDescription("Total stake passively delegated as a percentage of all CCDs in existence.")]
    public decimal DelegatedStakePercentage { get; set; }

    [GraphQLIgnore]
    public ulong CurrentPaydayDelegatedStake { get; set; }

    [UsePaging(DefaultPageSize = 10)]
    public IQueryable<DelegationSummary> GetDelegators(GraphQlDbContext dbContext)
    {
        return dbContext.Accounts.AsNoTracking()
            .Where(x => x.Delegation!.DelegationTarget == new PassiveDelegationTarget())
            .OrderByDescending(x => x.Delegation!.StakedAmount)
            .Select(x => new DelegationSummary(x.CanonicalAddress, x.Delegation!.StakedAmount, x.Delegation.RestakeEarnings));
    }
    
    public CommissionRates GetCommissionRates(GraphQlDbContext dbContext)
    {
        var latestChainParameters = dbContext.ChainParameters
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        if (latestChainParameters != null && ChainParameters.TryGetPassiveCommissions(
                latestChainParameters,
                out var passiveFinalizationCommission,
                out var passiveBakingCommission,
                out var passiveTransactionCommission))
        {
            return new CommissionRates
            {
                TransactionCommission = passiveTransactionCommission!.Value,
                FinalizationCommission = passiveFinalizationCommission!.Value,
                BakingCommission = passiveBakingCommission!.Value
            };
        }

        throw new NotImplementedException("Cannot get commission rates for passive delegation for this version of chain parameters!");
    }
    
    [UsePaging(DefaultPageSize = 10, InferConnectionNameFromField = false, ProviderName = "payday_pool_reward_by_descending_index")]
    public IQueryable<PaydayPoolReward> GetPoolRewards(GraphQlDbContext dbContext)
    {
        var pool = new PassiveDelegationPoolRewardTarget();

        return dbContext.PaydayPoolRewards.AsNoTracking()
            .Where(x => x.Pool == pool)
            .OrderByDescending(x => x.Index);
    }
    
    public double? GetApy(ApyPeriod period)
    {
        return period switch
        {
            ApyPeriod.Last7Days => PoolApys?.Apy7Days.DelegatorsApy ?? null,
            ApyPeriod.Last30Days => PoolApys?.Apy30Days.DelegatorsApy ?? null,
            _ => throw new NotImplementedException()
        };
    }
    
    /// <summary>
    /// This property is there for loading the pool apys row from the database. The data
    /// is exposed elsewhere in the model to create a better and more meaningful model.
    /// </summary>
    [GraphQLIgnore] 
    public PoolApys? PoolApys { get; set; }
}
