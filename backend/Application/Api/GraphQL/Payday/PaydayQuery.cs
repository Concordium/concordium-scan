using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Payday;

[ExtendObjectType(typeof(Query))]
public class PaydayQuery
{
    public Task<PaydayStatus?> GetPaydayStatus(GraphQlDbContext dbContext)
    {
        return dbContext.PaydayStatuses.AsNoTracking().SingleOrDefaultAsync();
    }
}
