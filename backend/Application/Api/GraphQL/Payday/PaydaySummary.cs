using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Payday;

public class PaydaySummary
{
    [GraphQLIgnore]
    public long BlockId { get; init; }
    
    [GraphQLIgnore]
    public DateTimeOffset PaydayTime { get; init; }

    [GraphQLIgnore]
    public long PaydayDurationSeconds { get; init; }
    
    public Block GetBlock(GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .Single(block => block.Id == BlockId);
    }
}
