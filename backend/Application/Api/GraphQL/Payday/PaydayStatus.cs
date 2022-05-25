using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Payday;

public class PaydayStatus
{
    [GraphQLIgnore]
    public int Id { get; init; }
    
    public DateTimeOffset NextPaydayTime { get; set; }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10)]
    public IQueryable<PaydaySummary> GetPaydaySummaries([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.PaydaySummaries.AsNoTracking()
            .OrderByDescending(x => x.BlockId);
    }

}