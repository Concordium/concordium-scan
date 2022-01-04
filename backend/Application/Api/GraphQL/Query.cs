using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class Query
{
    private const int DefaultPageSize = 20;
    
    public Block? GetBlock([Service] GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext
            .Blocks
            .AsNoTracking()
            .SingleOrDefault(block => block.Id == id);
    }
    
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public IQueryable<Block> GetBlocks([Service] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(b => b.Id);
    }

    public Transaction? GetTransaction([Service] GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Transactions.SingleOrDefault(tx => tx.Id == id);
    }
    
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public IQueryable<Transaction> GetTransactions([Service] GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(tx => tx.Id);
    }

    public SearchResult Search([Service] GraphQlDbContext dbContext, string query)
    {
        if (long.TryParse(query, out var blockHeight))
        {
            return new SearchResult
            {
                Blocks = dbContext.Blocks.Where(block => block.BlockHeight == blockHeight)
            };
        }

        try
        {
            return new SearchResult
            {
                Blocks = dbContext.Blocks.Where(block => block.BlockHash == query).ToArray(),
                Transactions = dbContext.Transactions.Where(transaction => transaction.TransactionHash == query).ToArray()
            };
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException) // thrown if given query is not a valid block- or transaction hash
        {
            return new SearchResult();
        }
    }
}