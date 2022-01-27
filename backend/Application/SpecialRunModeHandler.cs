using System.Threading.Tasks;
using Application.Api.GraphQL;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class SpecialRunModeHandler
{
    public static async Task HandleCommandLineArgs(string[] args)
    {
        if (args[0] == "print-graphql-schema")
        {
            var schema = await new ServiceCollection()
                .AddGraphQLServer()
                .Configure()
                .BuildSchemaAsync();
        
            Console.Write(schema.Print());
        }
        else
        {
            Console.WriteLine("Unknown command!");
        }
    }
}