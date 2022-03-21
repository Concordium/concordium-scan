using System.Threading.Tasks;
using Application.Database;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public interface IAccountLookup
{
    public Task<IDictionary<string, long?>> GetAccountIdsFromBaseAddressesAsync(IEnumerable<string> accountBaseAddresses);
    void AddToCache(string baseAddress, long? accountId);
}

public class AccountLookup : IAccountLookup
{
    private readonly IMemoryCache _cache;
    private readonly DatabaseSettings _dbSettings;
    private readonly ILogger _logger;

    public AccountLookup(IMemoryCache cache, DatabaseSettings dbSettings)
    {
        _cache = cache;
        _dbSettings = dbSettings;
        _logger = Log.ForContext(GetType());
    }

    public async Task<IDictionary<string, long?>> GetAccountIdsFromBaseAddressesAsync(IEnumerable<string> accountBaseAddresses)
    {
        var result = new List<LookupResult>();
        var notCached = new List<string>();
        
        foreach (var accountBaseAddress in accountBaseAddresses)
        {
            if (_cache.TryGetValue<long?>(accountBaseAddress, out var cached))
                result.Add(new LookupResult(accountBaseAddress, cached));
            else
                notCached.Add(accountBaseAddress);
        }

        if (notCached.Count > 0)
        {
            _logger.Debug("Cache-miss for {count} accounts", notCached.Count);
            
            var accounts = await QueryDatabase(notCached);
            foreach (var account in accounts)
            {
                AddToCache(account.Key, account.Result);
                result.Add(account);

                notCached.Remove(account.Key);
            }

            foreach (var nonExistingAccount in notCached)
            {
                AddToCache(nonExistingAccount, null);
                result.Add(new LookupResult(nonExistingAccount, null));
            }
        }
        
        return result.ToDictionary(x => x.Key, x => x.Result);
    }

    public void AddToCache(string baseAddress, long? accountId)
    {
        using var entry = _cache.CreateEntry(baseAddress);
        entry.SlidingExpiration = TimeSpan.FromDays(7);
        entry.Value = accountId;
    }

    private async Task<IEnumerable<LookupResult>> QueryDatabase(IEnumerable<string> baseAddresses)
    {
        var sql = @"
                select base_address as Key, id as Result  
                from graphql_accounts 
                where base_address = any(@BaseAddresses)";
        await using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        await conn.OpenAsync();

        return await conn.QueryAsync<LookupResult>(sql, new { BaseAddresses = baseAddresses });
    }

    private record LookupResult (string Key, long? Result);
}

