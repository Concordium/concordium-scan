using Application.Api.GraphQL;
using Application.Api.GraphQL.Configurations;
using FluentAssertions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using VerifyXunit;

namespace Tests.Api.GraphQL;

[UsesVerify]
public class SchemaTest
{
    /// <summary>
    /// Smoke test that ensures that the schema is at least correct to a point that allows HotChocolate to build the schema. 
    /// </summary>
    [Fact]
    public async Task BuildSchema()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .Configure()
            .BuildSchemaAsync();
        
        schema.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenGraphqlSchemaChanges_WhenBuild_ThenFailDueToSnapshotsNotMatched()
    {
        // Arrange & Act
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .Configure()
            .BuildSchemaAsync();
        
        // Assert
        var print = schema.Print();
        await Verifier.Verify(print)
            .UseDirectory("__snapshots__");
    }
}
