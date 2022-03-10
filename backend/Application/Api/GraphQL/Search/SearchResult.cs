using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Search;

public class SearchResult
{
    private readonly string _queryString;
    private readonly long? _queryNumeric;

    public SearchResult(string query)
    {
        _queryString = query;
        var isQueryNumeric = long.TryParse(query, out var queryNumeric);
        _queryNumeric = isQueryNumeric ? queryNumeric : null;
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Block> GetBlocks([ScopedService] GraphQlDbContext dbContext)
    {
        if (string.IsNullOrEmpty(_queryString)) return new List<Block>().AsQueryable();
        
        var lowerCaseQuery = _queryString.ToLowerInvariant();
        return dbContext.Blocks.AsNoTracking()
            .Where(block => block.BlockHash.StartsWith(lowerCaseQuery) ||
                            _queryNumeric.HasValue && block.BlockHeight == _queryNumeric.Value)
            .OrderByDescending(block => block.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Transaction> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        if (string.IsNullOrEmpty(_queryString)) return new List<Transaction>().AsQueryable();

        var lowerCaseQuery = _queryString.ToLowerInvariant();
        return dbContext.Transactions.AsNoTracking()
            .Where(transaction => transaction.TransactionHash.StartsWith(lowerCaseQuery))
            .OrderByDescending(transaction => transaction.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Account> GetAccounts([ScopedService] GraphQlDbContext dbContext)
    {
        if (string.IsNullOrEmpty(_queryString)) return new List<Account>().AsQueryable();

        return dbContext.Accounts.AsNoTracking()
            .Where(account => account.CanonicalAddress.StartsWith(_queryString))
            .OrderByDescending(account => account.Id);
    }
}