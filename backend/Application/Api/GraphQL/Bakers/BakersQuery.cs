using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

[ExtendObjectType(typeof(Query))]
public class BakersQuery
{
    [UseDbContext(typeof(GraphQlDbContext))]
    public Baker? GetBaker([ScopedService] GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Bakers
            .AsNoTracking()
            .SingleOrDefault(baker => baker.Id == id);
    }
         
    [UseDbContext(typeof(GraphQlDbContext))]
    public Baker? GetBakerByBakerId([ScopedService] GraphQlDbContext dbContext, long bakerId)
    {
        return dbContext.Bakers
            .AsNoTracking()
            .SingleOrDefault(baker => baker.Id == bakerId);
    }
         
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Baker> GetBakers([ScopedService] GraphQlDbContext dbContext, BakerSort sort = BakerSort.BakerIdAsc, BakerFilter? filter = null)
    {
        var result = dbContext.Bakers
            .AsNoTracking();

        if (filter != null && filter.OpenStatusFilter != null)
            result = result.Where(x => x.ActiveState!.Pool!.OpenStatus == filter.OpenStatusFilter);
        
        result = sort switch
        {
            BakerSort.BakerIdAsc => result.OrderBy(x => x.Id),
            BakerSort.BakerIdDesc => result.OrderByDescending(x => x.Id),
            BakerSort.BakerStakedAmountAsc => result.OrderBy(x => x.ActiveState.StakedAmount != null).ThenBy(x => x.ActiveState!.StakedAmount),
            BakerSort.BakerStakedAmountDesc => result.OrderBy(x => x.ActiveState.StakedAmount != null).ThenByDescending(x => x.ActiveState!.StakedAmount),
            BakerSort.TotalStakedAmountAsc => result.OrderBy(x => x.ActiveState.Pool.TotalStake != null).ThenBy(x => x.ActiveState!.Pool!.TotalStake),
            BakerSort.TotalStakedAmountDesc => result.OrderBy(x => x.ActiveState.Pool.TotalStake != null).ThenByDescending(x => x.ActiveState.Pool.TotalStake),
            BakerSort.DelegatorCountAsc => result.OrderBy(x => x.ActiveState.Pool.DelegatorCount != null).ThenBy(x => x.ActiveState!.Pool!.DelegatorCount),
            BakerSort.DelegatorCountDesc => result.OrderBy(x => x.ActiveState.Pool.DelegatorCount != null).ThenByDescending(x => x.ActiveState!.Pool!.DelegatorCount),
            _ => throw new NotImplementedException()
        };

        return result;
    }
}