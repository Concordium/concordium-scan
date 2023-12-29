using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Blocks;

[ExtendObjectType(typeof(Query))]
public class BlocksQuery
{
    private const int DefaultPageSize = 20;

    public Block? GetBlock(GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .SingleOrDefault(block => block.Id == id);
    }
    
    public Block? GetBlockByBlockHash(GraphQlDbContext dbContext, string blockHash)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .SingleOrDefault(block => block.BlockHash == blockHash);
    }
    
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize, ProviderName = "block_by_descending_id")]
    public IQueryable<Block> GetBlocks(GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(b => b.Id);
    }
}
