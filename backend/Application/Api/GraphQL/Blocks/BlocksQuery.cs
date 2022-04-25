using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Blocks;

[ExtendObjectType(typeof(Query))]
public class BlocksQuery
{
    private const int DefaultPageSize = 20;

    [UseDbContext(typeof(GraphQlDbContext))]
    public Block? GetBlock([ScopedService] GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .SingleOrDefault(block => block.Id == id);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public Block? GetBlockByBlockHash([ScopedService] GraphQlDbContext dbContext, string blockHash)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .SingleOrDefault(block => block.BlockHash == blockHash);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize, ProviderName = "block_by_descending_id")]
    public IQueryable<Block> GetBlocks([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(b => b.Id);
    }
}