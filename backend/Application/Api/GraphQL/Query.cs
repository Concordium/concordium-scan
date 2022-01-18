using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class Query
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
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public IQueryable<Block> GetBlocks([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(b => b.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Transaction? GetTransaction([ScopedService] GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefault(tx => tx.Id == id);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public IQueryable<Transaction> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(tx => tx.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    public SearchResult Search([ScopedService] GraphQlDbContext dbContext, string query)
    {
        if (long.TryParse(query, out var blockHeight))
        {
            return new SearchResult
            {
                Blocks = dbContext.Blocks.AsNoTracking().Where(block => block.BlockHeight == blockHeight)
            };
        }

        try
        {
            return new SearchResult
            {
                Blocks = dbContext.Blocks.AsNoTracking().Where(block => block.BlockHash == query).ToArray(),
                Transactions = dbContext.Transactions.AsNoTracking().Where(transaction => transaction.TransactionHash == query).ToArray()
            };
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException) // thrown if given query is not a valid block- or transaction hash
        {
            return new SearchResult();
        }
    }
}