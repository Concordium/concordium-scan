using Application.Common.Diagnostics;
using Application.Database;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Application.Api.GraphQL.Import;

public interface IAccountLookup
{
    public IDictionary<string, long?> GetAccountIdsFromBaseAddresses(IEnumerable<string> accountBaseAddresses);
    void AddToCache(string baseAddress, long? accountId);
}

public class AccountLookup : IAccountLookup
{
    private readonly IMemoryCache _cache;
    private readonly DatabaseSettings _dbSettings;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;

    public AccountLookup(IMemoryCache cache, DatabaseSettings dbSettings, IMetrics metrics)
    {
        _cache = cache;
        _dbSettings = dbSettings;
        _metrics = metrics;
        _logger = Log.ForContext(GetType());
    }

    public IDictionary<string, long?> GetAccountIdsFromBaseAddresses(IEnumerable<string> accountBaseAddresses)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountLookup), nameof(GetAccountIdsFromBaseAddresses));

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

            var accounts = QueryDatabase(notCached);
            foreach (var account in accounts)
            {
                AddToCache(account.Key, account.Result);
                result.Add(account);
            }

            var nonExistingAccounts = notCached.Except(result.Select(x => x.Key));
            foreach (var nonExistingAccount in nonExistingAccounts)
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

    private IEnumerable<LookupResult> QueryDatabase(IEnumerable<string> baseAddresses)
    {
        var sql = @"
                select base_address as Key, id as Result  
                from graphql_accounts 
                where base_address = any(@BaseAddresses)";
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        return conn.Query<LookupResult>(sql, new { BaseAddresses = baseAddresses });
    }

    private record LookupResult (string Key, long? Result);
}

