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
    public IQueryable<Baker> GetBakers([ScopedService] GraphQlDbContext dbContext, BakerSort sort = BakerSort.StakedAmountDesc)
    {
        var result = dbContext.Bakers
            .AsNoTracking();
        
        return sort switch
        {
            BakerSort.BakerIdAsc => result.OrderBy(x => x.Id),
            BakerSort.BakerIdDesc => result.OrderByDescending(x => x.Id),
            BakerSort.StakedAmountAsc => result.OrderBy(x => x.ActiveState != null ? (long)x.ActiveState.StakedAmount : -1),
            BakerSort.StakedAmountDesc => result.OrderByDescending(x => x.ActiveState != null ? (long)x.ActiveState.StakedAmount : -1),
            _ => throw new NotImplementedException()
        };
    }
}