using System.IO;
using Application.Api.GraphQL;
using FluentAssertions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Tests.Api.GraphQL;

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
    
    /// <summary>
    /// This test ensures that no unintended GraphQL schema changes occur.
    ///
    /// If the current changes are intentional please regenerate the committed snap-shat and review the
    /// schema changes before pushing them.  
    ///
    /// Details: ---------------------------------------------------------------------
    /// A file is persisted with a snapshot of the last committed schema in the root folder of the solution:
    /// committed_graphql_schema.txt
    ///
    /// The file is updated/regenerated with the current schema by running the power shell script:
    /// regenerate-committed-graphql-schema-file.ps1
    ///
    /// To see what has changed in the schema regenerate the persisted snapshot and do a git diff.
    /// </summary>
    [Fact]
    public async Task GraphQlSchemaShouldNotChangeUnexpectedly()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .Configure()
            .BuildSchemaAsync();
        
        var currentSchema = schema.Print();
        var persistedSchema = await File.ReadAllTextAsync("committed_graphql_schema.txt");

        // persisted file normally differs with additional line-ending
        persistedSchema = persistedSchema.TrimEnd(' ', '\r', '\n');

        var equal = currentSchema.Equals(persistedSchema);
        if (!equal)
            throw new XunitException("The GraphQL schema has changed unexpectedly. See why this test fails and what you should do in comments in this test!");
    }
}
