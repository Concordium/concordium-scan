using Application.Database;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Api.GraphQL;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class QueryTest : IAsyncLifetime
{
    private readonly GraphQlTestHelper _testHelper = new();
    private readonly DatabaseSettings _dbSettings;

    public QueryTest(DatabaseFixture dbFixture)
    {
        _dbSettings = dbFixture.DatabaseSettings;
    }

    public async Task InitializeAsync()
    {
        await _testHelper.InitializeAsync(_dbSettings);
    }

    public async Task DisposeAsync()
    {
        await _testHelper.DisposeAsync();
    }
    
    /// <summary>
    /// The GraphQL built-in cursor based paging is actually just an offset based paging - this means that when new
    /// blocks are added, the pages will "shift". A custom cursor based paging has been implemented to mitigate this. 
    /// </summary>
    [Fact]
    public async Task GetBlocks_EnsurePagingCursorAreStableWhenNewBlocksAreAdded()
    {
        // Create some initial blocks...
        await _testHelper.DbContext.Blocks.AddRangeAsync(
            new BlockBuilder().WithId(0).WithBlockHeight(100).Build(),
            new BlockBuilder().WithId(0).WithBlockHeight(101).Build(),
            new BlockBuilder().WithId(0).WithBlockHeight(102).Build(),
            new BlockBuilder().WithId(0).WithBlockHeight(103).Build(),
            new BlockBuilder().WithId(0).WithBlockHeight(104).Build());
        await _testHelper.DbContext.SaveChangesAsync();

        // ... request the "first" two of them...
        var result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                blocks(first:2) {
                    nodes { 
                        blockHeight 
                    }
                    pageInfo {
                        startCursor
                        endCursor
                    }
                }
            }");
        
        // ... and check that we get the right nodes...
        var blocksNode = result["blocks"];
        var actual = blocksNode?["nodes"]?.AsArray().Select(x => (int)x!["blockHeight"]!).ToArray();
        actual.Should().Equal(104, 103);

        // ... remember the end cursor of this query...
        var endCursor = (string)blocksNode?["pageInfo"]?["endCursor"]!;

        // ... and add a new block to the top of the chain (in the database)...
        await _testHelper.DbContext.Blocks.AddRangeAsync(new BlockBuilder().WithId(0).WithBlockHeight(105).Build());
        await _testHelper.DbContext.SaveChangesAsync();
        
        // ... and make a new request using the cursor from the previous query...
        result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                blocks(first:2, after:""" + endCursor + @""") {
                    nodes { 
                        blockHeight 
                    }
                    pageInfo {
                        startCursor
                        endCursor
                    }
                }
            }");

        // ... and ensure that we get the right blocks (that the inserted block hasn't shifted the paging cursor)
        blocksNode = result["blocks"];
        actual = blocksNode?["nodes"]?.AsArray().Select(x => (int)x!["blockHeight"]!).ToArray();
        actual.Should().Equal(102, 101);
    }
    
    /// <summary>
    /// The GraphQL built-in cursor based paging is actually just an offset based paging - this means that when new
    /// blocks are added, the pages will "shift". A custom cursor based paging has been implemented to mitigate this. 
    /// </summary>
    [Fact]
    public async Task GetTransactions_EnsurePagingCursorAreStableWhenNewBlocksAreAdded()
    {
        // Create some initial transactions...
        await _testHelper.DbContext.Transactions.AddRangeAsync(
            new TransactionBuilder().WithId(0).WithTransactionHash("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b").Build(),
            new TransactionBuilder().WithId(0).WithTransactionHash("29a595cf84a713209651da4917d739677a6d7a29575e719be7180bbe640e3d6e").Build(),
            new TransactionBuilder().WithId(0).WithTransactionHash("e2df806768b6f6a52f8654a12be2e6c832fedabe1d1a27eb278dc4e5f9d8631f").Build(),
            new TransactionBuilder().WithId(0).WithTransactionHash("6cba7c4230d6ea6995f82383a9847b9c9abbbdc03dd56ed4b5aedc65e03da7e5").Build());
        await _testHelper.DbContext.SaveChangesAsync();

        // ... request the "first" two of them...
        var result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                transactions(first:2) {
                    nodes { 
                        transactionHash 
                    }
                    pageInfo {
                        startCursor
                        endCursor
                    }
                }
            }");
        
        // ... and check that we get the right nodes...
        var transactionsNode = result["transactions"];
        var actual = transactionsNode?["nodes"]?.AsArray().Select(x => (string)x!["transactionHash"]!).ToArray();
        actual.Should().Equal("6cba7c4230d6ea6995f82383a9847b9c9abbbdc03dd56ed4b5aedc65e03da7e5", "e2df806768b6f6a52f8654a12be2e6c832fedabe1d1a27eb278dc4e5f9d8631f");

        // ... remember the end cursor of this query...
        var endCursor = (string)transactionsNode?["pageInfo"]?["endCursor"]!;

        // ... and add a new transaction to the top of the chain (in the database)...
        await _testHelper.DbContext.Transactions.AddRangeAsync(new TransactionBuilder().WithId(0).WithTransactionHash("f090381cb30f2fefb449617fe9ed9eb7156a0f67d7d22e2c3bb358c5bc82ae23").Build());
        await _testHelper.DbContext.SaveChangesAsync();
        
        // ... and make a new request using the cursor from the previous query...
        result = await _testHelper.ExecuteGraphQlQueryAsync(@"
            query {
                transactions(first:2, after:""" + endCursor + @""") {
                    nodes { 
                        transactionHash 
                    }
                    pageInfo {
                        startCursor
                        endCursor
                    }
                }
            }");

        // ... and ensure that we get the right transactions (that the inserted transaction hasn't shifted the paging cursor)
        transactionsNode = result["transactions"];
        actual = transactionsNode?["nodes"]?.AsArray().Select(x => (string)x!["transactionHash"]!).ToArray();
        actual.Should().Equal("29a595cf84a713209651da4917d739677a6d7a29575e719be7180bbe640e3d6e", "42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b");
    }
}