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
        var isQueryNumeric = long.TryParse(query, out var queryNumeric);
            
        await using var blocksDbContext = await dbContextFactory.CreateDbContextAsync();
        var blocksTask = blocksDbContext.Blocks.AsNoTracking()
            .Where(block => block.BlockHash.StartsWith(query) || isQueryNumeric && block.BlockHeight == queryNumeric)
            .OrderByDescending(block => block.Id)
            .ToArrayAsync();

        await using var transactionsDbContext = await dbContextFactory.CreateDbContextAsync();
        var transactionsTask = transactionsDbContext.Transactions.AsNoTracking()
            .Where(transaction => transaction.TransactionHash.StartsWith(query))
            .OrderByDescending(transaction => transaction.Id)
            .ToArrayAsync();

        await using var accountsDbContext = await dbContextFactory.CreateDbContextAsync();
        var accountsTask = accountsDbContext.Accounts.AsNoTracking()
            .Where(account => account.Address.StartsWith(query))
            .OrderByDescending(account => account.Id)
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