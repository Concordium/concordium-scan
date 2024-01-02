using System.Collections.Generic;
using System.Threading;
using Application.Api.GraphQL.Import;
using Application.Database;
using Application.Import.ConcordiumNode;
using Concordium.Sdk.Types;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class AccountLookupTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DatabaseSettings _databaseSettings;

    public AccountLookupTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        _databaseSettings = dbFixture.DatabaseSettings;
    }
    
    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_EmptyQuery()
    {
        using var testObject = await TestObject.Create(_databaseSettings);
        var result = testObject.AccountLookup.GetAccountIdsFromBaseAddresses(Array.Empty<string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_QuerySingle_DoesntExist()
    {
        using var testObject = await TestObject.Create(_databaseSettings);
        var result = testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", null}
        };
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_QuerySingle_AccountExists()
    {
        using var testObject = await TestObject.Create(_databaseSettings);
        await CreateAccount(42, "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        
        var result = testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", 42}
        };
        result.Should().Equal(expected);
    }

    [Fact]
    public async Task GetAccountIdsFromBaseAddressesAsync_QueryMultiple_AccountsExists_NoneCached()
    {
        using var testObject = await TestObject.Create(_databaseSettings);
        await CreateAccount(42, "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        await CreateAccount(47, "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
        
        testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        var result = testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"});
        
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
        using var testObject = await TestObject.Create(_databaseSettings);
        await CreateAccount(42, "3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        await CreateAccount(47, "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");

        // Put one in cache...
        testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"});
        
        // .. and now lookup with one in cache and one not in cache
        var result = testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[]{"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", "44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"});
        
        var expected = new Dictionary<string, long?>()
        {
            {"3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", 42},
            {"44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", 47}
        };
        result.Should().Equal(expected);
    }

    [Fact]
    public async Task GivenNoAccountInCacheOrDatabase_WhenCallingNodeWhichKnowsAccount_ThenReturnAccount()
    {
        // Arrange
        var uniqueAddress = AccountAddressHelper.GetUniqueAddress();
        const long accountIndex = 1L;
        var expected = new Dictionary<string, long?>
        {
            { uniqueAddress, accountIndex }
        };
        var clientMock = new Mock<IConcordiumNodeClient>();
        var accountInfo = new AccountInfo(
            AccountSequenceNumber.From(1UL),
            CcdAmount.Zero, 
            new AccountIndex(accountIndex),
            AccountAddress.From(uniqueAddress),
            null
        );
        clientMock.Setup(m => m.GetAccountInfoAsync(It.IsAny<IAccountIdentifier>(), It.IsAny<IBlockHashInput>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(accountInfo));
        using var testObject = await TestObject.Create(_databaseSettings, client: clientMock.Object);
        
        // Act
        var result = testObject.AccountLookup.GetAccountIdsFromBaseAddresses(new[] { uniqueAddress });
        
        // Assert
        result.Should().Equal(expected);
    }
    
    private sealed class TestObject : IDisposable
    {
        private readonly MemoryCache _cache;
        internal AccountLookup AccountLookup { get; }

        private TestObject(
            MemoryCache? memoryCache, 
            IConcordiumNodeClient client,
            DatabaseSettings databaseSettings
        )
        {
            _cache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            AccountLookup = new AccountLookup(
                _cache,
                databaseSettings,
                new NullMetrics(),
                client);
        }

        internal static async Task<TestObject> Create(
            DatabaseSettings databaseSettings,
            MemoryCache? memoryCache = null, 
            IConcordiumNodeClient? client = null
        )
        {
            await DatabaseFixture.TruncateTables("graphql_accounts");
            if (client == null)
            {
                var mock = new Mock<IConcordiumNodeClient>();
                mock.Setup(m => m.GetAccountInfoAsync(
                        It.IsAny<IAccountIdentifier>(),
                        It.IsAny<IBlockHashInput>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new RpcException(new Status(StatusCode.NotFound, string.Empty)));
                client = mock.Object;
            }
            return new TestObject(memoryCache, client, databaseSettings);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }    

    private async Task CreateAccount(long accountId, string baseAddress)
    {
        var account = new AccountBuilder()
            .WithId(accountId)
            .WithCanonicalAddress(AccountAddress.From(baseAddress).CreateAliasAddress(100, 200, 1).ToString()) // Random alias 
            .WithBaseAddress(baseAddress)
            .Build();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
    }
}
