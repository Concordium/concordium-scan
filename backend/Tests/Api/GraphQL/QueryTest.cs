using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Pagination;
using FluentAssertions;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Api.GraphQL;

public class QueryTest
{
    [Fact]
    public async Task FactMethodName()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<GraphQlDbContext>(options => options.UseInMemoryDatabase("graphql"));
        
        var executor = await services
            .AddGraphQLServer()
            .ConfigureSchema(SchemaConfiguration.Configure)
            .AddCursorPagingProvider<QueryableCursorPagingProvider>(defaultProvider:true)
            .AddCursorPagingProvider<BlockByDescendingIdCursorPagingProvider>(providerName:"block_by_descending_id")
            .BuildRequestExecutorAsync();

        var dbContextFactory = services.BuildServiceProvider().GetService<IDbContextFactory<GraphQlDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.Blocks.AddRangeAsync(
            new BlockBuilder().WithId(1).WithBlockHeight(100).Build(),
            new BlockBuilder().WithId(2).WithBlockHeight(101).Build(),
            new BlockBuilder().WithId(3).WithBlockHeight(102).Build(),
            new BlockBuilder().WithId(4).WithBlockHeight(103).Build(),
            new BlockBuilder().WithId(5).WithBlockHeight(104).Build());
        await dbContext.SaveChangesAsync();

        var result = await executor.ExecuteAsync("query {" +
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
        
        
        result.Errors.Should().BeNull();
        var json = await result.ToJsonAsync();
        var doc = JsonNode.Parse(json)!;
        var blocksNode = doc["data"]?["blocks"];
        var actual = blocksNode?["nodes"]?.AsArray().Select(x => (int)x!["blockHeight"]!).ToArray();
        actual.Should().Equal(104, 103);

        var endCursor = (string)blocksNode?["pageInfo"]?["endCursor"]!;

        await dbContext.Blocks.AddRangeAsync(new BlockBuilder().WithId(6).WithBlockHeight(105).Build());
        await dbContext.SaveChangesAsync();

        
        var result2 = await executor.ExecuteAsync("query {" +
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

        result2.Errors.Should().BeNull();
        json = await result2.ToJsonAsync();
        doc = JsonNode.Parse(json)!;
        blocksNode = doc["data"]?["blocks"];
        actual = blocksNode?["nodes"]?.AsArray().Select(x => (int)x!["blockHeight"]!).ToArray();
        actual.Should().Equal(102, 101);
    }
}

public class BlockBuilder
{
    private long _id = 1;
    private int _blockHeight = 47;

    public Block Build()
    {
        return new Block
        {
            Id = _id,
            BlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1",
            BlockHeight = _blockHeight,
            BlockSlotTime = new DateTimeOffset(2010, 10, 10, 12, 0, 0, TimeSpan.Zero),
            BakerId = 7,
            Finalized = true,
            TransactionCount = 0,
            SpecialEvents = new SpecialEvents(),

        };
    }

    public BlockBuilder WithId(long value)
    {
        _id = value;
        return this;
    }

    public BlockBuilder WithBlockHeight(int value)
    {
        _blockHeight = value;
        return this;
    }
}