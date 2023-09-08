using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Search;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Api.GraphQL.Search;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class SearchResultTest
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public SearchResultTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);

        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }

    [Theory]
    [InlineData("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1")]
    [InlineData("5c0a11302f4098572c4741905b071d958066e0550d03c3186")]
    [InlineData("5c0a1130")]
    [InlineData("100")]
    public async Task GetBlocks_FullBlockHash_SingleResult(string query)
    {
        await AddBlock(
            new BlockBuilder().WithBlockHeight(100).WithBlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1").Build(),
            new BlockBuilder().WithBlockHeight(101).WithBlockHash("12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9").Build());
        
        var target = new SearchResult(query);

        var result = target
            .GetBlocks(_dbContextFactory.CreateDbContextWithLog(_outputHelper.WriteLine))
            .ToArray();

        result.Length.Should().Be(1);
        result[0].BlockHash.Should().Be("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1");
    }

    [Fact]
    public async Task GetAccounts_FullAddressByCanonicalAddress()
    {
        await AddAccount(
            new AccountBuilder().WithId(1).WithCanonicalAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", true).Build(),
            new AccountBuilder().WithId(2).WithCanonicalAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", true).Build()
            );
        
        var target = new SearchResult("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        
        var result = target
            .GetAccounts(_dbContextFactory.CreateDbContext())
            .ToArray();

        result.Length.Should().Be(1);
        result[0].CanonicalAddress.AsString.Should().Be("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    }
    
    [Fact]
    public async Task GetAccounts_FullAddressByPartialAddress()
    {
        await AddAccount(
            new AccountBuilder().WithId(1).WithCanonicalAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", true).Build(),
            new AccountBuilder().WithId(2).WithCanonicalAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", true).Build()
            );
        
        var aliasAddress = AccountAddressHelper.GetAliasAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", 127);
        var target = new SearchResult(aliasAddress);
        
        var result = target
            .GetAccounts(_dbContextFactory.CreateDbContext())
            .ToArray();

        result.Length.Should().Be(1);
        result[0].CanonicalAddress.AsString.Should().Be("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    }
    
    [Fact]
    public async Task GetAccounts_PartialAddress()
    {
        await AddAccount(
            new AccountBuilder().WithId(1).WithCanonicalAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P", true).Build(),
            new AccountBuilder().WithId(2).WithCanonicalAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy", true).Build()
            );
        
        var target = new SearchResult("3XSLuJ");
        
        var result = target
            .GetAccounts(_dbContextFactory.CreateDbContextWithLog(_outputHelper.WriteLine))
            .ToArray();

        result.Length.Should().Be(1);
        result[0].CanonicalAddress.AsString.Should().Be("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    }

    [Theory]
    [InlineData("<31,32>", true, "<31, 32%")]
    [InlineData("31,32>", true, "<31, 32%")]
    [InlineData("42,32", true, "<42, 32%")]
    [InlineData("<31, 32>", true, "<31, 32%")]
    [InlineData("31, 32>", true, "<31, 32%")]
    [InlineData("42, 32", true, "<42, 32%")]
    [InlineData("42", true, "<42%")]
    [InlineData("31>", true, "<31%")]
    [InlineData("<42", true, "<42%")]
    [InlineData("42,", true, "<42,%")]
    [InlineData("42, ", true, "<42,%")]
    [InlineData("a", false, null)]
    [InlineData("00a", false, null)]
    [InlineData(null, false, null)]
    [InlineData("", false, null)]
    [InlineData("-42,32", false, null)]
    [InlineData("42,-32", false, null)]
    public void WhenMatchContractRegex_ThenReturnGroups(
        string query,
        bool expectedDidMatch,
        string expectedAddress)
    {
        // Act
        var searchResult = SearchResult.TryMatchContractPattern(
            query,
            out var actualAddress);
        
        // Assert
        searchResult.Should().Be(expectedDidMatch);
        actualAddress.Should().Be(expectedAddress);
    }
    
    private async Task AddBlock(params Block[] entities)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Blocks.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }
    
    private async Task AddAccount(params Account[] entities)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }
}