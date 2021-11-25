using System;
using System.Linq;
using System.Net.Http;
using Application.Common.Logging;
using Application.Database;
using Application.Import.ConcordiumNode;
using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;

var performDatabaseMigration = args.FirstOrDefault()?.ToLower() == "migrate-db";

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<SourceClassNameEnricher>()    
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.AddHostedService<ImportController>();
builder.Services.AddSingleton<ConcordiumNodeGrpcClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<BlockRepository>();
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddSingleton(builder.Configuration.GetSection("PostgresDatabase").Get<DatabaseSettings>());
builder.Services.AddSingleton(builder.Configuration.GetSection("ConcordiumNodeGrpc").Get<ConcordiumNodeGrpcClientSettings>());
builder.Host.UseSystemd();
var app = builder.Build();

var logger = Log.ForContext<Program>();

try
{
    if (performDatabaseMigration)
    {
        logger.Information("Application started in database migration mode. Starting database migration...");
        app.Services.GetRequiredService<DatabaseMigrator>().MigrateDatabase();
        logger.Information("Database migration finished successfully");
    }
    else
    {
        logger.Information("Application starting...");
        app.Services.GetRequiredService<DatabaseMigrator>().EnsureDatabaseMigrationNotNeeded();
        app.Run();    
    }
}
catch (Exception e)
{
    logger.Fatal(e, "Unhandled exception caught. Terminating application.");

    // TODO: Do we need to signal to the process host that we are terminating due to an exception? throw?
}

logger.Information("Exiting application!");