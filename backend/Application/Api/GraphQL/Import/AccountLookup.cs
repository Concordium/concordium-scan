using System.Threading.Tasks;
using Application.Common.Diagnostics;
using Application.Database;
using Application.Import.ConcordiumNode;
using Concordium.Sdk.Types;
using Dapper;
using Grpc.Core;
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
    private readonly IConcordiumNodeClient _client;
    private readonly ILogger _logger;
    private readonly IBlockHashInput _blockHashInput = new LastFinal();

    public AccountLookup(
        IMemoryCache cache,
        DatabaseSettings dbSettings,
        IMetrics metrics,
        IConcordiumNodeClient client)
    {
        _cache = cache;
        _dbSettings = dbSettings;
        _metrics = metrics;
        _client = client;
        _logger = Log.ForContext(GetType());
    }

    /// <summary>
    /// The method first attempts to get account index using base address from cache,
    ///
    /// If not present the database i queried and the cache is updated for addresses present in the database.
    ///
    /// If the base address isn't present in the database the node is called and the cache is updated for addresses
    /// found on the node. This should almost always return an account index and only in cases where different nodes
    /// are queried would there be a small change of not fully synchronization between the nodes.
    /// Calls to the node are synchronized from async to sync using <see cref="Task.GetAwaiter"/>. 
    /// </summary>
    /// <param name="accountBaseAddresses"></param>
    /// <returns>
    /// Account ids from base addresses. Null is returned only if not present in cache, database and node.
    /// </returns>
    /// <exception cref="RpcException">Rethrow if error from node isn't <see cref="StatusCode.NotFound"/></exception>
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
        }
        
        var nonExistingAccounts = notCached.Except(result.Select(x => x.Key));
        
        foreach (var nonExistingAccount in nonExistingAccounts)
        {
            try
            {
                var response = _client.GetAccountInfoAsync(AccountAddress.From(nonExistingAccount), _blockHashInput)
                    .Result;
                var lookupResult = new LookupResult(nonExistingAccount, (long)response.AccountIndex.Index);
                AddToCache(lookupResult.Key, lookupResult.Result);
                result.Add(lookupResult);
            }
            catch (AggregateException e)
            {
                // Only catch errors due to account not found.
                if (e.InnerException is not RpcException rpcException)
                {
                    throw;
                }
                if (rpcException.StatusCode != StatusCode.NotFound)
                {
                    throw;
                }

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

