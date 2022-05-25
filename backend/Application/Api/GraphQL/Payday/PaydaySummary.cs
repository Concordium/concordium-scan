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
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public Block GetBlock([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .Single(block => block.Id == BlockId);
    }
}