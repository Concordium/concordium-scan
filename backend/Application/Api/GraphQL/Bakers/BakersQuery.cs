using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

[ExtendObjectType(typeof(Query))]
public class BakersQuery
{
    public Baker? GetBaker(GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Bakers
            .AsNoTracking()
            .SingleOrDefault(baker => baker.Id == id);
    }

    public Baker? GetBakerByBakerId(GraphQlDbContext dbContext, long bakerId)
    {
        return dbContext.Bakers
            .AsNoTracking()
            .SingleOrDefault(baker => baker.Id == bakerId);
    }

    [UsePaging]
    public IQueryable<Baker> GetBakers(
        GraphQlDbContext dbContext,
        BakerSort sort = BakerSort.BakerIdAsc,
        BakerFilter? filter = null)
    {
        var result = dbContext.Bakers
            .AsNoTracking();

        if (filter != null && filter.OpenStatusFilter != null)
            result = result.Where(x => x.ActiveState!.Pool!.OpenStatus == filter.OpenStatusFilter);

        if (filter != null
            && filter.IncludeRemoved != null
            && filter.IncludeRemoved == true)
        {
        }
        else
        {
            result = result.Where(b => b.RemovedState == null);
        }

        result = sort switch
        {
            BakerSort.BakerIdAsc => result.OrderBy(x => x.Id),
            BakerSort.BakerIdDesc => result.OrderByDescending(x => x.Id),
            BakerSort.BakerStakedAmountAsc => result.OrderBy(x => x.ActiveState.StakedAmount == null).ThenBy(x => x.ActiveState!.StakedAmount),
            BakerSort.BakerStakedAmountDesc => result.OrderBy(x => x.ActiveState.StakedAmount == null).ThenByDescending(x => x.ActiveState!.StakedAmount),
            BakerSort.TotalStakedAmountAsc => result.OrderBy(x => x.ActiveState.Pool.TotalStake == null).ThenBy(x => x.ActiveState!.Pool!.TotalStake),
            BakerSort.TotalStakedAmountDesc => result.OrderBy(x => x.ActiveState.Pool.TotalStake == null).ThenByDescending(x => x.ActiveState.Pool.TotalStake),
            BakerSort.DelegatorCountAsc => result.OrderBy(x => x.ActiveState.Pool.DelegatorCount == null).ThenBy(x => x.ActiveState!.Pool!.DelegatorCount),
            BakerSort.DelegatorCountDesc => result.OrderBy(x => x.ActiveState.Pool.DelegatorCount == null).ThenByDescending(x => x.ActiveState!.Pool!.DelegatorCount),
            BakerSort.BakerApy30DaysDesc => result.OrderBy(x => x.RemovedState.RemovedAt != null).ThenBy(x => x.PoolApys.Apy30Days.BakerApy == null).ThenByDescending(x => x.PoolApys!.Apy30Days.BakerApy),
            BakerSort.DelegatorApy30DaysDesc => result.OrderBy(x => x.RemovedState.RemovedAt != null).ThenBy(x => x.PoolApys.Apy30Days.DelegatorsApy == null).ThenByDescending(x => x.PoolApys!.Apy30Days.DelegatorsApy),
            BakerSort.BlockCommissionsAsc => result.OrderBy(x => x.ActiveState!.Pool!.PaydayStatus.CommissionRates.BakingCommission != null).ThenBy(x => x.ActiveState!.Pool!.PaydayStatus.CommissionRates.BakingCommission),
            BakerSort.BlockCommissionsDesc => result.OrderByDescending(x => x.ActiveState!.Pool!.PaydayStatus.CommissionRates.BakingCommission != null).ThenByDescending(x => x.ActiveState!.Pool!.PaydayStatus.CommissionRates.BakingCommission),
            _ => throw new NotImplementedException()
        };

        return result;
    }
}
