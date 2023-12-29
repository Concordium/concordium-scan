using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Payday;

public class PaydayStatus
{
    [GraphQLIgnore]
    public int Id { get; init; }
    
    public DateTimeOffset NextPaydayTime { get; set; }

    [GraphQLIgnore]
    public DateTimeOffset PaydayStartTime { get; set; }

    [GraphQLIgnore]
    public int? ProtocolVersion { get; set; }

    [UsePaging(DefaultPageSize = 10)]
    public IQueryable<PaydaySummary> GetPaydaySummaries(GraphQlDbContext dbContext)
    {
        return dbContext.PaydaySummaries.AsNoTracking()
            .OrderByDescending(x => x.BlockId);
    }

}
