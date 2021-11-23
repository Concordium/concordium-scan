using System;
using System.Linq;
using System.Net.Http;
using Application.Database;
using Application.Import.ConcordiumNode;
using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

var onlyDatabaseMigration = args.FirstOrDefault()?.ToLower() == "migrate-db";

Log.Logger = new LoggerConfiguration()
    .Enrich.With<SourceClassNameEnricher>()    
    .WriteTo.Console(outputTemplate:"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] [{SourceClassName}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<ImportController>();
builder.Services.AddSingleton<ConcordiumNodeGrpcClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<BlockRepository>();
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddSingleton(builder.Configuration.GetSection("PostgresDatabase").Get<DatabaseSettings>());
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);
var app = builder.Build();

var logger = Log.ForContext<Program>();
try
{
    if (onlyDatabaseMigration)
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

public class SourceClassNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string result;
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var sourceContextString = sourceContext.ToString("l", null);
            var classNameStartIndex = sourceContextString.LastIndexOf(".", StringComparison.Ordinal) + 1;

            result = classNameStartIndex > 0
                ? sourceContextString.Substring(classNameStartIndex, sourceContextString.Length - classNameStartIndex)
                : sourceContextString;
        }
        else
            result = "(no context)";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SourceClassName", result));
    }
}
