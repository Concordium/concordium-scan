using System.Net.Http;
using Application.Api.GraphQL;
using Application.Common.Logging;
using Application.Database;
using Application.Import.ConcordiumNode;
using Application.Persistence;
using ConcordiumSdk.NodeApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var performDatabaseMigration = args.FirstOrDefault()?.ToLower() == "migrate-db";

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<SourceClassNameEnricher>()    
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);
var logger = Log.ForContext<Program>();

logger.Information("Application starting... [run-mode={runMode}]", performDatabaseMigration ? "migrate-db" : "normal");

var databaseSettings = builder.Configuration.GetSection("PostgresDatabase").Get<DatabaseSettings>();
logger.Information("Using Postgres connection string: {postgresConnectionString}", databaseSettings.ConnectionString);

builder.Services.AddGraphQLServer().AddQueryType<Query>();
builder.Services.AddHostedService<ImportController>();
builder.Services.AddControllers();
builder.Services.AddSingleton<GrpcNodeClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<BlockRepository>();
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddSingleton(databaseSettings);
builder.Services.AddSingleton(builder.Configuration.GetSection("ConcordiumNodeGrpc").Get<GrpcNodeClientSettings>());
builder.Host.UseSystemd();
var app = builder.Build();

try
{
    if (performDatabaseMigration)
    {
        logger.Information("Starting database migration...");
        app.Services.GetRequiredService<DatabaseMigrator>().MigrateDatabase();
        logger.Information("Database migration finished successfully");
    }
    else
    {
        app.Services.GetRequiredService<DatabaseMigrator>().EnsureDatabaseMigrationNotNeeded();
        
        app
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGraphQL();
            });
        
        app.Run();    
    }
}
catch (Exception e)
{
    logger.Fatal(e, "Unhandled exception caught. Terminating application.");
    Environment.ExitCode = -1;
}

logger.Information("Exiting application!");