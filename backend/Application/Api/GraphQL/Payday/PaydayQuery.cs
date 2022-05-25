using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Payday;

[ExtendObjectType(typeof(Query))]
public class PaydayQuery
{
    [UseDbContext(typeof(GraphQlDbContext))]
    public Task<PaydayStatus?> GetPaydayStatus([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.PaydayStatuses.AsNoTracking().SingleOrDefaultAsync();
    }
}