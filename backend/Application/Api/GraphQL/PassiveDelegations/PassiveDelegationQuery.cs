using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.PassiveDelegations;

[ExtendObjectType(typeof(Query))]
public class PassiveDelegationQuery
{
    public PassiveDelegation? GetPassiveDelegation(GraphQlDbContext dbContext)
    {
        var result = dbContext.PassiveDelegations
            .AsNoTracking()
            .SingleOrDefault();

        return result;
    }
}
