using Application.Api.GraphQL;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Api.GraphQL;

public class QueryTest
{
    [Fact(Skip = "Work in progress...")]
    public async Task FactMethodName()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        var query = @"
            {
                block(blockHash:""test"")
                {
                    blockHash blockHeight
                }
            }";
        
        var result = await executor.ExecuteAsync(query);
        // Assert.Null(result.Errors);
        // var jsonDocument = JsonDocument.Parse(result.ToJson());
    }    
}
