using System.Collections.Generic;
using Application.Api.GraphQL.EfCore;

namespace Tests.TestUtilities.Stubs;

public class AccountLookupStub : IAccountLookup
{
    private readonly Dictionary<string, long?> _store = new();
    
    public Task<IDictionary<string, long?>> GetAccountIdsFromBaseAddressesAsync(IEnumerable<string> accountBaseAddresses)
    {
        var result = accountBaseAddresses
            .Select(x =>
            {
                if (_store.TryGetValue(x, out var value))
                    return new LookupResult(x, value);
                throw new InvalidOperationException("Address not found in dictionary. Please set up expected result via AddToCache!");
            })
            .ToArray();
        
        var dictionary = result.ToDictionary(x => x.Key, x => x.Result);
        return Task.FromResult<IDictionary<string, long?>>(dictionary);
    }

    public void AddToCache(string baseAddress, long? accountId)
    {
        _store[baseAddress] = accountId;
    }
}