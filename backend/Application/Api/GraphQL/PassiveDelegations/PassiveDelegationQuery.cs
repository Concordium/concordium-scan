using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.PassiveDelegations;

[ExtendObjectType(typeof(Query))]
public class PassiveDelegationQuery
{
    [UseDbContext(typeof(GraphQlDbContext))]
    public PassiveDelegation? GetPassiveDelegation([ScopedService] GraphQlDbContext dbContext)
    {
        var latestChainParameters = dbContext.ChainParameters
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        if (latestChainParameters is ChainParametersV1 chainParamsV1)
        {
            var commissionRates = new CommissionRates
            {
                TransactionCommission = chainParamsV1.PassiveTransactionCommission,
                FinalizationCommission = chainParamsV1.PassiveFinalizationCommission,
                BakingCommission = chainParamsV1.PassiveBakingCommission
            };

            return new PassiveDelegation(commissionRates);
        }
        else
        {
            return null;
        }
    }
    
}