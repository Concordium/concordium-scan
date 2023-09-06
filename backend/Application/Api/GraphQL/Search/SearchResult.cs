using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Network;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Search;
public class SearchResult
{
    private static readonly Regex HashRegex = new("^[a-fA-F0-9]{1,64}$");
    private static readonly Regex AccountAddressRegex = new("^[a-zA-Z0-9]{1,64}$");
    private static readonly Regex ContractAddressRegex = new Regex(@"^<?(\d{1,20})(?:,\s?(\d{0,20}))?>?$");
    private readonly string _queryString;
    private readonly long? _queryNumeric;

    /// <summary>
    /// Try match query with contract regex.
    /// 
    /// Only consider query if there is a match.
    /// </summary>
    internal static bool TryMatchContractPattern(
        string? query, out string? pattern)
    {
        const char end = '%';
        const char start = '<';
        
        pattern = null;
        if (query is null)
        {
            return false;
        }
        
        var match = ContractAddressRegex.Match(query);
        if (!match.Success) return false;

        if (!match.Groups[2].Success || string.IsNullOrEmpty(match.Groups[2].Value))
        {
            var firstSpan = match.Groups[1].ValueSpan;
            Span<char> patternSpan = stackalloc char[1 + firstSpan.Length + 1];
            patternSpan[0] = start;
            firstSpan.CopyTo(patternSpan[1..]);
            patternSpan[^1] = end;
            pattern = patternSpan.ToString();
        }
        else
        {
            ReadOnlySpan<char> section = stackalloc char[] { ',', ' ' };
            var firstSpan = match.Groups[1].ValueSpan;
            var secondSpan = match.Groups[2].ValueSpan;
            Span<char> patternSpan = stackalloc char[1 + firstSpan.Length + section.Length + secondSpan.Length + 1];
            patternSpan[0] = start;
            firstSpan.CopyTo(patternSpan[1..]);
            section.CopyTo(patternSpan[(1 + firstSpan.Length)..]);
            secondSpan.CopyTo(patternSpan[(1 + firstSpan.Length + section.Length)..]);
            patternSpan[^1] = end;
            pattern = patternSpan.ToString();
        }

        return true;
    }

    public SearchResult(string query)
    {
        _queryString = query;
        var isQueryNumeric = long.TryParse(query, out var queryNumeric);
        _queryNumeric = isQueryNumeric ? queryNumeric : null;
    }

    [UsePaging]
    public IQueryable<Contract> GetContracts(GraphQlDbContext context)
    {
        if (!TryMatchContractPattern(_queryString, out var pattern))
        {
            return new List<Contract>().AsQueryable();
        }
        
        return context.Contract
            .AsNoTracking()
            .Where(c => EF.Functions.Like(c.ContractAddress, pattern!))
            .OrderByDescending(c => c.ContractAddressIndex)
            .ThenByDescending(c => c.ContractAddressSubIndex);
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

        if (Concordium.Sdk.Types.AccountAddress.TryParse(_queryString, out var parsed))
        {
            // Valid (full) address given, search by base address to allow searching by an alias address
            var baseAddress = new AccountAddress(parsed!.GetBaseAddress().ToString());
            return dbContext.Accounts
                .AsNoTracking()
                .Where(account => account.BaseAddress == baseAddress);
        }
        
        // Cannot convert partial address to base address, so do a simple like-search on canonical address 
        return dbContext.Accounts.AsNoTracking()
            .Where(account => EF.Functions.Like((string)account.CanonicalAddress, _queryString + "%"))
            .OrderByDescending(account => account.Id);
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public async Task<IEnumerable<Baker>> GetBakers([ScopedService] GraphQlDbContext dbContext)
    {
        if (_queryNumeric.HasValue)
        {
            var result = await dbContext.Bakers
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == _queryNumeric.Value);

            if (result != null) 
                return new[] { result };
        }

        return Array.Empty<Baker>();
    }
    
    [UsePaging]
    public IEnumerable<NodeStatus> GetNodeStatuses([Service] NodeStatusSnapshot nodeSummarySnapshot)
    {
        return nodeSummarySnapshot.NodeStatuses
            .Where(x => x.NodeName != null && x.NodeName.Contains(_queryString, StringComparison.InvariantCultureIgnoreCase));
    }
}