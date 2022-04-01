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
    [UsePaging]  // TODO: Paging in a stable manner regarding baker id ascending  
    public IQueryable<Baker> GetBakers([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Bakers
            .AsNoTracking()
            .OrderBy(x => x.Id);
    }
}