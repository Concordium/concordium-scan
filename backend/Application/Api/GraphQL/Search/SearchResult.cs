using System.Text.RegularExpressions;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Search;
public class SearchResult
{
    private static readonly Regex HashRegex = new("^[a-fA-F0-9]{1,64}$");
    private static readonly Regex AccountAddressRegex = new("^[a-zA-Z0-9]{1,64}$");
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
        if (string.IsNullOrEmpty(_queryString) || !HashRegex.IsMatch(_queryString)) 
            return new List<Block>().AsQueryable();
        
        var lowerCaseQuery = _queryString.ToLowerInvariant() + "%";
        return dbContext.Blocks.AsNoTracking()
            .Where(block => EF.Functions.Like(block.BlockHash, lowerCaseQuery) ||
                            _queryNumeric.HasValue && block.BlockHeight == _queryNumeric.Value)
            .OrderByDescending(block => block.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Transaction> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        if (string.IsNullOrEmpty(_queryString) || !HashRegex.IsMatch(_queryString)) 
            return new List<Transaction>().AsQueryable();

        var lowerCaseQuery = _queryString.ToLowerInvariant() + "%";
        return dbContext.Transactions.AsNoTracking()
            .Where(transaction => EF.Functions.Like(transaction.TransactionHash, lowerCaseQuery))
            .OrderByDescending(transaction => transaction.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Account> GetAccounts([ScopedService] GraphQlDbContext dbContext)
    {
        if (string.IsNullOrEmpty(_queryString) || !AccountAddressRegex.IsMatch(_queryString)) 
            return new List<Account>().AsQueryable();

        if (ConcordiumSdk.Types.AccountAddress.TryParse(_queryString, out var parsed))
        {
            // Valid (full) address given, search by base address to allow searching by an alias address
            var baseAddress = new AccountAddress(parsed!.GetBaseAddress().AsString);
            return dbContext.Accounts
                .AsNoTracking()
                .Where(account => account.BaseAddress == baseAddress);
        }
        
        // Cannot convert partial address to base address, so do a simple like-search on canonical address 
        return dbContext.Accounts.AsNoTracking()
            .Where(account => EF.Functions.Like(account.CanonicalAddress, _queryString + "%"))
            .OrderByDescending(account => account.Id);
    }
}