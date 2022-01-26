using System.Text.Json.Nodes;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using FluentAssertions;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.TestUtilities.Builders.GraphQL;

namespace Tests.Api.GraphQL;

public class QueryTest : IAsyncLifetime
{
    private IRequestExecutor? _executor;
    private GraphQlDbContext? _dbContext;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<GraphQlDbContext>(options => options.UseInMemoryDatabase("graphql"));
        
        _executor = await services.AddGraphQLServer().Configure().BuildRequestExecutorAsync();

        var dbContextFactory = services.BuildServiceProvider().GetService<IDbContextFactory<GraphQlDbContext>>()!;
        _dbContext = await dbContextFactory.CreateDbContextAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null) 
            await _dbContext.DisposeAsync();
    }

    /// <summary>
    /// The GraphQL built-in cursor based paging is actually just an offset based paging - this means that when new
    /// blocks are added, the pages will "shift". A custom cursor based paging has been implemented to mitigate this. 
    /// </summary>
    [Fact]
    public async Task GetBlocks_EnsurePagingCursorAreStableWhenNewBlocksAreAdded()
    {
        if (_dbContext == null || _executor == null) throw new InvalidOperationException();
        
        // Create some initial blocks...
        await _dbContext.Blocks.AddRangeAsync(
            new BlockBuilder().WithId(1).WithBlockHeight(100).Build(),
            new BlockBuilder().WithId(2).WithBlockHeight(101).Build(),
            new BlockBuilder().WithId(3).WithBlockHeight(102).Build(),
            new BlockBuilder().WithId(4).WithBlockHeight(103).Build(),
            new BlockBuilder().WithId(5).WithBlockHeight(104).Build());
        await _dbContext.SaveChangesAsync();

        // ... request the "first" two of them...
        var result = await _executor.ExecuteAsync("query {" +
                                                 "  blocks(first:2)" +
                                                 "  {" +
                                                 "    nodes { " +
                                                 "      blockHeight " +
                                                 "    }" +
                                                 "    pageInfo {" +
                                                 "      startCursor" +
                                                 "      endCursor" +
                                                 "    }" +
                                                 "  }" +
                                                 "}");
        
        // ... and check that we get the right nodes...
        result.Errors.Should().BeNull();
        var json = await result.ToJsonAsync();
        var doc = JsonNode.Parse(json)!;
        var blocksNode = doc["data"]?["blocks"];
        var actual = blocksNode?["nodes"]?.AsArray().Select(x => (int)x!["blockHeight"]!).ToArray();
        actual.Should().Equal(104, 103);

        // ... remember the end cursor of this query...
        var endCursor = (string)blocksNode?["pageInfo"]?["endCursor"]!;

        // ... and add a new block to the top of the chain (in the database)...
        await _dbContext.Blocks.AddRangeAsync(new BlockBuilder().WithId(6).WithBlockHeight(105).Build());
        await _dbContext.SaveChangesAsync();
        
        // ... and make a new request using the cursor from the previous query...
        var result2 = await _executor.ExecuteAsync("query {" +
                                                 $"  blocks(first:2, after:\"{endCursor}\")" +
                                                 "  {" +
                                                 "    nodes { " +
                                                 "      blockHeight " +
                                                 "    }" +
                                                 "    pageInfo {" +
                                                 "      startCursor" +
                                                 "      endCursor" +
                                                 "    }" +
                                                 "  }" +
                                                 "}");

        // ... and ensure that we get the right blocks (that the inserted block hasn't shifted the paging cursor)
        result2.Errors.Should().BeNull();
        json = await result2.ToJsonAsync();
        doc = JsonNode.Parse(json)!;
        blocksNode = doc["data"]?["blocks"];
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
        if (_dbContext == null || _executor == null) throw new InvalidOperationException();
        
        // Create some initial transactions...
        await _dbContext.Transactions.AddRangeAsync(
            new TransactionBuilder().WithId(1).WithTransactionHash("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b").Build(),
            new TransactionBuilder().WithId(2).WithTransactionHash("29a595cf84a713209651da4917d739677a6d7a29575e719be7180bbe640e3d6e").Build(),
            new TransactionBuilder().WithId(3).WithTransactionHash("e2df806768b6f6a52f8654a12be2e6c832fedabe1d1a27eb278dc4e5f9d8631f").Build(),
            new TransactionBuilder().WithId(4).WithTransactionHash("6cba7c4230d6ea6995f82383a9847b9c9abbbdc03dd56ed4b5aedc65e03da7e5").Build());
        await _dbContext.SaveChangesAsync();

        // ... request the "first" two of them...
        var result = await _executor.ExecuteAsync("query {" +
                                                 "  transactions(first:2)" +
                                                 "  {" +
                                                 "    nodes { " +
                                                 "      transactionHash " +
                                                 "    }" +
                                                 "    pageInfo {" +
                                                 "      startCursor" +
                                                 "      endCursor" +
                                                 "    }" +
                                                 "  }" +
                                                 "}");
        
        // ... and check that we get the right nodes...
        result.Errors.Should().BeNull();
        var json = await result.ToJsonAsync();
        var doc = JsonNode.Parse(json)!;
        var transactionsNode = doc["data"]?["transactions"];
        var actual = transactionsNode?["nodes"]?.AsArray().Select(x => (string)x!["transactionHash"]!).ToArray();
        actual.Should().Equal("6cba7c4230d6ea6995f82383a9847b9c9abbbdc03dd56ed4b5aedc65e03da7e5", "e2df806768b6f6a52f8654a12be2e6c832fedabe1d1a27eb278dc4e5f9d8631f");

        // ... remember the end cursor of this query...
        var endCursor = (string)transactionsNode?["pageInfo"]?["endCursor"]!;

        // ... and add a new transaction to the top of the chain (in the database)...
        await _dbContext.Transactions.AddRangeAsync(new TransactionBuilder().WithId(5).WithTransactionHash("f090381cb30f2fefb449617fe9ed9eb7156a0f67d7d22e2c3bb358c5bc82ae23").Build());
        await _dbContext.SaveChangesAsync();
        
        // ... and make a new request using the cursor from the previous query...
        var result2 = await _executor.ExecuteAsync("query {" +
                                                 $"  transactions(first:2, after:\"{endCursor}\")" +
                                                 "  {" +
                                                 "    nodes { " +
                                                 "      transactionHash " +
                                                 "    }" +
                                                 "    pageInfo {" +
                                                 "      startCursor" +
                                                 "      endCursor" +
                                                 "    }" +
                                                 "  }" +
                                                 "}");

        // ... and ensure that we get the right transactions (that the inserted transaction hasn't shifted the paging cursor)
        result2.Errors.Should().BeNull();
        json = await result2.ToJsonAsync();
        doc = JsonNode.Parse(json)!;
        transactionsNode = doc["data"]?["transactions"];
        actual = transactionsNode?["nodes"]?.AsArray().Select(x => (string)x!["transactionHash"]!).ToArray();
        actual.Should().Equal("29a595cf84a713209651da4917d739677a6d7a29575e719be7180bbe640e3d6e", "42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b");
    }
}