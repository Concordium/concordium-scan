using System.Collections.Generic;
using Application.Api.GraphQL.Import;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class AccountLookupTest : IClassFixture<DatabaseFixture>, IDisposable
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly MemoryCache _memoryCache;
    private readonly AccountLookup _target;

    public AccountLookupTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);

        var options = new MemoryCacheOptions();
        _memoryCache = new MemoryCache(options);
        _target = new AccountLookup(_memoryCache, dbFixture.DatabaseSettings);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public void GetAccountIdsFromBaseAddressesAsync_EmptyQuery()
    {
        var result = _target.GetAccountIdsFromBaseAddresses(Array.Empty<string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAccountIdsFromBaseAddressesAsync_QuerySingle_DoesntExist()
    {
        var result = _target.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", null}
        };
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_QuerySingle_AccountExists()
    {
        await CreateAccount(42, "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        
        var result = _target.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", 42}
        };
        result.Should().Equal(expected);
    }

    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_QueryMultiple_AccountsExists_NoneCached()
    {
        await CreateAccount(42, "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        await CreateAccount(47, "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        
        _target.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        var result = _target.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", 42},
            {"44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", 47}
        };
        result.Should().Equal(expected);
    }

    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_QueryMultiple_AccountsExists_PartlyCached()
    {
        await CreateAccount(42, "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        await CreateAccount(47, "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");

        // Put one in cache...
        _target.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        // .. and now lookup with one in cache and one not in cache
        var result = _target.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", 42},
            {"44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", 47}
        };
        result.Should().Equal(expected);
    }

    private async Task CreateAccount(long accountId, string baseAddress)
    {
        var account = new AccountBuilder()
            .WithId(accountId)
            .WithCanonicalAddress(new AccountAddress(baseAddress).CreateAliasAddress(100, 200, 1).AsString) // Random alias 
            .WithBaseAddress(baseAddress)
            .Build();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
    }
}