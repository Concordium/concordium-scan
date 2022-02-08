using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Search;

[ExtendObjectType(typeof(Query))]
public class SearchQuery
{
    public async Task<SearchResult> Search([Service] IDbContextFactory<GraphQlDbContext> dbContextFactory, string query)
    {
        if (long.TryParse(query, out var blockHeight))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            return new SearchResult
            {
                Blocks = dbContext.Blocks.AsNoTracking()
                    .Where(block => block.BlockHeight == blockHeight)
                    .ToArray()
            };
        }

        await using var blocksDbContext = await dbContextFactory.CreateDbContextAsync();
        var blocksTask = blocksDbContext.Blocks.AsNoTracking()
            .Where(block => block.BlockHash == query)
            .ToArrayAsync();

        await using var transactionsDbContext = await dbContextFactory.CreateDbContextAsync();
        var transactionsTask = transactionsDbContext.Transactions.AsNoTracking()
            .Where(transaction => transaction.TransactionHash == query)
            .ToArrayAsync();

        await using var accountsDbContext = await dbContextFactory.CreateDbContextAsync();
        var accountsTask = accountsDbContext.Accounts.AsNoTracking()
            .Where(account => account.Address == query)
            .ToArrayAsync();

        await Task.WhenAll(blocksTask, transactionsTask, accountsTask);
        
        return new SearchResult
        {
            Blocks = blocksTask.Result,
            Transactions = transactionsTask.Result,
            Accounts = accountsTask.Result
        };
    }
}