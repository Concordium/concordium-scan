using Application.Api.GraphQL;
using FluentAssertions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Api.GraphQL;

public class SchemaConfigurationTest
{
    /// <summary>
    /// Smoke test that ensures that the schema is at least correct to a point that allows HotChocolate to build the schema. 
    /// </summary>
    [Fact]
    public async Task BuildSchema()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer().ConfigureSchema(SchemaConfiguration.Configure)
            .BuildSchemaAsync();
        
        schema.Should().NotBeNull();
    }
}
